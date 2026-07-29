using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Cliq.Application;
using Cliq.Domain;
using GenerationIdentifierNaming = Cliq.Infrastructure.Generation.IdentifierNaming;

namespace Cliq.Infrastructure;

public sealed partial class WorkspaceRepository : IWorkspaceRepository, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    private readonly SemaphoreSlim _writeGate = new(1, 1);
    private readonly SemaphoreSlim _workspaceGate = new(1, 1);
    private FileStream? _workspaceLease;
    private bool _disposed;

    public string? RootPath { get; private set; }

    public async Task<WorkspaceSnapshot> OpenAsync(string rootPath, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var fullPath = Path.GetFullPath(rootPath);
        if (!Directory.Exists(fullPath))
        {
            throw new DirectoryNotFoundException($"Workspace '{fullPath}' does not exist.");
        }

        await _workspaceGate.WaitAsync(cancellationToken);
        try
        {
            if (string.Equals(RootPath, fullPath, StringComparison.OrdinalIgnoreCase))
            {
                return await LoadSnapshotAsync(fullPath, cancellationToken);
            }

            var cliqDirectory = Path.Combine(fullPath, ".cliq");
            Directory.CreateDirectory(cliqDirectory);
            var candidateLease = await AcquireWorkspaceLeaseAsync(cliqDirectory, cancellationToken);
            try
            {
                Directory.CreateDirectory(Path.Combine(cliqDirectory, "pages"));
                Directory.CreateDirectory(Path.Combine(cliqDirectory, "tests"));
                Directory.CreateDirectory(Path.Combine(cliqDirectory, "runs"));
                await EnsureWorkspaceIgnoreAsync(cliqDirectory, cancellationToken);

                var projectPath = Path.Combine(cliqDirectory, "project.json");
                if (!File.Exists(projectPath))
                {
                    var now = DateTimeOffset.UtcNow;
                    var project = new ProjectManifest(
                        ProjectManifest.CurrentSchemaVersion,
                        Slug(Path.GetFileName(fullPath)),
                        Path.GetFileName(fullPath),
                        "https://example.com",
                        BrowserKind.Chrome,
                        new OutputSettings(),
                        now);
                    await WriteAtomicAsync(projectPath, project, cancellationToken);
                }

                var settingsPath = Path.Combine(cliqDirectory, "settings.json");
                if (!File.Exists(settingsPath))
                {
                    await WriteAtomicAsync(settingsPath, DefaultSettings(), cancellationToken);
                }

                var snapshot = await LoadSnapshotAsync(fullPath, cancellationToken);
                var previousLease = _workspaceLease;
                _workspaceLease = candidateLease;
                RootPath = fullPath;
                candidateLease = null;
                previousLease?.Dispose();
                return snapshot;
            }
            finally
            {
                candidateLease?.Dispose();
            }
        }
        finally
        {
            _workspaceGate.Release();
        }
    }

    public async Task CloseAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        await _workspaceGate.WaitAsync(cancellationToken);
        try
        {
            RootPath = null;
            _workspaceLease?.Dispose();
            _workspaceLease = null;
        }
        finally
        {
            _workspaceGate.Release();
        }
    }

    public async Task<WorkspaceSnapshot> GetSnapshotAsync(CancellationToken cancellationToken)
    {
        var rootPath = RequireRoot();
        return await LoadSnapshotAsync(rootPath, cancellationToken);
    }

    private static async Task<WorkspaceSnapshot> LoadSnapshotAsync(
        string rootPath,
        CancellationToken cancellationToken)
    {
        var cliqDirectory = Path.Combine(rootPath, ".cliq");
        var projectPath = Path.Combine(cliqDirectory, "project.json");
        var project = await ReadRequiredAsync<ProjectManifest>(projectPath, cancellationToken);
        ValidateVersion(project.SchemaVersion, ProjectManifest.CurrentSchemaVersion, projectPath);
        return new WorkspaceSnapshot(
            rootPath,
            project,
            await ReadDirectoryAsync<PageDefinition>(
                Path.Combine(cliqDirectory, "pages"),
                PageDefinition.CurrentSchemaVersion,
                cancellationToken),
            await ReadDirectoryAsync<TestCase>(
                Path.Combine(cliqDirectory, "tests"),
                TestCase.CurrentSchemaVersion,
                cancellationToken));
    }

    public async Task<ProjectManifest> SaveProjectAsync(ProjectManifest project, CancellationToken cancellationToken)
    {
        EnsureOpen();
        ValidateVersion(project.SchemaVersion, ProjectManifest.CurrentSchemaVersion, "project");
        ValidateRelativeDirectory(project.Output.PageObjectsDirectory, nameof(project.Output.PageObjectsDirectory));
        ValidateRelativeDirectory(project.Output.TestsDirectory, nameof(project.Output.TestsDirectory));
        var updated = project with { UpdatedAt = DateTimeOffset.UtcNow };
        await WriteAtomicAsync(ProjectPath, updated, cancellationToken);
        return updated;
    }

    public async Task<CliqSettings> GetSettingsAsync(CancellationToken cancellationToken)
    {
        EnsureOpen();
        var settings = await ReadOptionalAsync<CliqSettings>(SettingsPath, cancellationToken) ?? DefaultSettings();
        ValidateVersion(settings.SchemaVersion, CliqSettings.CurrentSchemaVersion, "settings");

        // A settings.json written before a field was introduced deserializes that field as null
        // (not an empty collection) via constructor binding - backfill so older files upgrade
        // cleanly instead of crashing whatever first reads the missing value.
        if (settings.ElementIdentification.IgnoreIdPatterns is null)
        {
            settings = settings with
            {
                ElementIdentification = settings.ElementIdentification with { IgnoreIdPatterns = [] },
            };
        }

        return settings;
    }

    public async Task<CliqSettings> SaveSettingsAsync(CliqSettings settings, CancellationToken cancellationToken)
    {
        EnsureOpen();
        ValidateVersion(settings.SchemaVersion, CliqSettings.CurrentSchemaVersion, "settings");
        ValidateSettings(settings);
        await WriteAtomicAsync(SettingsPath, settings, cancellationToken);
        return settings;
    }

    private static readonly HashSet<string> KnownIdentificationStrategies = new(StringComparer.Ordinal) { "testId", "id", "name" };

    private static void ValidateSettings(CliqSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.ElementIdentification.TestIdAttribute))
        {
            throw new WorkspaceValidationException("elementIdentification.testIdAttribute must not be empty.");
        }

        if (settings.ElementIdentification.StrategyOrder.Count == 0)
        {
            throw new WorkspaceValidationException("elementIdentification.strategyOrder must not be empty.");
        }

        foreach (var strategy in settings.ElementIdentification.StrategyOrder)
        {
            if (!KnownIdentificationStrategies.Contains(strategy))
            {
                throw new WorkspaceValidationException(
                    $"elementIdentification.strategyOrder contains unknown strategy '{strategy}'. Expected one of: testId, id, name.");
            }
        }

        foreach (var pattern in settings.ElementIdentification.IgnoreIdPatterns)
        {
            try
            {
                _ = new Regex(pattern);
            }
            catch (ArgumentException exception)
            {
                throw new WorkspaceValidationException(
                    $"elementIdentification.ignoreIdPatterns contains an invalid regular expression '{pattern}': {exception.Message}");
            }
        }

        if (settings.TestExecution.DefaultStepTimeoutMs <= 0)
        {
            throw new WorkspaceValidationException("testExecution.defaultStepTimeoutMs must be positive.");
        }

        if (settings.TestExecution.PageReadyTimeoutMs <= 0)
        {
            throw new WorkspaceValidationException("testExecution.pageReadyTimeoutMs must be positive.");
        }

        if (settings.Scanning.LoadTimeoutMs <= 0)
        {
            throw new WorkspaceValidationException("scanning.loadTimeoutMs must be positive.");
        }

        if (settings.Scanning.NetworkIdleGraceMs <= 0)
        {
            throw new WorkspaceValidationException("scanning.networkIdleGraceMs must be positive.");
        }
    }

    // Known framework-instance-generated id shapes: values that look like this are volatile
    // across page loads even without any DOM/data change, so they're untrustworthy as a locator
    // and as a naming source - see ElementIdentificationSettings.IgnoreIdPatterns.
    private static readonly string[] DefaultIgnoreIdPatterns =
    [
        @"^ember\d+$",
        @"^react-select-\d+-",
        @"^mat-[a-z]+(-[a-z]+)*-\d+$",
        @"^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$",
    ];

    private static CliqSettings DefaultSettings() => new(
        CliqSettings.CurrentSchemaVersion,
        new ElementIdentificationSettings("data-testid", ["testId", "id", "name"], DefaultIgnoreIdPatterns),
        new TestExecutionSettings(DefaultStepTimeoutMs: 10_000, PageReadyTimeoutMs: 15_000, CaptureScreenshotOnFailure: true),
        new ScanningSettings(LoadTimeoutMs: 30_000, NetworkIdleGraceMs: 3_000, CaptureScreenshot: true));

    public Task<IReadOnlyList<PageDefinition>> ListPagesAsync(CancellationToken cancellationToken) =>
        ReadDirectoryAsync<PageDefinition>(PagesDirectory, PageDefinition.CurrentSchemaVersion, cancellationToken);

    public async Task<PageDefinition?> GetPageAsync(string id, CancellationToken cancellationToken) =>
        await ReadOptionalAsync<PageDefinition>(EntityPath(PagesDirectory, id), cancellationToken);

    public async Task<PageDefinition> SavePageAsync(PageDefinition page, CancellationToken cancellationToken)
    {
        EnsureOpen();
        ValidateVersion(page.SchemaVersion, PageDefinition.CurrentSchemaVersion, "page");
        var updated = page with { UpdatedAt = DateTimeOffset.UtcNow };
        await WriteAtomicAsync(EntityPath(PagesDirectory, page.Id), updated, cancellationToken);
        return updated;
    }

    public Task DeletePageAsync(string id, CancellationToken cancellationToken) =>
        DeleteAsync(EntityPath(PagesDirectory, id), cancellationToken);

    public Task<IReadOnlyList<TestCase>> ListTestsAsync(CancellationToken cancellationToken) =>
        ReadDirectoryAsync<TestCase>(TestsDirectory, TestCase.CurrentSchemaVersion, cancellationToken);

    public async Task<TestCase?> GetTestAsync(string id, CancellationToken cancellationToken) =>
        await ReadOptionalAsync<TestCase>(EntityPath(TestsDirectory, id), cancellationToken);

    public async Task<TestCase> SaveTestAsync(TestCase test, CancellationToken cancellationToken)
    {
        EnsureOpen();
        ValidateVersion(test.SchemaVersion, TestCase.CurrentSchemaVersion, "test");
        await ValidateStepReferencesAsync(test, cancellationToken);
        var updated = test with { UpdatedAt = DateTimeOffset.UtcNow };
        await WriteAtomicAsync(EntityPath(TestsDirectory, test.Id), updated, cancellationToken);
        return updated;
    }

    private static readonly HashSet<StepAction> ActionsRequiringElement =
    [
        StepAction.Click,
        StepAction.Fill,
        StepAction.Select,
        StepAction.PressKey,
        StepAction.AssertVisible,
        StepAction.AssertText,
    ];

    private async Task ValidateStepReferencesAsync(TestCase test, CancellationToken cancellationToken)
    {
        var pageCache = new Dictionary<string, PageDefinition?>(StringComparer.Ordinal);

        foreach (var step in test.Steps)
        {
            if (!ActionsRequiringElement.Contains(step.Action))
            {
                continue;
            }

            if (string.IsNullOrEmpty(step.PageId))
            {
                throw new WorkspaceValidationException($"Step '{step.Id}' has no page reference.");
            }

            if (!pageCache.TryGetValue(step.PageId, out var page))
            {
                page = await ReadOptionalAsync<PageDefinition>(EntityPath(PagesDirectory, step.PageId), cancellationToken);
                pageCache[step.PageId] = page;
            }

            if (page is null)
            {
                throw new WorkspaceValidationException($"Step '{step.Id}' references page '{step.PageId}', which does not exist.");
            }

            if (string.IsNullOrEmpty(step.ElementId) || page.Elements.All(element => element.Id != step.ElementId))
            {
                throw new WorkspaceValidationException(
                    $"Step '{step.Id}' references element '{step.ElementId}' on page '{step.PageId}', which does not exist.");
            }
        }
    }

    public Task DeleteTestAsync(string id, CancellationToken cancellationToken) =>
        DeleteAsync(EntityPath(TestsDirectory, id), cancellationToken);

    public Task SaveRunAsync(TestRun run, CancellationToken cancellationToken)
    {
        EnsureOpen();
        var runDirectory = Path.Combine(RunsDirectory, SafeId(run.Id));
        Directory.CreateDirectory(runDirectory);
        return WriteAtomicAsync(Path.Combine(runDirectory, "run.json"), run, cancellationToken);
    }

    public string? GetScreenshotPath(string runId, string fileName)
    {
        EnsureOpen();
        if (!IdPattern().IsMatch(runId) || !GenerationIdentifierNaming.IsSafeFileName(fileName))
        {
            return null;
        }

        var path = Path.Combine(RunsDirectory, runId, fileName);
        return File.Exists(path) ? path : null;
    }

    internal string GetContainedPath(string relativeDirectory)
    {
        EnsureOpen();
        ValidateRelativeDirectory(relativeDirectory, nameof(relativeDirectory));
        var root = Path.GetFullPath(RootPath!);
        var candidate = Path.GetFullPath(Path.Combine(root, relativeDirectory));
        if (!candidate.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new WorkspaceValidationException("Configured output directory escapes the workspace.");
        }

        return candidate;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        RootPath = null;
        _workspaceLease?.Dispose();
        _workspaceLease = null;
        _writeGate.Dispose();
        _workspaceGate.Dispose();
    }

    private string CliqDirectory => Path.Combine(RequireRoot(), ".cliq");
    private string ProjectPath => Path.Combine(CliqDirectory, "project.json");
    private string SettingsPath => Path.Combine(CliqDirectory, "settings.json");
    private string PagesDirectory => Path.Combine(CliqDirectory, "pages");
    private string TestsDirectory => Path.Combine(CliqDirectory, "tests");
    private string RunsDirectory => Path.Combine(CliqDirectory, "runs");

    private string RequireRoot() => RootPath ?? throw new WorkspaceNotOpenException();
    private void EnsureOpen() => _ = RequireRoot();

    private static string EntityPath(string directory, string id) =>
        Path.Combine(directory, $"{SafeId(id)}.json");

    private static string SafeId(string id)
    {
        if (!IdPattern().IsMatch(id))
        {
            throw new WorkspaceValidationException($"'{id}' is not a valid identifier.");
        }

        return id;
    }

    private static string Slug(string value)
    {
        var slug = Regex.Replace(value.ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');
        return string.IsNullOrWhiteSpace(slug) ? "cliq-project" : slug;
    }

    private static void ValidateVersion(int actual, int expected, string source)
    {
        if (actual != expected)
        {
            throw new WorkspaceValidationException(
                $"Unsupported schema version {actual} in '{source}'. Expected {expected}.");
        }
    }

    private static void ValidateRelativeDirectory(string value, string field)
    {
        if (string.IsNullOrWhiteSpace(value) || Path.IsPathRooted(value))
        {
            throw new WorkspaceValidationException($"{field} must be a relative workspace directory.");
        }
    }

    private static async Task<IReadOnlyList<T>> ReadDirectoryAsync<T>(
        string directory,
        int currentVersion,
        CancellationToken cancellationToken)
    {
        if (!Directory.Exists(directory))
        {
            return [];
        }

        var items = new List<T>();
        foreach (var path in Directory.EnumerateFiles(directory, "*.json").Order(StringComparer.Ordinal))
        {
            var item = await ReadRequiredAsync<T>(path, cancellationToken);
            var version = typeof(T).GetProperty("SchemaVersion")?.GetValue(item) as int?;
            ValidateVersion(version ?? 0, currentVersion, path);
            items.Add(item);
        }

        return items;
    }

    private static async Task<T> ReadRequiredAsync<T>(string path, CancellationToken cancellationToken) =>
        await ReadOptionalAsync<T>(path, cancellationToken)
        ?? throw new WorkspaceValidationException($"Required workspace file '{path}' is missing.");

    private static async Task<T?> ReadOptionalAsync<T>(string path, CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            return default;
        }

        try
        {
            await using var stream = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions, cancellationToken);
        }
        catch (JsonException exception)
        {
            throw new WorkspaceValidationException($"Invalid JSON in '{path}': {exception.Message}");
        }
    }

    private async Task WriteAtomicAsync<T>(string path, T value, CancellationToken cancellationToken)
    {
        await _writeGate.WaitAsync(cancellationToken);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var tempPath = $"{path}.{Guid.NewGuid():N}.tmp";
            await using (var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await JsonSerializer.SerializeAsync(stream, value, JsonOptions, cancellationToken);
                await stream.FlushAsync(cancellationToken);
            }

            File.Move(tempPath, path, true);
        }
        finally
        {
            _writeGate.Release();
        }
    }

    private static Task DeleteAsync(string path, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    private static async Task EnsureWorkspaceIgnoreAsync(
        string cliqDirectory,
        CancellationToken cancellationToken)
    {
        var ignorePath = Path.Combine(cliqDirectory, ".gitignore");
        const string contents = "runs/\nsecrets.local.json\nworkspace.lock\n*.tmp\n";
        if (!File.Exists(ignorePath) || await File.ReadAllTextAsync(ignorePath, cancellationToken) != contents)
        {
            await File.WriteAllTextAsync(ignorePath, contents, cancellationToken);
        }
    }

    private static async Task<FileStream> AcquireWorkspaceLeaseAsync(
        string cliqDirectory,
        CancellationToken cancellationToken)
    {
        var lockPath = Path.Combine(cliqDirectory, "workspace.lock");
        FileStream stream;
        try
        {
            stream = new FileStream(
                lockPath,
                FileMode.OpenOrCreate,
                FileAccess.ReadWrite,
                FileShare.Read,
                bufferSize: 4_096,
                FileOptions.Asynchronous);
        }
        catch (IOException)
        {
            throw new WorkspaceInUseException(await ReadLeaseOwnerAsync(lockPath, cancellationToken));
        }

        try
        {
            var owner = new WorkspaceLeaseOwner(
                NormalizeHostKind(Environment.GetEnvironmentVariable("CLIQ_HOST_KIND")),
                Environment.ProcessId,
                DateTimeOffset.UtcNow);
            stream.SetLength(0);
            await JsonSerializer.SerializeAsync(stream, owner, JsonOptions, cancellationToken);
            await stream.FlushAsync(cancellationToken);
            return stream;
        }
        catch
        {
            await stream.DisposeAsync();
            throw;
        }
    }

    private static async Task<WorkspaceLeaseOwner?> ReadLeaseOwnerAsync(
        string lockPath,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = new FileStream(
                lockPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete,
                bufferSize: 4_096,
                FileOptions.Asynchronous);
            var owner = await JsonSerializer.DeserializeAsync<WorkspaceLeaseOwner>(
                stream,
                JsonOptions,
                cancellationToken);
            return owner is null
                ? null
                : owner with
                {
                    HostKind = NormalizeHostKind(owner.HostKind),
                    ProcessId = Math.Max(0, owner.ProcessId),
                };
        }
        catch (IOException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string NormalizeHostKind(string? hostKind)
    {
        if (string.IsNullOrWhiteSpace(hostKind))
        {
            return "unknown";
        }

        var normalized = new string(hostKind
            .Trim()
            .Take(64)
            .Where(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_')
            .ToArray());
        return string.IsNullOrEmpty(normalized) ? "unknown" : normalized;
    }

    [GeneratedRegex("^[a-zA-Z0-9][a-zA-Z0-9_-]{0,127}$")]
    private static partial Regex IdPattern();
}

using System.Text.Json;
using System.Text.RegularExpressions;
using Cliq.Application;
using Cliq.Domain;

namespace Cliq.Infrastructure;

/// <summary>
/// Workspace lifecycle (open/close/lease) and the generic JSON file I/O every other partial of
/// this class builds on. The per-concern CRUD (project/settings/pages/tests/runs) lives in its own
/// WorkspaceRepository.*.cs file - this one only owns what's shared across all of them.
/// </summary>
public sealed partial class WorkspaceRepository : IWorkspaceRepository, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = CliqJsonOptions.Indented;

    private readonly AtomicFileWriter _atomicWriter = new();
    private readonly SemaphoreSlim _workspaceGate = new(1, 1);
    private WorkspaceLeaseManager.WorkspaceLease? _workspaceLease;
    private bool _disposed;

    public string? RootPath { get; private set; }

    public async Task<WorkspaceSnapshot> OpenAsync(string rootPath, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var fullPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(rootPath));
        if (!Directory.Exists(fullPath))
        {
            throw new DirectoryNotFoundException($"Workspace '{fullPath}' does not exist.");
        }

        return await _workspaceGate.WithLockAsync(async () =>
        {
            if (string.Equals(RootPath, fullPath, StringComparison.OrdinalIgnoreCase))
            {
                return await LoadSnapshotAsync(fullPath, cancellationToken);
            }

            var cliqDirectory = Path.Combine(fullPath, ".cliq");
            Directory.CreateDirectory(cliqDirectory);
            var candidateLease = await WorkspaceLeaseManager.AcquireAsync(cliqDirectory, JsonOptions, cancellationToken);
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
        }, cancellationToken);
    }

    public async Task CloseAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        await _workspaceGate.WithLockAsync(() =>
        {
            RootPath = null;
            _workspaceLease?.Dispose();
            _workspaceLease = null;
            return Task.CompletedTask;
        }, cancellationToken);
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
        _atomicWriter.Dispose();
        _workspaceGate.Dispose();
    }

    private string CliqDirectory => Path.Combine(RequireRoot(), ".cliq");
    private string ProjectPath => Path.Combine(CliqDirectory, "project.json");
    private string SettingsPath => Path.Combine(CliqDirectory, "settings.json");
    private string PagesDirectory => Path.Combine(CliqDirectory, "pages");
    private string TestsDirectory => Path.Combine(CliqDirectory, "tests");
    private string RunsDirectory => Path.Combine(CliqDirectory, "runs");
    private string PendingCustomMethodsPath => Path.Combine(CliqDirectory, "pending-custom-methods.json");

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

    private Task WriteAtomicAsync<T>(string path, T value, CancellationToken cancellationToken) =>
        _atomicWriter.WriteJsonAsync(path, value, JsonOptions, cancellationToken);

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

    [GeneratedRegex("^[a-zA-Z0-9][a-zA-Z0-9_-]{0,127}$")]
    private static partial Regex IdPattern();
}

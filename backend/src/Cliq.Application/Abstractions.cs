using Cliq.Domain;

namespace Cliq.Application;

public interface IWorkspaceRepository
{
    string? RootPath { get; }
    Task<WorkspaceSnapshot> OpenAsync(string rootPath, CancellationToken cancellationToken);
    Task CloseAsync(CancellationToken cancellationToken);
    Task<WorkspaceSnapshot> GetSnapshotAsync(CancellationToken cancellationToken);
    Task<ProjectManifest> SaveProjectAsync(ProjectManifest project, CancellationToken cancellationToken);
    Task<CliqSettings> GetSettingsAsync(CancellationToken cancellationToken);
    Task<CliqSettings> SaveSettingsAsync(CliqSettings settings, CancellationToken cancellationToken);
    Task<IReadOnlyList<PageDefinition>> ListPagesAsync(CancellationToken cancellationToken);
    Task<PageDefinition?> GetPageAsync(string id, CancellationToken cancellationToken);
    Task<PageDefinition> SavePageAsync(PageDefinition page, CancellationToken cancellationToken);
    Task DeletePageAsync(string id, CancellationToken cancellationToken);
    Task<IReadOnlyList<TestCase>> ListTestsAsync(CancellationToken cancellationToken);
    Task<TestCase?> GetTestAsync(string id, CancellationToken cancellationToken);
    Task<TestCase> SaveTestAsync(TestCase test, CancellationToken cancellationToken);
    Task DeleteTestAsync(string id, CancellationToken cancellationToken);
    Task SaveRunAsync(TestRun run, CancellationToken cancellationToken);
    string? GetScreenshotPath(string runId, string fileName);
    string? GetDiscoveryScreenshotPath(string fileName);
    Task<IReadOnlyList<PendingCustomMethodEntry>> ListPendingCustomMethodsAsync(CancellationToken cancellationToken);
    Task DeletePendingCustomMethodAsync(string id, CancellationToken cancellationToken);

    /// <summary>
    /// The absolute path <paramref name="page"/>'s Page Object would be written to by the code
    /// generator - computed the same way generation itself derives it (project's configured
    /// output directory + the page's generated class name), regardless of whether that file has
    /// actually been generated yet.
    /// </summary>
    Task<string> GetGeneratedPageObjectPathAsync(PageDefinition page, CancellationToken cancellationToken);
}

public interface IPageDiscovery
{
    Task<PageScanResult> ScanAsync(
        string url,
        string name,
        BrowserKind browser,
        bool headless,
        CancellationToken cancellationToken);
}

public interface ICodeGeneration
{
    Task<GenerationResult> GenerateAsync(
        ProjectManifest project,
        IReadOnlyList<PageDefinition> pages,
        IReadOnlyList<TestCase> tests,
        string workspaceRoot,
        CancellationToken cancellationToken,
        IReadOnlyDictionary<string, IReadOnlyList<string>>? customMethodResolutions = null,
        // Per page, the custom methods the user chose to permanently discard rather than carry
        // over or defer to the waiting list. Anything neither resolved nor discarded still
        // defaults to deferred.
        IReadOnlyDictionary<string, IReadOnlyList<string>>? customMethodDiscards = null);
}

public interface ITestRunner
{
    Task<TestRun> RunAsync(
        string runId,
        TestCase test,
        IReadOnlyDictionary<string, PageDefinition> pages,
        BrowserKind browser,
        IReadOnlyDictionary<string, string> secrets,
        IProgress<StepResult> progress,
        CancellationToken cancellationToken);

    /// <summary>
    /// Opens a visible browser, navigates to <paramref name="page"/>'s URL, locates the element
    /// identified by <paramref name="elementId"/>, and highlights it - deliberately leaves the
    /// browser open afterward (unlike <see cref="RunAsync"/>, which always closes its driver) so
    /// the caller can visually inspect the element. Throws <see cref="WorkspaceValidationException"/>
    /// if the element id doesn't exist on the page or can't be located within the configured
    /// timeout; in that failure case the browser is closed before the exception is thrown.
    /// </summary>
    Task LocateElementAsync(
        PageDefinition page,
        string elementId,
        BrowserKind browser,
        CancellationToken cancellationToken);

    /// <summary>
    /// Same behavior as <see cref="LocateElementAsync"/>, but resolves the page and element from
    /// a generated Page Object's class name and the <c>// Locator (...): ...</c> comment text
    /// selected in its source file, instead of a page id/element id pair - the entry point for
    /// triggering "Locate in Browser" directly from the .cs file. Throws
    /// <see cref="WorkspaceValidationException"/> if the class name doesn't match any page, the
    /// comment isn't recognizable, it names a dynamic (templated) locator, or no element renders
    /// to that locator.
    /// </summary>
    Task LocateElementFromSourceAsync(
        string className,
        string locatorComment,
        BrowserKind browser,
        CancellationToken cancellationToken);
}

public interface ISecretStore
{
    Task<IReadOnlyDictionary<string, string>> ReadAsync(CancellationToken cancellationToken);
    Task SaveAsync(IReadOnlyDictionary<string, string> secrets, CancellationToken cancellationToken);
    Task DeleteAsync(string key, CancellationToken cancellationToken);
}

public sealed class WorkspaceNotOpenException : InvalidOperationException
{
    public WorkspaceNotOpenException() : base("No Cliq workspace is open.") { }
}

public sealed record WorkspaceLeaseOwner(
    string HostKind,
    int ProcessId,
    DateTimeOffset AcquiredAt);

public sealed class WorkspaceInUseException(WorkspaceLeaseOwner? owner)
    : InvalidOperationException("The Cliq workspace is open in another host.")
{
    public WorkspaceLeaseOwner? Owner { get; } = owner;
}

public sealed class WorkspaceValidationException(string message) : InvalidOperationException(message);

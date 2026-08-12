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

public interface IScanReviewService
{
    Task<StagedPageScanResult> ScanAsync(
        string url,
        string name,
        BrowserKind browser,
        bool headless,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<ScanReview>> ListAsync(
        string? pageId,
        ScanReviewStatus? status,
        CancellationToken cancellationToken);
    Task<ScanReview> GetAsync(string reviewId, CancellationToken cancellationToken);
    Task<ScanReview> UpdateDecisionsAsync(
        string reviewId,
        IReadOnlyList<ScanReviewDecisionUpdate> updates,
        CancellationToken cancellationToken);
    Task<ScanReviewApplyResult> ApplyAsync(string reviewId, CancellationToken cancellationToken);
    Task<ScanReview> DiscardAsync(string reviewId, CancellationToken cancellationToken);
}

public sealed record ScanReviewDecisionUpdate(string ItemId, ScanReviewDecision Decision);

public interface ICodeGeneration
{
    Task<GenerationResult> GenerateAsync(
        ProjectManifest project,
        IReadOnlyList<PageDefinition> pages,
        IReadOnlyList<TestCase> tests,
        string workspaceRoot,
        CancellationToken cancellationToken);
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

public sealed class ScanReviewConflictException(
    string code,
    string message,
    IReadOnlyList<ScanReviewReference>? references = null) : InvalidOperationException(message)
{
    public string Code { get; } = code;
    public IReadOnlyList<ScanReviewReference> References { get; } = references ?? [];
}

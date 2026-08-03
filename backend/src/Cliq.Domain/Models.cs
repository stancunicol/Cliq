namespace Cliq.Domain;

public enum BrowserKind
{
    Chrome,
    Edge,
}

public enum LocatorStrategy
{
    TestId,
    Id,
    Name,
    Css,
    XPath,
}

public enum StepAction
{
    Navigate,
    Click,
    Fill,
    Select,
    PressKey,
    AssertVisible,
    AssertText,
    AssertUrl,
}

public enum RunStatus
{
    Queued,
    Running,
    Passed,
    Failed,
    Cancelled,
}

public sealed record OutputSettings(
    string PageObjectsDirectory = "Generated/PageObjects",
    string TestsDirectory = "Generated/Tests",
    string PageObjectsNamespace = "Generated.PageObjects",
    string TestsNamespace = "Generated.Tests");

public sealed record ProjectManifest(
    int SchemaVersion,
    string Id,
    string Name,
    string BaseUrl,
    BrowserKind DefaultBrowser,
    OutputSettings Output,
    DateTimeOffset UpdatedAt)
{
    public const int CurrentSchemaVersion = 1;
}

public sealed record ElementIdentificationSettings(
    string TestIdAttribute,
    IReadOnlyList<string> StrategyOrder,
    IReadOnlyList<string> IgnoreIdPatterns);

public sealed record TestExecutionSettings(
    int DefaultStepTimeoutMs,
    int PageReadyTimeoutMs,
    bool CaptureScreenshotOnFailure);

public sealed record ScanningSettings(
    int LoadTimeoutMs,
    int NetworkIdleGraceMs,
    bool CaptureScreenshot);

public sealed record CliqSettings(
    int SchemaVersion,
    ElementIdentificationSettings ElementIdentification,
    TestExecutionSettings TestExecution,
    ScanningSettings Scanning)
{
    public const int CurrentSchemaVersion = 1;
}

public sealed record Locator(
    LocatorStrategy Strategy,
    string Value,
    string? DisplayName = null);

public sealed record PageElement(
    string Id,
    string Name,
    string Tag,
    StepAction Action,
    Locator Locator,
    string? ExpectedText = null,
    bool NotFound = false);

public sealed record ScanReconciliationReport(
    int MatchedCount,
    int NewCount,
    int MissingCount);

public sealed record PageScanResult(
    PageDefinition Page,
    ScanReconciliationReport Reconciliation);

public sealed record PageDefinition(
    int SchemaVersion,
    string Id,
    string Name,
    string Url,
    IReadOnlyList<PageElement> Elements,
    DateTimeOffset UpdatedAt)
{
    public const int CurrentSchemaVersion = 1;
}

public sealed record StepValue(
    string Kind,
    string Value)
{
    public static StepValue Literal(string value) => new("literal", value);
    public static StepValue Secret(string key) => new("secret", key);
}

public sealed record TestStep(
    string Id,
    StepAction Action,
    string Label,
    bool Enabled,
    string? PageId = null,
    string? ElementId = null,
    string? Url = null,
    StepValue? Value = null,
    int? TimeoutMs = null);

public sealed record TestCase(
    int SchemaVersion,
    string Id,
    string Name,
    string? Description,
    IReadOnlyList<string> Tags,
    IReadOnlyList<TestStep> Steps,
    DateTimeOffset UpdatedAt)
{
    public const int CurrentSchemaVersion = 1;
}

public sealed record StepResult(
    string StepId,
    RunStatus Status,
    long DurationMs,
    string? Error = null,
    string? ScreenshotPath = null,
    long? LocateMs = null,
    long? ActionMs = null);

public sealed record TestRun(
    string Id,
    string TestId,
    RunStatus Status,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    IReadOnlyList<StepResult> Steps,
    string? Error = null);

public sealed record WorkspaceSnapshot(
    string RootPath,
    ProjectManifest Project,
    IReadOnlyList<PageDefinition> Pages,
    IReadOnlyList<TestCase> Tests);

public sealed record GeneratedArtifact(
    string Kind,
    string Name,
    string Path,
    bool Created);

public sealed record GenerationResult(IReadOnlyList<GeneratedArtifact> Artifacts);

using System.Text.Json.Serialization;

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
    // The default camelCase JsonStringEnumConverter lowercases only the leading run of capitals
    // before a lowercase letter, so XPath would otherwise serialize as "xPath" - override it to
    // match the "xpath" string used as a strategy identifier everywhere else (the JS scanner,
    // settings.json's strategyOrder, the frontend's LocatorStrategy type).
    [JsonStringEnumMemberName("xpath")]
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

public sealed record LocatorDiffEntry(
    string ElementId,
    string Name,
    string Tag,
    Locator Locator);

/// <summary>
/// Locators-only diff between a page's previous and freshly scanned element sets. Reconciliation
/// tiers match on an exact locator value (see <c>ElementReconciler</c>), so there is no "changed"
/// category: anything that doesn't match becomes an entry in exactly one of these two lists, never
/// both, and this replaces the earlier self-healing approach of guessing a live replacement.
/// </summary>
public sealed record LocatorDiffReport(
    string PageId,
    DateTimeOffset ScannedAt,
    IReadOnlyList<LocatorDiffEntry> New,
    IReadOnlyList<LocatorDiffEntry> Missing);

public sealed record PageScanResult(
    PageDefinition Page,
    ScanReconciliationReport Reconciliation,
    LocatorDiffReport? LocatorDiff = null);

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

/// <summary>
/// Flags a Page Object file that still has hand-written <c>[CliqCustomMethod]</c> content
/// generation could not safely fold into the current output - the file itself is left in place
/// (never deleted, sometimes also copied to a timestamped backup) rather than the difference
/// being silently dropped. Two situations raise this:
/// <list type="bullet">
/// <item>Same-name regeneration recovers fewer marked methods than the previous file's marker
/// count suggests it should have (see <c>CustomMethodExtractor.CountMarkerOccurrences</c>) - in
/// this case <see cref="Path"/> is a timestamped copy under
/// <c>.cliq/runs/custom-method-backups</c>.</item>
/// <item>A page was renamed, so its Page Object now generates under a new class name - the old
/// file is deleted automatically if it has no marked methods, otherwise <see cref="Path"/> is
/// that old file's own, untouched location.</item>
/// </list>
/// </summary>
public sealed record CustomMethodRecoveryWarning(
    string PageObjectName,
    int ExpectedCount,
    int RecoveredCount,
    string Path);

public sealed record GenerationResult(
    IReadOnlyList<GeneratedArtifact> Artifacts,
    IReadOnlyList<CustomMethodRecoveryWarning> Warnings);

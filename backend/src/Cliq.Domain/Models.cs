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
    bool NotFound = false,
    // Only meaningful for Fill (the value type an <input> holds - "string"/"int"/"DateTime",
    // inferred from the scanned input type); ignored for every other action.
    string DataType = "string");

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

/// <summary>
/// A scan-detected candidate for a <see cref="DynamicElement"/> - a group of 3+ structurally
/// similar elements found at the same relative position inside a repeating row (e.g. every row's
/// "edit" button in a table). Not persisted on its own; the user reviews and either accepts it
/// (becoming a real <see cref="DynamicElement"/> with a fresh id) or ignores it. Always
/// <see cref="LocatorStrategy.XPath"/> - a repeating group's members don't individually carry a
/// stable non-positional key the way a single scanned element might.
/// </summary>
public sealed record DynamicElementSuggestion(
    string Name,
    StepAction Action,
    LocatorStrategy Strategy,
    string LocatorTemplate,
    int SampleCount);

public sealed record PageScanResult(
    PageDefinition Page,
    ScanReconciliationReport Reconciliation,
    LocatorDiffReport? LocatorDiff = null,
    IReadOnlyList<DynamicElementSuggestion>? DynamicElementSuggestions = null,
    // Bare file name only (not a full path) - same convention as StepResult.ScreenshotPath -
    // resolved through WorkspaceRepository.GetDiscoveryScreenshotPath, never trusted as a path
    // directly. Only present when scanning.captureScreenshot is enabled.
    string? ScreenshotPath = null);

/// <summary>
/// A user-authored locator for a repeating element (e.g. a table row) that a one-time scan can't
/// discover, because there isn't a single fixed instance - the count and content are
/// data-dependent. <see cref="LocatorTemplate"/> is raw XPath/CSS text containing the literal
/// token <c>{param}</c>, which <c>PageObjectCodeGenerator</c> turns into a real parameter on a
/// generated factory method instead of a fixed field. Only <see cref="StepAction.Click"/>,
/// <see cref="StepAction.Fill"/>, <see cref="StepAction.Select"/>, and
/// <see cref="StepAction.AssertText"/> are supported.
/// </summary>
public sealed record DynamicElement(
    string Id,
    string Name,
    StepAction Action,
    LocatorStrategy Strategy,
    string LocatorTemplate,
    // "int" or "string" - the type of the generated factory method's parameter.
    string ParameterType = "int",
    // Only meaningful when Action is Fill - same meaning as PageElement.DataType.
    string DataType = "string");

public sealed record PageDefinition(
    int SchemaVersion,
    string Id,
    string Name,
    string Url,
    IReadOnlyList<PageElement> Elements,
    DateTimeOffset UpdatedAt,
    // Nullable/no-default-collection so older persisted pages deserialize as null rather than
    // forcing every existing constructor call across the codebase to supply one - callers backfill
    // with `?? []` at the point of use, same pattern as ElementIdentificationSettings.IgnoreIdPatterns.
    IReadOnlyList<DynamicElement>? DynamicElements = null)
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
    int? TimeoutMs = null,
    // Set instead of ElementId when this step targets a DynamicElement; DynamicParameterValue is
    // the concrete value (e.g. "3") this step substitutes into that element's template.
    string? DynamicElementId = null,
    string? DynamicParameterValue = null);

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

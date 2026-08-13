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
    Name,
    Css,
    // The default camelCase JsonStringEnumConverter lowercases only the leading run of capitals
    // before a lowercase letter, so XPath would otherwise serialize as "xPath" - override it to
    // match the "xpath" string used as a strategy identifier everywhere else (the JS scanner,
    // settings.json's strategyOrder, the frontend's LocatorStrategy type).
#if NET9_0_OR_GREATER
    [JsonStringEnumMemberName("xpath")]
#endif
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
    bool CaptureScreenshotOnFailure,
    // How long a live run (SeleniumTestRunner only - never the generated NUnit code, which stays
    // unattended-CI fast) pauses on an outlined element before acting on it, so a human watching
    // the visible browser window can actually see what's about to happen.
    int HighlightPauseMs = 400);

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
    string DataType = "string",
    // Only meaningful for Select (a <select multiple> was scanned) - StepAction has no dedicated
    // MultiSelect value (same reasoning as Radio - see WorkspaceCodeGeneration.ToDescriptor), so
    // this is the only place that distinguishes a multi-select from a regular one after the scan.
    bool Multiple = false,
    // Only meaningful for Click (a checkbox was scanned) - StepAction has no dedicated Check
    // value either (a checkbox's Action is StepAction.Click, same as a button), so this is the
    // only place that survives to tell them apart after the scan.
    bool Checkbox = false);

public sealed record ScanReconciliationReport(
    int MatchedCount,
    int NewCount,
    int MissingCount,
    // True when the page had never been scanned before (nothing to reconcile against) - lets the
    // UI say "N elements found" instead of a matched/new/missing breakdown that's meaningless when
    // there was no previous scan to compare with. Set by the caller (PuppeteerPageDiscovery), not
    // ElementReconciler itself, which only ever sees an elements list, never whether a previous
    // PageDefinition existed at all versus existed-but-was-empty.
    bool IsFirstScan = false);

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

public enum ScanReviewDecision
{
    Wait,
    Accept,
    Deny,
}

public enum ScanReviewChangeKind
{
    New,
    Missing,
}

public enum ScanReviewStatus
{
    Pending,
    Applied,
    Discarded,
    Superseded,
}

public sealed record ScanReviewItem(
    string Id,
    ScanReviewChangeKind Kind,
    PageElement Element,
    ScanReviewDecision Decision = ScanReviewDecision.Wait);

public sealed record ScanReview(
    int SchemaVersion,
    string Id,
    string PageId,
    ScanReviewStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? CompletedAt,
    PageDefinition BaselinePage,
    PageDefinition ProposedPage,
    IReadOnlyList<ScanReviewItem> Items)
{
    public const int CurrentSchemaVersion = 1;
}

public sealed record StagedPageScanResult(
    PageDefinition ActivePage,
    ScanReconciliationReport Reconciliation,
    ScanReview? Review = null,
    IReadOnlyList<DynamicElementSuggestion>? DynamicElementSuggestions = null,
    string? ScreenshotPath = null);

public sealed record ScanReviewReference(string TestId, string StepId);

public sealed record ScanReviewApplyResult(ScanReview Review, PageDefinition Page);

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
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? DynamicElementId = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? DynamicParameterValue = null,
    // Set instead of Value when this is a Select step against a PageElement.Multiple element -
    // one selection per entry, uploaded together as a single array (see
    // ElementWrapper<string[]>.Upload). Unused, like Value, for every other action.
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    IReadOnlyList<StepValue>? Values = null);

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
/// generation could not safely fold into the current output - the file itself is left in place,
/// completely untouched, rather than being silently overwritten or the difference being dropped.
/// Two situations raise this, and in both <see cref="Path"/> is that same still-live file's own
/// location (never deleted, never copied elsewhere) - re-detected and re-raised on every repeat
/// generate call until the user actually resolves it via the VS extension's merge UI:
/// <list type="bullet">
/// <item>Same-name regeneration recovers fewer marked methods than the file's marker count
/// suggests it should have (see <c>CustomMethodExtractor.CountMarkerOccurrences</c>).</item>
/// <item>A page was renamed, so its Page Object now generates under a new class name - the old
/// file is deleted automatically if it has no marked methods, otherwise it's left in place.</item>
/// </list>
/// </summary>
public sealed record CustomMethodRecoveryWarning(
    string PageObjectName,
    int ExpectedCount,
    int RecoveredCount,
    string Path);

/// <summary>
/// One extracted (or best-effort-recovered) custom method offered to the user in the VS
/// extension's merge UI, alongside <see cref="CustomMethodRecoveryWarning"/>'s passive warning
/// signal aimed at the Electron frontend. <see cref="SourceText"/> is always the
/// full method block (attribute, doc comments, signature, body) regardless of
/// <see cref="HasSyntaxError"/> - a broken method still needs to be shown, just flagged.
/// </summary>
public sealed record CustomMethodCandidate(
    string Signature,
    string SourceText,
    bool HasSyntaxError,
    IReadOnlyList<string> SyntaxErrors);

/// <summary>
/// Raised alongside (not instead of) a <see cref="CustomMethodRecoveryWarning"/> for the same
/// page, whenever that page's old file has custom methods that need a human decision: either a
/// rename (which today carries nothing over automatically) or a same-name regeneration where
/// recovery came up short. <see cref="Candidates"/> includes both freshly-detected methods and
/// any already-deferred ones from the global waiting list for this same page.
/// </summary>
public sealed record PendingCustomMethodMerge(
    string PageId,
    string PageObjectName,
    IReadOnlyList<CustomMethodCandidate> Candidates);

/// <summary>
/// A custom method the user chose to defer rather than decide on immediately - persisted in the
/// global (cross-page) waiting list at <c>.cliq/pending-custom-methods.json</c> until the user
/// acts on it again (include it in a future generate, or explicitly discard it).
/// </summary>
public sealed record PendingCustomMethodEntry(
    string Id,
    string PageId,
    string PageObjectName,
    string Signature,
    string SourceText,
    bool HasSyntaxError,
    DateTimeOffset DeferredAt);

public sealed record GenerationResult(
    IReadOnlyList<GeneratedArtifact> Artifacts,
    IReadOnlyList<CustomMethodRecoveryWarning> Warnings,
    IReadOnlyList<PendingCustomMethodMerge> PendingMerges);

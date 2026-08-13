using Cliq.Domain;

namespace Cliq.Api;

public sealed record OpenWorkspaceRequest(string RootPath);
public sealed record ScanPageRequest(string Url, string Name, BrowserKind? Browser = null, bool Headless = true);
public sealed record ScanPageResponse(
    PageDefinition Page,
    ScanReconciliationReport Reconciliation,
    LocatorDiffReport? LocatorDiff = null,
    IReadOnlyList<DynamicElementSuggestion>? DynamicElementSuggestions = null,
    string? ScreenshotPath = null);
public sealed record ScanReviewDecisionRequest(string ItemId, ScanReviewDecision Decision);
public sealed record UpdateScanReviewDecisionsRequest(IReadOnlyList<ScanReviewDecisionRequest> Decisions);
public sealed record GenerationRequest(
    IReadOnlyList<string>? PageIds = null,
    IReadOnlyList<string>? TestIds = null,
    IReadOnlyDictionary<string, IReadOnlyList<string>>? CustomMethodResolutions = null,
    // Per page, the custom methods the user chose to permanently discard rather than carry over
    // or defer to the waiting list - distinct from CustomMethodResolutions (which lists what's
    // carried over): anything neither resolved nor discarded still defaults to deferred, same as
    // before this existed.
    IReadOnlyDictionary<string, IReadOnlyList<string>>? CustomMethodDiscards = null);
public sealed record StartRunRequest(BrowserKind? Browser = null);
public sealed record LocateFromSourceRequest(string ClassName, string LocatorComment, BrowserKind? Browser = null);
public sealed record GeneratedPathResponse(string Path);

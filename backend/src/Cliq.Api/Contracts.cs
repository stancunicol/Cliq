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
public sealed record GenerationRequest(
    IReadOnlyList<string>? PageIds = null,
    IReadOnlyList<string>? TestIds = null,
    IReadOnlyDictionary<string, IReadOnlyList<string>>? CustomMethodResolutions = null);
public sealed record StartRunRequest(BrowserKind? Browser = null);
public sealed record LocateFromSourceRequest(string ClassName, string LocatorComment, BrowserKind? Browser = null);
public sealed record GeneratedPathResponse(string Path);

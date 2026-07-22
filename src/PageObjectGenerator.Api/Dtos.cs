namespace PageObjectGenerator.Api;

public sealed record GeneratePageObjectRequest(
    string Url,
    string? Browser = "chrome",
    bool Headless = true);

public sealed record GeneratePageObjectResponse(
    string ClassName,
    int ElementCount,
    string PageObjectCode,
    string TestCode,
    string PageObjectPath,
    string TestPath,
    string CustomPath,
    bool CustomFileCreated,
    string? ScreenshotUrl);

public sealed record GeneratedPageSummary(string ClassName);

public sealed record GeneratedPageDetail(string ClassName, string PageObjectCode, string? TestCode, string? CustomCode);

public sealed record RunPageObjectRequest(
    bool Headless = false,
    Dictionary<string, string>? Values = null);

public sealed record ApiStepResult(
    string Name, string Kind, string Selector, bool Passed, string? Error, string? ScreenshotUrl);

public sealed record ApiRunResult(
    string ClassName,
    string Url,
    int TotalSteps,
    int CompletedSteps,
    bool StoppedDueToNavigation,
    string? BaselineScreenshotUrl,
    IReadOnlyList<ApiStepResult> Steps);

public sealed record ApiElementDescriptor(
    string Tag,
    string Kind,
    string Strategy,
    string LocatorValue,
    string Selector,
    string SuggestedName,
    string? ExpectedText,
    string? GroupName);

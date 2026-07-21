using PageObjectGenerator.Core;

namespace PageObjectGenerator.Api;

/// <summary>
/// Adapts Core's host-agnostic types (which the CLI also consumes, and which
/// report real filesystem paths and numeric enums) into API-friendly shapes -
/// fetchable screenshot URLs instead of server-local paths, string enum names
/// instead of numbers.
/// </summary>
internal static class ResponseMapping
{
    public static string? ToScreenshotUrl(string? absolutePath) =>
        absolutePath is null ? null : $"/api/screenshots/{Uri.EscapeDataString(Path.GetFileName(absolutePath))}";

    public static ApiStepResult ToApi(StepResult step) =>
        new(step.Name, step.Kind.ToString(), step.Selector, step.Passed, step.Error, ToScreenshotUrl(step.ScreenshotPath));

    public static ApiRunResult ToApi(RunResult result) =>
        new(
            result.ClassName,
            result.Url,
            result.TotalSteps,
            result.CompletedSteps,
            result.StoppedDueToNavigation,
            ToScreenshotUrl(result.BaselineScreenshotPath),
            result.Steps.Select(ToApi).ToList());

    public static ApiElementDescriptor ToApi(ElementDescriptor element) =>
        new(
            element.Tag,
            element.Kind.ToString(),
            element.Strategy.ToString(),
            element.LocatorValue,
            element.Selector,
            element.SuggestedName,
            element.ExpectedText,
            element.GroupName);
}

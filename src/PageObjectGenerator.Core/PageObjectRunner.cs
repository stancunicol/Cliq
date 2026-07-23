using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace PageObjectGenerator.Core;

public sealed record StepResult(
    string Name, ElementActionKind Kind, string Selector, bool Passed, string? Error, string? ScreenshotPath = null);

public sealed record RunResult(
    string ClassName,
    string Url,
    int TotalSteps,
    int CompletedSteps,
    bool StoppedDueToNavigation,
    string? BaselineScreenshotPath,
    IReadOnlyList<StepResult> Steps);

/// <summary>
/// Re-opens a previously generated page and actually performs each discovered
/// action (fill/click/select/check/...) live, non-headless by default, with a
/// short pause between steps so a human can watch it happen. Unlike the
/// generated NUnit smoke tests, this exercises the page instead of just
/// checking presence. A failed step (e.g. a decorative element that isn't
/// really clickable, even though it reports as displayed) doesn't stop the
/// run - the remaining steps still target the same page and are worth trying.
/// Only a click that actually navigates the page away stops the sequence,
/// since every selector after that was resolved against a DOM that's gone.
/// Every failed step is screenshotted (best-effort) so a human reviewing the
/// run afterward can see what the page actually looked like at that point.
/// Also captures a "before" screenshot at the start of each run - it can't show
/// drift on its own (it always matches "now"), but paired with the fixed
/// generation-time screenshot (see PageObjectWorkflow) it lets a human compare
/// "when this Page Object was built" against "right before this specific run".
/// The whole run is synchronous/blocking (it's driving a real browser); the
/// optional onStep callback is how a caller gets progress before the final
/// RunResult - e.g. the API's WebSocket endpoint runs this on a background
/// thread and forwards each step over the socket as onStep fires.
/// </summary>
public static class PageObjectRunner
{
    private const string DefaultFillValue = "test";
    private static readonly TimeSpan StepDelay = TimeSpan.FromMilliseconds(600);

    public static RunResult Run(
        GenerationMetadata metadata,
        bool headless,
        IReadOnlyDictionary<string, string>? values,
        string? screenshotDir = null,
        Action<StepResult>? onStep = null)
    {
        screenshotDir ??= SolutionPaths.DefaultScreenshotDirectory;

        using var driver = BrowserFactory.Create(metadata.Browser, headless);
        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
        var steps = new List<StepResult>();
        var stoppedDueToNavigation = false;

        string? baselineScreenshotPath;

        try
        {
            driver.Navigate().GoToUrl(metadata.Url);
            PageReadiness.WaitForComplete(driver);

            // Reference image of the page as it looked before any step ran - a
            // stable baseline that later runs can be compared against by eye,
            // independent of whatever this particular run ends up changing.
            baselineScreenshotPath = ScreenshotCapture.TryCapture(driver, screenshotDir, $"{metadata.ClassName}_run_start");

            foreach (var element in metadata.Elements)
            {
                var urlBefore = driver.Url;
                var step = RunStep(driver, wait, element, values, screenshotDir);
                steps.Add(step);
                onStep?.Invoke(step);
                Thread.Sleep(StepDelay);

                if (step.Passed && element.Kind == ElementActionKind.Click && driver.Url != urlBefore)
                {
                    stoppedDueToNavigation = true;
                    break;
                }
            }

            Thread.Sleep(StepDelay);
        }
        finally
        {
            driver.Quit();
        }

        return new RunResult(
            metadata.ClassName, metadata.Url, metadata.Elements.Count, steps.Count(s => s.Passed),
            stoppedDueToNavigation, baselineScreenshotPath, steps);
    }

    private static StepResult RunStep(
        IWebDriver driver, WebDriverWait wait, ElementDescriptor element, IReadOnlyDictionary<string, string>? values, string screenshotDir)
    {
        var result = TryRunStep(driver, wait, element, values);
        if (result.Passed)
        {
            return result;
        }

        return result with { ScreenshotPath = ScreenshotCapture.TryCapture(driver, screenshotDir, element.SuggestedName) };
    }

    private static StepResult TryRunStep(
        IWebDriver driver, WebDriverWait wait, ElementDescriptor element, IReadOnlyDictionary<string, string>? values)
    {
        try
        {
            var found = wait.Until(d => d.FindElement(BuildLocator(element)));
            var value = values is not null && values.TryGetValue(element.SuggestedName, out var v) ? v : null;

            switch (element.Kind)
            {
                case ElementActionKind.Fill:
                    found.Clear();
                    found.SendKeys(value ?? DefaultFillValue);
                    break;

                case ElementActionKind.Click:
                    found.Click();
                    break;

                case ElementActionKind.Select:
                    var select = new SelectElement(found);
                    if (value is not null)
                    {
                        select.SelectByValue(value);
                    }
                    else if (select.Options.Count > 0)
                    {
                        select.SelectByIndex(0);
                    }

                    break;

                case ElementActionKind.MultiSelect:
                    var multiSelect = new SelectElement(found);
                    if (value is not null)
                    {
                        multiSelect.DeselectAll();
                        foreach (var v2 in value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                        {
                            multiSelect.SelectByValue(v2);
                        }
                    }
                    else if (multiSelect.Options.Count > 0)
                    {
                        multiSelect.SelectByIndex(0);
                    }

                    break;

                case ElementActionKind.Check:
                    var shouldCheck = value is null || !string.Equals(value, "false", StringComparison.OrdinalIgnoreCase);
                    if (found.Selected != shouldCheck)
                    {
                        found.Click();
                    }

                    break;

                case ElementActionKind.Radio:
                    if (value is not null)
                    {
                        var optionSelector = "input[name='" + PageObjectCodeGenerator.CssEscapeSingleQuoted(element.LocatorValue) +
                            "'][value='" + PageObjectCodeGenerator.CssEscapeSingleQuoted(value) + "']";
                        driver.FindElement(By.CssSelector(optionSelector)).Click();
                    }
                    else
                    {
                        found.Click();
                    }

                    break;

                case ElementActionKind.Assert:
                    var expectedText = element.ExpectedText ?? "";
                    if (!string.Equals(found.Text, expectedText, StringComparison.Ordinal))
                    {
                        return new StepResult(element.SuggestedName, element.Kind, element.Selector, false,
                            $"Expected text '{expectedText}' but found '{found.Text}'.");
                    }

                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(element));
            }

            return new StepResult(element.SuggestedName, element.Kind, element.Selector, true, null);
        }
        catch (Exception ex)
        {
            // Broad catch is intentional: any Selenium failure (not found, stale,
            // not interactable, timed out) should surface as a failed step in the
            // run report, not crash the whole run.
            return new StepResult(element.SuggestedName, element.Kind, element.Selector, false, ex.Message);
        }
    }

    private static By BuildLocator(ElementDescriptor element) => element.Strategy switch
    {
        LocatorStrategy.Id => By.Id(element.LocatorValue),
        LocatorStrategy.Name => By.Name(element.LocatorValue),
        LocatorStrategy.CssSelector => By.CssSelector(element.LocatorValue),
        _ => throw new ArgumentOutOfRangeException(nameof(element)),
    };
}

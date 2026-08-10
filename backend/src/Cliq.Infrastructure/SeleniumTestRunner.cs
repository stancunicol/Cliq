using System.Diagnostics;
using Cliq.Application;
using Cliq.Domain;
using Cliq.Infrastructure.Generation;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using DomainBrowser = Cliq.Domain.BrowserKind;
using DomainLocator = Cliq.Domain.LocatorStrategy;
using DomainStepResult = Cliq.Domain.StepResult;
using GenerationBrowser = Cliq.Infrastructure.Generation.BrowserKind;

namespace Cliq.Infrastructure;

public sealed class SeleniumTestRunner(WorkspaceRepository repository) : ITestRunner
{
    public async Task<TestRun> RunAsync(
        string runId,
        TestCase test,
        IReadOnlyDictionary<string, PageDefinition> pages,
        DomainBrowser browser,
        IReadOnlyDictionary<string, string> secrets,
        IProgress<DomainStepResult> progress,
        CancellationToken cancellationToken)
    {
        var settings = await repository.GetSettingsAsync(cancellationToken);
        return await Task.Run(
            () => Run(runId, test, pages, browser, secrets, settings, progress, cancellationToken),
            cancellationToken);
    }

    private TestRun Run(
        string runId,
        TestCase test,
        IReadOnlyDictionary<string, PageDefinition> pages,
        DomainBrowser browser,
        IReadOnlyDictionary<string, string> secrets,
        CliqSettings settings,
        IProgress<DomainStepResult> progress,
        CancellationToken cancellationToken)
    {
        var started = DateTimeOffset.UtcNow;
        var results = new List<DomainStepResult>();
        using var driver = BrowserFactory.Create(
            browser == DomainBrowser.Edge ? GenerationBrowser.Edge : GenerationBrowser.Chrome,
            headless: false);

        try
        {
            foreach (var step in test.Steps.Where(item => item.Enabled))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var stopwatch = Stopwatch.StartNew();
                DomainStepResult result;
                try
                {
                    var timing = ExecuteStep(driver, step, pages, secrets, settings);
                    result = new DomainStepResult(
                        step.Id,
                        RunStatus.Passed,
                        stopwatch.ElapsedMilliseconds,
                        LocateMs: timing.LocateMs,
                        ActionMs: timing.ActionMs);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    var screenshot = settings.TestExecution.CaptureScreenshotOnFailure
                        ? CaptureFailure(driver, runId, step, pages, secrets, settings)
                        : null;
                    result = new DomainStepResult(
                        step.Id,
                        RunStatus.Failed,
                        stopwatch.ElapsedMilliseconds,
                        exception.Message,
                        screenshot);
                }

                results.Add(result);
                progress.Report(result);
            }
        }
        finally
        {
            driver.Quit();
        }

        var failed = results.Any(result => result.Status == RunStatus.Failed);
        return new TestRun(
            runId,
            test.Id,
            failed ? RunStatus.Failed : RunStatus.Passed,
            started,
            DateTimeOffset.UtcNow,
            results,
            failed ? "One or more test steps failed." : null);
    }

    private static StepTiming ExecuteStep(
        IWebDriver driver,
        TestStep step,
        IReadOnlyDictionary<string, PageDefinition> pages,
        IReadOnlyDictionary<string, string> secrets,
        CliqSettings settings)
    {
        if (step.Action == StepAction.Navigate)
        {
            driver.Navigate().GoToUrl(step.Url ?? throw new WorkspaceValidationException("Navigate step has no URL."));
            PageReadiness.WaitForComplete(driver, TimeSpan.FromMilliseconds(settings.TestExecution.PageReadyTimeoutMs));
            return new StepTiming(null, null);
        }

        if (step.Action == StepAction.AssertUrl)
        {
            var expected = ResolveValue(step.Value, secrets);
            if (!System.Text.RegularExpressions.Regex.IsMatch(driver.Url, expected))
            {
                throw new InvalidOperationException($"URL '{driver.Url}' does not match '{expected}'.");
            }

            return new StepTiming(null, null);
        }

        var timeout = TimeSpan.FromMilliseconds(step.TimeoutMs is > 0 ? step.TimeoutMs.Value : settings.TestExecution.DefaultStepTimeoutMs);
        var testIdAttribute = settings.ElementIdentification.TestIdAttribute;

        // A Radio group has no single element to locate up front, unlike every other kind: its
        // PageElement.Locator names the *group* (LocatorStrategy.Name, the shared `name`
        // attribute), and which specific <input> to act on depends on the step's own value - not
        // something SelectElement (Selenium's <select> wrapper, used for the case below) can do.
        // Mirrors PageObjectCodeGenerator.AppendMethod's Radio case, which builds the same
        // name+value selector for the same reason.
        if (step.Action == StepAction.Select && step.DynamicElementId is null)
        {
            var candidate = ResolveElement(step, pages);
            if (candidate.Locator.Strategy == DomainLocator.Name)
            {
                return ExecuteRadioSelect(driver, candidate, step, secrets, timeout);
            }
        }

        var (locator, elementName) = ResolveLocator(step, pages, testIdAttribute);

        var locateStopwatch = Stopwatch.StartNew();
        var webElement = new WebDriverWait(driver, timeout).Until(current => current.FindElement(locator));
        var locateMs = locateStopwatch.ElapsedMilliseconds;

        var actionStopwatch = Stopwatch.StartNew();
        switch (step.Action)
        {
            case StepAction.Click:
                webElement.Click();
                break;
            case StepAction.Fill:
                webElement.Clear();
                webElement.SendKeys(ResolveValue(step.Value, secrets));
                break;
            case StepAction.Select:
                new SelectElement(webElement).SelectByValue(ResolveValue(step.Value, secrets));
                break;
            case StepAction.PressKey:
                webElement.SendKeys(ResolveValue(step.Value, secrets));
                break;
            case StepAction.AssertVisible:
                if (!webElement.Displayed)
                {
                    throw new InvalidOperationException($"Element '{elementName}' is not visible.");
                }
                break;
            case StepAction.AssertText:
                var expected = ResolveValue(step.Value, secrets);
                if (!string.Equals(webElement.Text, expected, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException($"Expected text '{expected}' but found '{webElement.Text}'.");
                }
                break;
        }

        return new StepTiming(locateMs, actionStopwatch.ElapsedMilliseconds);
    }

    private static StepTiming ExecuteRadioSelect(
        IWebDriver driver,
        PageElement element,
        TestStep step,
        IReadOnlyDictionary<string, string> secrets,
        TimeSpan timeout)
    {
        var value = ResolveValue(step.Value, secrets);
        var locator = By.CssSelector(RadioOptionCssSelector(element.Locator.Value, value));

        var locateStopwatch = Stopwatch.StartNew();
        var webElement = new WebDriverWait(driver, timeout).Until(current => current.FindElement(locator));
        var locateMs = locateStopwatch.ElapsedMilliseconds;

        var actionStopwatch = Stopwatch.StartNew();
        webElement.Click();
        return new StepTiming(locateMs, actionStopwatch.ElapsedMilliseconds);
    }

    /// <summary>Selects one option out of a Radio group by combining its shared `name` with the
    /// option's own `value` - mirrors PageObjectCodeGenerator.AppendMethod's Radio case exactly,
    /// so a live run and the generated NUnit test locate the same option the same way. Internal
    /// (rather than folded inline into ExecuteRadioSelect) so the escaping can be unit-tested
    /// without a real WebDriver.</summary>
    internal static string RadioOptionCssSelector(string groupName, string value) =>
        $"input[name=\"{CssEscape(groupName)}\"][value=\"{CssEscape(value)}\"]";

    private readonly record struct StepTiming(long? LocateMs, long? ActionMs);

    /// <summary>Resolves a step to a locator and a display name for error messages - either a
    /// fixed <see cref="PageElement"/>, or (when <see cref="TestStep.DynamicElementId"/> is set)
    /// a <see cref="DynamicElement"/> template with its parameter substituted in at run time.
    /// Both funnel through the same <see cref="ByFor"/> so the rest of <see cref="ExecuteStep"/>
    /// doesn't need to know which kind it got.</summary>
    private static (By Locator, string Name) ResolveLocator(
        TestStep step,
        IReadOnlyDictionary<string, PageDefinition> pages,
        string testIdAttribute)
    {
        if (step.DynamicElementId is not null)
        {
            return ResolveDynamicLocator(step, pages, testIdAttribute);
        }

        var element = ResolveElement(step, pages);
        return (ByFor(element.Locator, testIdAttribute), element.Name);
    }

    private static PageElement ResolveElement(
        TestStep step,
        IReadOnlyDictionary<string, PageDefinition> pages)
    {
        if (step.PageId is null || !pages.TryGetValue(step.PageId, out var page))
        {
            throw new WorkspaceValidationException($"Step '{step.Id}' has no valid page reference.");
        }

        return page.Elements.SingleOrDefault(element => element.Id == step.ElementId)
            ?? throw new WorkspaceValidationException($"Step '{step.Id}' has no valid element reference.");
    }

    private static (By Locator, string Name) ResolveDynamicLocator(
        TestStep step,
        IReadOnlyDictionary<string, PageDefinition> pages,
        string testIdAttribute)
    {
        if (step.PageId is null || !pages.TryGetValue(step.PageId, out var page))
        {
            throw new WorkspaceValidationException($"Step '{step.Id}' has no valid page reference.");
        }

        var dynamicElement = (page.DynamicElements ?? []).SingleOrDefault(item => item.Id == step.DynamicElementId)
            ?? throw new WorkspaceValidationException($"Step '{step.Id}' has no valid dynamic element reference.");

        var paramValue = step.DynamicParameterValue
            ?? throw new WorkspaceValidationException($"Step '{step.Id}' has no dynamic parameter value.");

        var resolvedValue = dynamicElement.LocatorTemplate.Replace("{param}", paramValue);
        return (ByFor(new Locator(dynamicElement.Strategy, resolvedValue), testIdAttribute), dynamicElement.Name);
    }

    // Only the three strategies discovery actually produces for a regular element - see
    // elementIdentification.strategyOrder (testId, xpath) plus the always-available css fallback
    // in extract-elements.js's locatorFor. Id/Name are never assigned to a regular element's
    // Locator.Strategy (id is excluded as a locator entirely, kept only as a naming hint); Name is
    // assigned only to a Radio group's Locator, which live execution doesn't resolve through here
    // at all - see the StepAction.Select case in ExecuteStep, which is Selenium's <select> wrapper
    // and isn't wired up for a Radio input group regardless of what this method returns for it.
    private static By ByFor(Locator locator, string testIdAttribute) => locator.Strategy switch
    {
        DomainLocator.TestId => By.CssSelector($"[{testIdAttribute}=\"{CssEscape(locator.Value)}\"]"),
        DomainLocator.XPath => By.XPath(locator.Value),
        DomainLocator.Css => By.CssSelector(locator.Value),
        _ => throw new WorkspaceValidationException(
            $"Locator strategy '{locator.Strategy}' is not supported for live execution - only testId, xpath, and css are."),
    };

    private static string ResolveValue(
        StepValue? value,
        IReadOnlyDictionary<string, string> secrets)
    {
        if (value is null)
        {
            return string.Empty;
        }

        if (value.Kind != "secret")
        {
            return value.Value;
        }

        return secrets.TryGetValue(value.Value, out var secret)
            ? secret
            : throw new WorkspaceValidationException($"Secret '{value.Value}' is not configured.");
    }

    private string? CaptureFailure(
        IWebDriver driver,
        string runId,
        TestStep step,
        IReadOnlyDictionary<string, PageDefinition> pages,
        IReadOnlyDictionary<string, string> secrets,
        CliqSettings settings)
    {
        var root = repository.RootPath;
        if (root is null)
        {
            return null;
        }

        var directory = Path.Combine(root, ".cliq", "runs", runId);
        var failedElement = TryFindFailedElement(driver, step, pages, secrets, settings);
        var path = ScreenshotCapture.TryCapture(driver, directory, step.Id, failedElement);
        return path is null ? null : Path.GetFileName(path);
    }

    private static IWebElement? TryFindFailedElement(
        IWebDriver driver,
        TestStep step,
        IReadOnlyDictionary<string, PageDefinition> pages,
        IReadOnlyDictionary<string, string> secrets,
        CliqSettings settings)
    {
        try
        {
            if (step.Action is StepAction.Navigate or StepAction.AssertUrl)
            {
                return null;
            }

            By locator;
            if (step.Action == StepAction.Select && step.DynamicElementId is null)
            {
                var element = ResolveElement(step, pages);
                locator = element.Locator.Strategy == DomainLocator.Name
                    ? By.CssSelector(RadioOptionCssSelector(
                        element.Locator.Value,
                        ResolveValue(step.Value, secrets)))
                    : ResolveLocator(
                        step,
                        pages,
                        settings.ElementIdentification.TestIdAttribute).Locator;
            }
            else
            {
                locator = ResolveLocator(
                    step,
                    pages,
                    settings.ElementIdentification.TestIdAttribute).Locator;
            }

            return driver.FindElements(locator).FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }

    private static string CssEscape(string value) =>
        value.Replace("\\", "\\\\").Replace("\"", "\\\"");
}

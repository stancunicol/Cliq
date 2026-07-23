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
    public Task<TestRun> RunAsync(
        TestCase test,
        IReadOnlyDictionary<string, PageDefinition> pages,
        DomainBrowser browser,
        IReadOnlyDictionary<string, string> secrets,
        IProgress<DomainStepResult> progress,
        CancellationToken cancellationToken) =>
        Task.Run(() => Run(test, pages, browser, secrets, progress, cancellationToken), cancellationToken);

    private TestRun Run(
        TestCase test,
        IReadOnlyDictionary<string, PageDefinition> pages,
        DomainBrowser browser,
        IReadOnlyDictionary<string, string> secrets,
        IProgress<DomainStepResult> progress,
        CancellationToken cancellationToken)
    {
        var runId = Guid.NewGuid().ToString("N");
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
                    ExecuteStep(driver, step, pages, secrets);
                    result = new DomainStepResult(step.Id, RunStatus.Passed, stopwatch.ElapsedMilliseconds);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    var screenshot = CaptureFailure(driver, runId, step.Id);
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

    private static void ExecuteStep(
        IWebDriver driver,
        TestStep step,
        IReadOnlyDictionary<string, PageDefinition> pages,
        IReadOnlyDictionary<string, string> secrets)
    {
        if (step.Action == StepAction.Navigate)
        {
            driver.Navigate().GoToUrl(step.Url ?? throw new WorkspaceValidationException("Navigate step has no URL."));
            PageReadiness.WaitForComplete(driver);
            return;
        }

        if (step.Action == StepAction.AssertUrl)
        {
            var expected = ResolveValue(step.Value, secrets);
            if (!System.Text.RegularExpressions.Regex.IsMatch(driver.Url, expected))
            {
                throw new InvalidOperationException($"URL '{driver.Url}' does not match '{expected}'.");
            }

            return;
        }

        var element = ResolveElement(step, pages);
        var timeout = TimeSpan.FromMilliseconds(step.TimeoutMs is > 0 ? step.TimeoutMs.Value : 10_000);
        var webElement = new WebDriverWait(driver, timeout).Until(current => current.FindElement(ByFor(element.Locator)));
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
                    throw new InvalidOperationException($"Element '{element.Name}' is not visible.");
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

    private static By ByFor(Locator locator) => locator.Strategy switch
    {
        DomainLocator.TestId => By.CssSelector($"[data-testid=\"{CssEscape(locator.Value)}\"]"),
        DomainLocator.Id => By.Id(locator.Value),
        DomainLocator.Name => By.Name(locator.Value),
        DomainLocator.XPath => By.XPath(locator.Value),
        _ => By.CssSelector(locator.Value),
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

    private string? CaptureFailure(IWebDriver driver, string runId, string stepId)
    {
        var root = repository.RootPath;
        if (root is null)
        {
            return null;
        }

        var directory = Path.Combine(root, ".cliq", "runs", runId);
        return ScreenshotCapture.TryCapture(driver, directory, stepId);
    }

    private static string CssEscape(string value) =>
        value.Replace("\\", "\\\\").Replace("\"", "\\\"");
}

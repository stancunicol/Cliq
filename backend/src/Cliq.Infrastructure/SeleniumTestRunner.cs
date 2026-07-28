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
                    var healed = ExecuteStep(driver, step, pages, secrets, settings, cancellationToken);
                    result = new DomainStepResult(step.Id, RunStatus.Passed, stopwatch.ElapsedMilliseconds, SelfHealed: healed);
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    var screenshot = settings.TestExecution.CaptureScreenshotOnFailure
                        ? CaptureFailure(driver, runId, step.Id)
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

    private bool ExecuteStep(
        IWebDriver driver,
        TestStep step,
        IReadOnlyDictionary<string, PageDefinition> pages,
        IReadOnlyDictionary<string, string> secrets,
        CliqSettings settings,
        CancellationToken cancellationToken)
    {
        if (step.Action == StepAction.Navigate)
        {
            driver.Navigate().GoToUrl(step.Url ?? throw new WorkspaceValidationException("Navigate step has no URL."));
            PageReadiness.WaitForComplete(driver, TimeSpan.FromMilliseconds(settings.TestExecution.PageReadyTimeoutMs));
            return false;
        }

        if (step.Action == StepAction.AssertUrl)
        {
            var expected = ResolveValue(step.Value, secrets);
            if (!System.Text.RegularExpressions.Regex.IsMatch(driver.Url, expected))
            {
                throw new InvalidOperationException($"URL '{driver.Url}' does not match '{expected}'.");
            }

            return false;
        }

        var element = ResolveElement(step, pages);
        var timeout = TimeSpan.FromMilliseconds(step.TimeoutMs is > 0 ? step.TimeoutMs.Value : settings.TestExecution.DefaultStepTimeoutMs);
        var testIdAttribute = settings.ElementIdentification.TestIdAttribute;

        IWebElement webElement;
        var healed = false;
        try
        {
            webElement = new WebDriverWait(driver, timeout).Until(current => current.FindElement(ByFor(element.Locator, testIdAttribute)));
        }
        catch (WebDriverTimeoutException)
        {
            var healedLocator = SelfHealingLocator.TryHeal(driver, element, settings.ElementIdentification);
            if (healedLocator is null)
            {
                throw;
            }

            webElement = driver.FindElement(ByFor(healedLocator, testIdAttribute));
            PersistHealedLocator(step, pages, element, healedLocator, cancellationToken);
            healed = true;
        }

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

        return healed;
    }

    private void PersistHealedLocator(
        TestStep step,
        IReadOnlyDictionary<string, PageDefinition> pages,
        PageElement element,
        Locator healedLocator,
        CancellationToken cancellationToken)
    {
        if (step.PageId is null || !pages.TryGetValue(step.PageId, out var page))
        {
            return;
        }

        var updatedElements = page.Elements
            .Select(candidate => candidate.Id == element.Id ? candidate with { Locator = healedLocator } : candidate)
            .ToList();
        var updatedPage = page with { Elements = updatedElements };
        repository.SavePageAsync(updatedPage, cancellationToken).GetAwaiter().GetResult();
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

    private static By ByFor(Locator locator, string testIdAttribute) => locator.Strategy switch
    {
        DomainLocator.TestId => By.CssSelector($"[{testIdAttribute}=\"{CssEscape(locator.Value)}\"]"),
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

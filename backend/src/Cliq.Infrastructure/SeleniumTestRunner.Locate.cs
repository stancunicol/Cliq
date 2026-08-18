using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Cliq.Application;
using Cliq.Domain;
using Cliq.Infrastructure.Generation;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using DomainBrowser = Cliq.Domain.BrowserKind;
using GenerationBrowser = Cliq.Infrastructure.Generation.BrowserKind;

namespace Cliq.Infrastructure;

/// <summary>
/// The interactive "locate one element, highlight it, and leave the browser window open" feature -
/// distinct from the unattended full-test-run path in SeleniumTestRunner.cs, sharing only ByFor and
/// the other locator-resolution helpers defined there.
/// </summary>
public sealed partial class SeleniumTestRunner
{
    // Holds every driver LocateElementAsync hands back to the user without closing, so nothing
    // garbage-collects (and potentially finalizer-Quit()s) it once the method returns and its
    // local variable goes out of scope. Deliberately never drained - each call that finds its
    // element leaves one more open window until the user closes it themselves or the sidecar
    // process exits, which is the whole point of the feature.
    private static readonly ConcurrentBag<IWebDriver> KeptAliveDrivers = new();

    public async Task LocateElementAsync(
        PageDefinition page,
        string elementId,
        DomainBrowser browser,
        CancellationToken cancellationToken)
    {
        var settings = await repository.GetSettingsAsync(cancellationToken);
        await Task.Run(() => LocateElement(page, elementId, browser, settings), cancellationToken);
    }

    private static void LocateElement(
        PageDefinition page,
        string elementId,
        DomainBrowser browser,
        CliqSettings settings)
    {
        var element = page.Elements.SingleOrDefault(candidate => candidate.Id == elementId)
            ?? throw new WorkspaceValidationException($"Page '{page.Id}' has no element '{elementId}'.");

        var driver = BrowserFactory.Create(
            browser == DomainBrowser.Edge ? GenerationBrowser.Edge : GenerationBrowser.Chrome,
            headless: false);
        try
        {
            driver.Navigate().GoToUrl(page.Url);
            PageReadiness.WaitForComplete(driver, TimeSpan.FromMilliseconds(settings.TestExecution.PageReadyTimeoutMs));

            var locator = ByFor(element.Locator, settings.ElementIdentification.TestIdAttribute);
            var timeoutMs = settings.TestExecution.DefaultStepTimeoutMs;
            IWebElement webElement;
            try
            {
                webElement = new WebDriverWait(driver, TimeSpan.FromMilliseconds(timeoutMs))
                    .Until(current => current.FindElement(locator));
            }
            catch (WebDriverTimeoutException)
            {
                throw new WorkspaceValidationException(
                    $"Element '{element.Name}' was not found on '{page.Url}' within {timeoutMs} ms.");
            }

            ScreenshotCapture.AddHighlight(driver, webElement, "Cliq", "#2979ff");
            KeptAliveDrivers.Add(driver);
        }
        catch
        {
            driver.Quit();
            throw;
        }
    }

    private static readonly Regex LocatorCommentPattern = new(
        @"^\s*//\s*Locator\s*\((?<label>\w+)(?:,\s*(?<template>template))?\)\s*:\s*(?<value>.+?)\s*$",
        RegexOptions.Compiled);

    public async Task LocateElementFromSourceAsync(
        string className,
        string locatorComment,
        DomainBrowser browser,
        CancellationToken cancellationToken)
    {
        var settings = await repository.GetSettingsAsync(cancellationToken);
        var pages = await repository.ListPagesAsync(cancellationToken);
        var page = pages.FirstOrDefault(candidate =>
            string.Equals(IdentifierNaming.ToPageObjectClassName(candidate.Name), className, StringComparison.Ordinal))
            ?? throw new WorkspaceValidationException($"No page generates the class '{className}'.");

        var element = ResolveElementFromComment(page, locatorComment, settings.ElementIdentification.TestIdAttribute);
        await Task.Run(() => LocateElement(page, element.Id, browser, settings), cancellationToken);
    }

    /// <summary>
    /// Parses a <c>// Locator (...): ...</c> comment (as written by
    /// <see cref="PageObjectCodeGenerator"/>) and finds the element on <paramref name="page"/> it
    /// was generated from, by re-deriving the same rendered (label, value) text every element
    /// would produce via <see cref="WorkspaceCodeGeneration.ToDescriptor"/> and comparing - kept
    /// separate from <see cref="LocateElementFromSourceAsync"/> so it's testable without a live
    /// browser.
    /// </summary>
    internal static PageElement ResolveElementFromComment(PageDefinition page, string locatorComment, string testIdAttribute)
    {
        var match = LocatorCommentPattern.Match(locatorComment.Trim());
        if (!match.Success)
        {
            throw new WorkspaceValidationException("Selected text is not a recognizable Cliq locator comment.");
        }

        if (match.Groups["template"].Success)
        {
            throw new WorkspaceValidationException(
                "Dynamic element locators need a parameter value and can't be located directly from source.");
        }

        var label = match.Groups["label"].Value;
        var value = match.Groups["value"].Value;
        return page.Elements.FirstOrDefault(candidate =>
        {
            var descriptor = WorkspaceCodeGeneration.ToDescriptor(candidate, testIdAttribute);
            return PageObjectCodeGenerator.StrategyLabel(descriptor.Strategy) == label
                && PageObjectCodeGenerator.SingleLine(descriptor.LocatorValue) == value;
        }) ?? throw new WorkspaceValidationException($"No element on page '{page.Name}' matches that locator.");
    }
}

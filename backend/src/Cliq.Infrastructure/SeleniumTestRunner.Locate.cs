using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Cliq.Application;
using Cliq.Domain;
using Cliq.Infrastructure.Generation;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using DomainBrowser = Cliq.Domain.BrowserKind;
using DomainLocatorStrategy = Cliq.Domain.LocatorStrategy;
using GenerationBrowser = Cliq.Infrastructure.Generation.BrowserKind;

namespace Cliq.Infrastructure;

/// <summary>
/// The interactive "locate one element and highlight it in an already-open, reused browser
/// session" feature - a DevTools-inspector feel (pick a different locator, the same tab
/// re-highlights) rather than a fresh window per call. Distinct from the unattended full-test-run
/// path in SeleniumTestRunner.cs, sharing only ByFor and the other locator-resolution helpers
/// defined there.
/// </summary>
public sealed partial class SeleniumTestRunner
{
    // One driver per page, reused across repeated locate calls on that same page instead of
    // opening a new window every time - keyed by page id so a stale session (the user closed the
    // window, or the browser crashed) is detected and replaced rather than reused blindly. Never
    // proactively closed otherwise: the window is meant to stay open, under the caller's control,
    // for as long as they keep inspecting that page. Concurrent locate calls on the very same page
    // (effectively impossible from a single IDE selection at a time) could in principle race here
    // and leak one extra window; accepted the same way this feature already accepts "never closes
    // a window on its own" as a tradeoff.
    private static readonly ConcurrentDictionary<string, IWebDriver> KeptAliveDriversByPageId = new();

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

        var driver = GetOrCreateSessionDriver(page, browser, settings);

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
            // Deliberately does NOT quit/evict the session here (unlike a failure to create or
            // navigate it below) - the browser window itself is fine, only this particular
            // locator didn't match right now, and the whole point of reuse is that picking a
            // different locator next should re-highlight in the same window, not reopen it.
            throw new WorkspaceValidationException(
                $"Element '{element.Name}' was not found on '{page.Url}' within {timeoutMs} ms.");
        }

        ScreenshotCapture.ClearHighlights(driver);
        ScreenshotCapture.AddHighlight(driver, webElement, "Cliq", "#2979ff");
    }

    /// <summary>
    /// Returns the page's already-open session driver if it's still alive, otherwise creates a
    /// fresh one (navigated and ready) and stores it for the next call. Only a failure at this
    /// stage - creating the browser, navigating, waiting for the page to be ready - quits the
    /// driver and leaves nothing pooled; a session that's already up and merely fails to find one
    /// particular element (handled by the caller) stays pooled exactly as it was.
    /// </summary>
    private static IWebDriver GetOrCreateSessionDriver(PageDefinition page, DomainBrowser browser, CliqSettings settings)
    {
        if (KeptAliveDriversByPageId.TryGetValue(page.Id, out var existing))
        {
            if (IsSessionAlive(existing))
            {
                return existing;
            }

            KeptAliveDriversByPageId.TryRemove(page.Id, out _);
            TryQuitBestEffort(existing);
        }

        var driver = BrowserFactory.Create(
            browser == DomainBrowser.Edge ? GenerationBrowser.Edge : GenerationBrowser.Chrome,
            headless: false);
        try
        {
            driver.Navigate().GoToUrl(page.Url);
            PageReadiness.WaitForComplete(driver, TimeSpan.FromMilliseconds(settings.TestExecution.PageReadyTimeoutMs));
        }
        catch
        {
            driver.Quit();
            throw;
        }

        KeptAliveDriversByPageId[page.Id] = driver;
        return driver;
    }

    // IWebDriver has no "is this still connected" property of its own - probing a cheap,
    // side-effect-free one (window handles) is the standard way to notice the user closed the
    // window (or the browser process died) before trying to reuse it.
    private static bool IsSessionAlive(IWebDriver driver)
    {
        try
        {
            _ = driver.WindowHandles;
            return true;
        }
        catch (WebDriverException)
        {
            return false;
        }
    }

    private static void TryQuitBestEffort(IWebDriver driver)
    {
        try
        {
            driver.Quit();
        }
        catch (WebDriverException)
        {
            // Already gone - nothing to clean up.
        }
    }

    private const string HoverProbeAttribute = "data-cliq-hover-probe";
    private static readonly TimeSpan HoverPollInterval = TimeSpan.FromMilliseconds(250);

    /// <inheritdoc cref="ITestRunner.StreamHoveredElementAsync"/>
    public async IAsyncEnumerable<string?> StreamHoveredElementAsync(
        PageDefinition page,
        DomainBrowser browser,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var settings = await repository.GetSettingsAsync(cancellationToken);
        string? lastReported = null;
        var hasReported = false;
        while (!cancellationToken.IsCancellationRequested)
        {
            var matched = await Task.Run(() => PollHoveredElement(page, browser, settings), cancellationToken);
            if (!hasReported || matched != lastReported)
            {
                hasReported = true;
                lastReported = matched;
                yield return matched;
            }

            await Task.Delay(HoverPollInterval, cancellationToken);
        }
    }

    /// <summary>
    /// One poll tick: ensures the page's session driver has the hover tracker installed, reads
    /// whichever element it last tagged as hovered, and matches it against <paramref name="page"/>'s
    /// known elements. A <c>data-testid</c>-strategy element matches by a cheap attribute compare;
    /// every other strategy falls back to resolving its own locator and checking whether it
    /// resolves to the same live element the tracker tagged, since there's no single attribute to
    /// compare for an xpath/css/name locator. Returns null when nothing is hovered or the hovered
    /// element isn't one Cliq knows about.
    /// </summary>
    private static string? PollHoveredElement(PageDefinition page, DomainBrowser browser, CliqSettings settings)
    {
        var driver = GetOrCreateSessionDriver(page, browser, settings);
        EnsureHoverTrackerInstalled(driver);

        var hovered = driver.FindElements(By.CssSelector($"[{HoverProbeAttribute}]")).FirstOrDefault();
        if (hovered is null)
        {
            return null;
        }

        var testIdAttribute = settings.ElementIdentification.TestIdAttribute;
        var hoveredTestId = TryGetAttribute(hovered, testIdAttribute);
        if (hoveredTestId is not null)
        {
            var byTestId = page.Elements.FirstOrDefault(candidate =>
                candidate.Locator.Strategy == DomainLocatorStrategy.TestId
                && candidate.Locator.Value == hoveredTestId);
            if (byTestId is not null)
            {
                return byTestId.Id;
            }
        }

        foreach (var candidate in page.Elements.Where(candidate => candidate.Locator.Strategy != DomainLocatorStrategy.TestId))
        {
            var locator = ByFor(candidate.Locator, testIdAttribute);
            if (driver.FindElements(locator).Any(found => found.Equals(hovered)))
            {
                return candidate.Id;
            }
        }

        return null;
    }

    private static string? TryGetAttribute(IWebElement element, string attributeName)
    {
        try
        {
            return element.GetAttribute(attributeName);
        }
        catch (StaleElementReferenceException)
        {
            return null;
        }
    }

    // Idempotent (checks its own installed-flag first) so it's safe to call on every poll tick -
    // a full page reload wipes both the flag and the listener, so re-running this after one just
    // reinstalls it; a same-page SPA route change leaves `document` (and the flag) intact, so this
    // is a no-op. Capture-phase listeners so the innermost element under the cursor is tagged even
    // when an ancestor's own handler stops propagation.
    private static void EnsureHoverTrackerInstalled(IWebDriver driver)
    {
        if (driver is not IJavaScriptExecutor scriptExecutor)
        {
            return;
        }

        const string script = $$"""
            if (window.__cliqHoverTrackerInstalled) {
                return;
            }
            window.__cliqHoverTrackerInstalled = true;
            let current = null;
            document.addEventListener('mouseover', (event) => {
                if (current) {
                    current.removeAttribute('{{HoverProbeAttribute}}');
                }
                current = event.target;
                current.setAttribute('{{HoverProbeAttribute}}', 'true');
            }, true);
            document.addEventListener('mouseout', (event) => {
                if (current === event.target) {
                    current.removeAttribute('{{HoverProbeAttribute}}');
                    current = null;
                }
            }, true);
            """;

        try
        {
            scriptExecutor.ExecuteScript(script);
        }
        catch
        {
            // Best-effort - a transient failure here just means the next poll finds nothing hovered.
        }
    }

    private static readonly Regex LocatorCommentPattern = new(
        @"^\s*//\s*Locator\s*\((?<label>\w+)(?:,\s*(?<template>template))?\)\s*:\s*(?:(?<name>\w+)\s*=\s*)?(?<value>.+?)\s*$",
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
        var name = match.Groups["name"].Success ? match.Groups["name"].Value : null;
        return page.Elements.FirstOrDefault(candidate =>
        {
            var descriptor = WorkspaceCodeGeneration.ToDescriptor(candidate, testIdAttribute);
            if (PageObjectCodeGenerator.StrategyLabel(descriptor.Strategy) != label)
            {
                return false;
            }

            // A comment carries the locator's generated name (new format) and either its absolute
            // /html/body/... path (xpath elements) or its relative value (css/testId). Legacy
            // name-less comments carry the relative value alone. Match on whichever the comment
            // holds - name when present, then the absolute path, then the relative value - so both
            // the current and older generated files keep resolving.
            if (name is not null && string.Equals(
                    IdentifierNaming.ToPascalCase(descriptor.SuggestedName), name, StringComparison.Ordinal))
            {
                return true;
            }

            if (descriptor.AbsoluteXpath is not null
                && PageObjectCodeGenerator.SingleLine(descriptor.AbsoluteXpath) == value)
            {
                return true;
            }

            return PageObjectCodeGenerator.SingleLine(descriptor.LocatorValue) == value;
        }) ?? throw new WorkspaceValidationException($"No element on page '{page.Name}' matches that locator.");
    }
}

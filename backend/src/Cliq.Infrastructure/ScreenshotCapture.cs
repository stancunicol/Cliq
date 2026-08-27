using OpenQA.Selenium;

namespace Cliq.Infrastructure.Generation;

public static class ScreenshotCapture
{
    /// <summary>Best-effort: a screenshot failure should never break the caller's real work.</summary>
    public static string? TryCapture(
        IWebDriver driver,
        string screenshotDir,
        string label,
        IWebElement? failedElement = null)
    {
        var highlight = TryAddFailureHighlight(driver, failedElement);
        try
        {
            if (driver is not ITakesScreenshot screenshotDriver)
            {
                return null;
            }

            Directory.CreateDirectory(screenshotDir);
            var fileName = $"{label}_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png";
            var path = Path.Combine(screenshotDir, fileName);
            screenshotDriver.GetScreenshot().SaveAsFile(path);
            return path;
        }
        catch
        {
            return null;
        }
        finally
        {
            TryRemoveFailureHighlight(driver, highlight);
        }
    }

    private static IWebElement? TryAddFailureHighlight(
        IWebDriver driver,
        IWebElement? failedElement)
    {
        if (failedElement is null)
        {
            return null;
        }

        return AddHighlight(driver, failedElement, "FAILED", "#ff1744");
    }

    /// <summary>
    /// Scrolls <paramref name="element"/> into view and draws a bordered overlay box (plus a
    /// small text badge) around its current <c>getBoundingClientRect()</c> - the same visual
    /// language <see cref="TryAddFailureHighlight"/> uses for a failed step, just with the
    /// label/color pulled out so a caller that isn't reporting a failure (e.g. "locate this
    /// element for inspection") isn't stuck with a red "FAILED" badge. Unlike the
    /// failure path, callers of this overload are expected to leave the overlay in place rather
    /// than pairing it with <see cref="TryRemoveFailureHighlight"/> - there is no "temporary"
    /// framing here, the whole point is that it stays visible.
    /// </summary>
    public static IWebElement? AddHighlight(
        IWebDriver driver,
        IWebElement element,
        string label,
        string colorHex)
    {
        if (driver is not IJavaScriptExecutor scriptExecutor)
        {
            return null;
        }

        try
        {
            const string script = """
                const target = arguments[0];
                const color = arguments[1];
                const label = arguments[2];
                target.scrollIntoView({ block: 'center', inline: 'center' });

                const rect = target.getBoundingClientRect();
                const overlay = document.createElement('div');
                overlay.setAttribute('data-cliq-highlight', 'true');
                Object.assign(overlay.style, {
                    position: 'absolute',
                    left: `${Math.max(0, rect.left + window.scrollX - 4)}px`,
                    top: `${Math.max(0, rect.top + window.scrollY - 4)}px`,
                    width: `${Math.max(12, rect.width + 8)}px`,
                    height: `${Math.max(12, rect.height + 8)}px`,
                    border: `4px solid ${color}`,
                    background: `${color}2e`,
                    boxShadow: `0 0 0 3px rgba(255, 255, 255, 0.95), 0 0 16px ${color}e6`,
                    boxSizing: 'border-box',
                    pointerEvents: 'none',
                    zIndex: '2147483647'
                });

                const badge = document.createElement('div');
                badge.textContent = label;
                Object.assign(badge.style, {
                    position: 'absolute',
                    left: '-4px',
                    bottom: '100%',
                    marginBottom: '6px',
                    padding: '3px 7px',
                    borderRadius: '3px',
                    background: color,
                    color: '#ffffff',
                    font: 'bold 12px/1.4 sans-serif',
                    letterSpacing: '0.08em',
                    whiteSpace: 'nowrap'
                });
                overlay.appendChild(badge);
                document.documentElement.appendChild(overlay);
                return overlay;
                """;

            return scriptExecutor.ExecuteScript(script, element, colorHex, label) as IWebElement;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Removes every overlay <see cref="AddHighlight"/> has ever left in the page. Used before
    /// re-highlighting a different element in a reused, already-open session (see
    /// SeleniumTestRunner.Locate.cs's session reuse) so repeatedly locating different elements on
    /// the same page doesn't leave a stack of stale highlight boxes behind - best-effort, like
    /// every other DOM mutation this class performs, since the page may have navigated away
    /// between calls.
    /// </summary>
    public static void ClearHighlights(IWebDriver driver)
    {
        if (driver is not IJavaScriptExecutor scriptExecutor)
        {
            return;
        }

        try
        {
            scriptExecutor.ExecuteScript("document.querySelectorAll('[data-cliq-highlight]').forEach(el => el.remove());");
        }
        catch
        {
            // Best-effort, same as every other DOM mutation in this class.
        }
    }

    private static void TryRemoveFailureHighlight(
        IWebDriver driver,
        IWebElement? highlight)
    {
        if (highlight is null || driver is not IJavaScriptExecutor scriptExecutor)
        {
            return;
        }

        try
        {
            scriptExecutor.ExecuteScript("arguments[0].remove();", highlight);
        }
        catch
        {
            // The page may have navigated after the screenshot; cleanup remains best-effort.
        }
    }
}

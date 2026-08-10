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
        if (failedElement is null || driver is not IJavaScriptExecutor scriptExecutor)
        {
            return null;
        }

        try
        {
            const string script = """
                const target = arguments[0];
                target.scrollIntoView({ block: 'center', inline: 'center' });

                const rect = target.getBoundingClientRect();
                const overlay = document.createElement('div');
                overlay.setAttribute('data-cliq-failure-highlight', 'true');
                Object.assign(overlay.style, {
                    position: 'fixed',
                    left: `${Math.max(0, rect.left - 4)}px`,
                    top: `${Math.max(0, rect.top - 4)}px`,
                    width: `${Math.max(12, rect.width + 8)}px`,
                    height: `${Math.max(12, rect.height + 8)}px`,
                    border: '4px solid #ff1744',
                    background: 'rgba(255, 23, 68, 0.18)',
                    boxShadow: '0 0 0 3px rgba(255, 255, 255, 0.95), 0 0 16px rgba(255, 23, 68, 0.9)',
                    boxSizing: 'border-box',
                    pointerEvents: 'none',
                    zIndex: '2147483647'
                });

                const badge = document.createElement('div');
                badge.textContent = 'FAILED';
                Object.assign(badge.style, {
                    position: 'absolute',
                    left: '-4px',
                    bottom: '100%',
                    marginBottom: '6px',
                    padding: '3px 7px',
                    borderRadius: '3px',
                    background: '#ff1744',
                    color: '#ffffff',
                    font: 'bold 12px/1.4 sans-serif',
                    letterSpacing: '0.08em',
                    whiteSpace: 'nowrap'
                });
                overlay.appendChild(badge);
                document.documentElement.appendChild(overlay);
                return overlay;
                """;

            return scriptExecutor.ExecuteScript(script, failedElement) as IWebElement;
        }
        catch
        {
            return null;
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

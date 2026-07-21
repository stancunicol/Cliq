using OpenQA.Selenium;

namespace PageObjectGenerator.Core;

public static class ScreenshotCapture
{
    /// <summary>Best-effort: a screenshot failure should never break the caller's real work.</summary>
    public static string? TryCapture(IWebDriver driver, string screenshotDir, string label)
    {
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
    }
}

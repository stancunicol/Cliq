using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace PageObjectGenerator.Core;

/// <summary>
/// Selenium's Navigate().GoToUrl already blocks for the classic "load" event,
/// but that isn't a hard guarantee across every driver/browser combination, and
/// it says nothing about client-side rendering that keeps running after load
/// fires. Waiting on document.readyState explicitly makes "the page is ready"
/// an actual condition every navigation checks, not an assumption inherited
/// from whatever GoToUrl happened to do.
/// </summary>
internal static class PageReadiness
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

    public static void WaitForComplete(IWebDriver driver)
    {
        new WebDriverWait(driver, Timeout).Until(d =>
            ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState")?.Equals("complete") == true);
    }
}

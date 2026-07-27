using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace Cliq.Infrastructure.Generation;

internal static class PageReadiness
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(15);

    public static void WaitForComplete(IWebDriver driver)
    {
        new WebDriverWait(driver, Timeout).Until(d =>
            ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState")?.Equals("complete") == true);
    }
}

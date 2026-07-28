using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace Cliq.Infrastructure.Generation;

internal static class PageReadiness
{
    public static void WaitForComplete(IWebDriver driver, TimeSpan timeout)
    {
        new WebDriverWait(driver, timeout).Until(d =>
            ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState")?.Equals("complete") == true);
    }
}

using System.Collections.Generic;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;

namespace Generated.PageObjects;

public sealed partial class RecorderFixturePage
{
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;

    public RecorderFixturePage(IWebDriver driver)
    {
        _driver = driver;
        _wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
    }

    public void ClickIncrement counter()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("[data-testid=\"increment-counter\"]")));
        element.Click();
    }

    public void HoverIncrement counter()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("[data-testid=\"increment-counter\"]")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void FillDisplay name(string value)
    {
        var element = _wait.Until(d => d.FindElement(By.Id("display-name")));
        element.Clear();
        element.SendKeys(value);
    }
}

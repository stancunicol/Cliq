using System.Collections.Generic;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;

namespace Generated.PageObjects;

public sealed class RecorderFixturePage
{
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;

    // Locator (css): [data-testid="increment-counter"]
    private static readonly By Increment counterLocator = By.CssSelector("[data-testid=\"increment-counter\"]");

    // Locator (xpath): //output[@aria-live='polite']
    private static readonly By Counter outputLocator = By.XPath("//output[@aria-live='polite']");

    // Locator (xpath): //input[@id='display-name']
    private static readonly By Display nameLocator = By.XPath("//input[@id='display-name']");

    // Locator (xpath): //select[@id='role']
    private static readonly By RoleLocator = By.XPath("//select[@id='role']");

    // Locator (xpath): //input[@id='terms']
    private static readonly By Accept termsLocator = By.XPath("//input[@id='terms']");

    // Locator (xpath): //a[@href='/complete.html']
    private static readonly By Continue to completionLocator = By.XPath("//a[@href='/complete.html']");
    public ElementWrapper<string> Display name { get; }
    public ElementWrapper<string> Role { get; }

    public RecorderFixturePage(IWebDriver driver)
    {
        _driver = driver;
        _wait = new WebDriverWait(driver, TimeSpan.FromMilliseconds(10000));

        Display name = new ElementWrapper<string>(
            value =>
            {
                var element = _wait.Until(d => d.FindElement(Display nameLocator));
                element.Clear();
                element.SendKeys(value);
            },
            () =>
            {
                var element = _wait.Until(d => d.FindElement(Display nameLocator));
                return element.GetAttribute("value") ?? "";
            });

        Role = new ElementWrapper<string>(
            value =>
            {
                var element = _wait.Until(d => d.FindElement(RoleLocator));
                new SelectElement(element).SelectByValue(value);
            },
            () =>
            {
                var element = _wait.Until(d => d.FindElement(RoleLocator));
                return new SelectElement(element).SelectedOption.GetAttribute("value") ?? "";
            });
    }

    public void ClickIncrement counter()
    {
        var element = _wait.Until(d => d.FindElement(Increment counterLocator));
        element.Click();
    }

    public void HoverIncrement counter()
    {
        var element = _wait.Until(d => d.FindElement(Increment counterLocator));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public string GetCounter outputText()
    {
        var element = _wait.Until(d => d.FindElement(Counter outputLocator));
        return element.Text;
    }

    public void ClickAccept terms()
    {
        var element = _wait.Until(d => d.FindElement(Accept termsLocator));
        element.Click();
    }

    public void HoverAccept terms()
    {
        var element = _wait.Until(d => d.FindElement(Accept termsLocator));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickContinue to completion()
    {
        var element = _wait.Until(d => d.FindElement(Continue to completionLocator));
        element.Click();
    }

    public void HoverContinue to completion()
    {
        var element = _wait.Until(d => d.FindElement(Continue to completionLocator));
        new Actions(_driver).MoveToElement(element).Perform();
    }
}

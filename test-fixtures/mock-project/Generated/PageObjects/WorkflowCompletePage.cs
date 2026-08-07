using System.Collections.Generic;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;

namespace Generated.PageObjects;

public sealed class WorkflowCompletePage
{
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;

    // Locator (xpath): //h1[normalize-space()='Workflow complete']
    private static readonly By Completion titleLocator = By.XPath("//h1[normalize-space()='Workflow complete']");

    // Locator (xpath): //a[@href='/']
    private static readonly By Start overLocator = By.XPath("//a[@href='/']");

    public WorkflowCompletePage(IWebDriver driver)
    {
        _driver = driver;
        _wait = new WebDriverWait(driver, TimeSpan.FromMilliseconds(10000));
    }

    public string GetCompletion titleText()
    {
        var element = _wait.Until(d => d.FindElement(Completion titleLocator));
        return element.Text;
    }

    public void ClickStart over()
    {
        var element = _wait.Until(d => d.FindElement(Start overLocator));
        element.Click();
    }

    public void HoverStart over()
    {
        var element = _wait.Until(d => d.FindElement(Start overLocator));
        new Actions(_driver).MoveToElement(element).Perform();
    }
}

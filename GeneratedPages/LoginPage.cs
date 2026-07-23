using System.Collections.Generic;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;

namespace GeneratedPages;

public sealed partial class LoginPage
{
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;

    public LoginPage(IWebDriver driver)
    {
        _driver = driver;
        _wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
    }

    public string GetLoginPageText()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(2) > div:nth-child(2) > div:nth-child(1) > h2:nth-child(1)")));
        return element.Text;
    }

    public string GetThisIsWhereYouCanLogIntoTheSecurText()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(2) > div:nth-child(2) > div:nth-child(1) > h4:nth-child(2)")));
        return element.Text;
    }

    public void FillUsername(string value)
    {
        var element = _wait.Until(d => d.FindElement(By.Id("username")));
        element.Clear();
        element.SendKeys(value);
    }

    public void FillPassword(string value)
    {
        var element = _wait.Until(d => d.FindElement(By.Id("password")));
        element.Clear();
        element.SendKeys(value);
    }

    public void ClickLogin()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(2) > div:nth-child(2) > div:nth-child(1) > form:nth-child(3) > button:nth-child(3)")));
        element.Click();
    }

    public void HoverLogin()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(2) > div:nth-child(2) > div:nth-child(1) > form:nth-child(3) > button:nth-child(3)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickElementalSelenium()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverElementalSelenium()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }
}

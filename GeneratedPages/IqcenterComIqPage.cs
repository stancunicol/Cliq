using System.Collections.Generic;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;

namespace Generated;

public sealed partial class IqcenterComIqPage
{
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;

    public IqcenterComIqPage(IWebDriver driver)
    {
        _driver = driver;
        _wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
    }

    public void ClickA0()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(273) > header:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverA0()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(273) > header:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public string GetDiscoverYourText()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(273) > main:nth-child(3) > div:nth-child(1) > section:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > h1:nth-child(1)")));
        return element.Text;
    }

    public string GetIqScoreText()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(273) > main:nth-child(3) > div:nth-child(1) > section:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > h1:nth-child(2)")));
        return element.Text;
    }

    public string GetCompleteThis5MinuteIqTestToMeasurText()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(273) > main:nth-child(3) > div:nth-child(1) > section:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > h5:nth-child(2)")));
        return element.Text;
    }

    public string GetStartBySelectingYourGenderText()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(273) > main:nth-child(3) > div:nth-child(1) > section:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > h5:nth-child(3)")));
        return element.Text;
    }

    public void ClickMale()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(273) > main:nth-child(3) > div:nth-child(1) > section:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > button:nth-child(1)")));
        element.Click();
    }

    public void HoverMale()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(273) > main:nth-child(3) > div:nth-child(1) > section:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > button:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickFemale()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(273) > main:nth-child(3) > div:nth-child(1) > section:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > button:nth-child(2)")));
        element.Click();
    }

    public void HoverFemale()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(273) > main:nth-child(3) > div:nth-child(1) > section:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > button:nth-child(2)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public string GetHowItWorksText()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(273) > main:nth-child(3) > div:nth-child(1) > section:nth-child(2) > div:nth-child(1) > div:nth-child(1) > h3:nth-child(1)")));
        return element.Text;
    }

    public string GetH38Text()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(273) > main:nth-child(3) > div:nth-child(1) > section:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(1) > h3:nth-child(1)")));
        return element.Text;
    }

    public string GetPrepareYourselfText()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(273) > main:nth-child(3) > div:nth-child(1) > section:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > h5:nth-child(1)")));
        return element.Text;
    }

    public string GetH310Text()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(273) > main:nth-child(3) > div:nth-child(1) > section:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > div:nth-child(1) > h3:nth-child(1)")));
        return element.Text;
    }

    public string GetCompleteTheTestText()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(273) > main:nth-child(3) > div:nth-child(1) > section:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > div:nth-child(2) > h5:nth-child(1)")));
        return element.Text;
    }

    public string GetH312Text()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(273) > main:nth-child(3) > div:nth-child(1) > section:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(3) > div:nth-child(1) > h3:nth-child(1)")));
        return element.Text;
    }

    public string GetReceiveYourInsightsText()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(273) > main:nth-child(3) > div:nth-child(1) > section:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(3) > div:nth-child(2) > h5:nth-child(1)")));
        return element.Text;
    }

    public void ClickA14()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(273) > main:nth-child(3) > div:nth-child(1) > section:nth-child(2) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverA14()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(273) > main:nth-child(3) > div:nth-child(1) > section:nth-child(2) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickTermsOfService()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(273) > main:nth-child(3) > div:nth-child(1) > section:nth-child(2) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > div:nth-child(2) > p:nth-child(1) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverTermsOfService()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(273) > main:nth-child(3) > div:nth-child(1) > section:nth-child(2) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > div:nth-child(2) > p:nth-child(1) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickPrivacyPolicy()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(273) > main:nth-child(3) > div:nth-child(1) > section:nth-child(2) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > div:nth-child(2) > p:nth-child(1) > a:nth-child(2)")));
        element.Click();
    }

    public void HoverPrivacyPolicy()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(273) > main:nth-child(3) > div:nth-child(1) > section:nth-child(2) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > div:nth-child(2) > p:nth-child(1) > a:nth-child(2)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickContactUs()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(273) > main:nth-child(3) > div:nth-child(1) > section:nth-child(2) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > div:nth-child(2) > p:nth-child(1) > a:nth-child(3)")));
        element.Click();
    }

    public void HoverContactUs()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(273) > main:nth-child(3) > div:nth-child(1) > section:nth-child(2) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > div:nth-child(2) > p:nth-child(1) > a:nth-child(3)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickFaq()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(273) > main:nth-child(3) > div:nth-child(1) > section:nth-child(2) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > div:nth-child(2) > p:nth-child(1) > a:nth-child(4)")));
        element.Click();
    }

    public void HoverFaq()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(273) > main:nth-child(3) > div:nth-child(1) > section:nth-child(2) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > div:nth-child(2) > p:nth-child(1) > a:nth-child(4)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }
}

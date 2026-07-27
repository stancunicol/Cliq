using System.Collections.Generic;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;

namespace Generated.PageObjects;

public sealed partial class WwwEmagRoPage
{
    private readonly IWebDriver _driver;
    private readonly WebDriverWait _wait;

    public WwwEmagRoPage(IWebDriver driver)
    {
        _driver = driver;
        _wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
    }

    public void ClickDeschide()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverDeschide()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickA1()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > nav:nth-child(3) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverA1()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > nav:nth-child(3) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickA2()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > nav:nth-child(3) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > a:nth-child(2)")));
        element.Click();
    }

    public void HoverA2()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > nav:nth-child(3) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > a:nth-child(2)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void FillSearchboxautocomplete(string value)
    {
        var element = _wait.Until(d => d.FindElement(By.Id("searchboxAutocomplete")));
        element.Clear();
        element.SendKeys(value);
    }

    public void FillSearchboxtrigger(string value)
    {
        var element = _wait.Until(d => d.FindElement(By.Id("searchboxTrigger")));
        element.Clear();
        element.SendKeys(value);
    }

    public void ClickCaut()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > nav:nth-child(3) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > div:nth-child(1) > form:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(2)")));
        element.Click();
    }

    public void HoverCaut()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > nav:nth-child(3) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > div:nth-child(1) > form:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(2)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickMyWishlist()
    {
        var element = _wait.Until(d => d.FindElement(By.Id("my_wishlist")));
        element.Click();
    }

    public void HoverMyWishlist()
    {
        var element = _wait.Until(d => d.FindElement(By.Id("my_wishlist")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickMyCart()
    {
        var element = _wait.Until(d => d.FindElement(By.Id("my_cart")));
        element.Click();
    }

    public void HoverMyCart()
    {
        var element = _wait.Until(d => d.FindElement(By.Id("my_cart")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickA8()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > section:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverA8()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > section:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickA9()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > section:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverA9()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > section:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickA10()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > section:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverA10()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > section:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickA11()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > section:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverA11()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > section:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickA12()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > section:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(5) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverA12()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > section:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(5) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickA13()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > section:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(6) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverA13()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > section:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(6) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickA14()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > section:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverA14()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > section:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickA15()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > section:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > a:nth-child(2)")));
        element.Click();
    }

    public void HoverA15()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > section:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > a:nth-child(2)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickOferteleEmag()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(2) > div:nth-child(1) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverOferteleEmag()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(2) > div:nth-child(1) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickGenius()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(2) > div:nth-child(2) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverGenius()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(2) > div:nth-child(2) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickEasyBuyback()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(2) > div:nth-child(3) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverEasyBuyback()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(2) > div:nth-child(3) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickGeniusDeals()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(2) > div:nth-child(4) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverGeniusDeals()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(2) > div:nth-child(4) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickOfertaZilei()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(2) > div:nth-child(5) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverOfertaZilei()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(2) > div:nth-child(5) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickResigilate()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(2) > div:nth-child(6) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverResigilate()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(2) > div:nth-child(6) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public string GetProduseAlesePentruTineText()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(1) > h2:nth-child(1)")));
        return element.Text;
    }

    public void ClickButton23()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(1) > h2:nth-child(1) > button:nth-child(1)")));
        element.Click();
    }

    public void HoverButton23()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(1) > h2:nth-child(1) > button:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickButton24()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)")));
        element.Click();
    }

    public void HoverButton24()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickSuperPret()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverSuperPret()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public string GetMasinaDeTocatManualaStorcatorManuText()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1)")));
        return element.Text;
    }

    public void ClickMasinaDeTocatManualaStorcatorManu2()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverMasinaDeTocatManualaStorcatorManu2()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickDeReviewUri38()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > div:nth-child(2) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverDeReviewUri38()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > div:nth-child(2) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickAdaugaInCos()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)")));
        element.Click();
    }

    public void HoverAdaugaInCos()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickButton30()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)")));
        element.Click();
    }

    public void HoverButton30()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickSuperPret2()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverSuperPret2()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public string GetPentruFeteCeSeNtMplCuMineText()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1)")));
        return element.Text;
    }

    public void ClickPentruFeteCeSeNtMplCuMine2()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverPentruFeteCeSeNtMplCuMine2()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickDeReviewUri27()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > div:nth-child(2) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverDeReviewUri27()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > div:nth-child(2) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickAdaugaInCos2()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)")));
        element.Click();
    }

    public void HoverAdaugaInCos2()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickButton36()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(3) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)")));
        element.Click();
    }

    public void HoverButton36()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(3) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickMultiDeals()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(3) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverMultiDeals()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(3) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public string GetSet8InimiAmxeaPentruRacireaBauturiText()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(3) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1)")));
        return element.Text;
    }

    public void ClickSet8InimiAmxeaPentruRacireaBauturi2()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(3) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverSet8InimiAmxeaPentruRacireaBauturi2()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(3) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickDeReviewUri26()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(3) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > div:nth-child(2) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverDeReviewUri26()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(3) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > div:nth-child(2) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickAdaugaInCos3()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(3) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)")));
        element.Click();
    }

    public void HoverAdaugaInCos3()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(3) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickButton42()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(4) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)")));
        element.Click();
    }

    public void HoverButton42()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(4) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickSuperPret3()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(4) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverSuperPret3()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(4) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public string GetAnvelopaAllSeasonLaufennLh71GFit4Text()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(4) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1)")));
        return element.Text;
    }

    public void ClickAnvelopaAllSeasonLaufennLh71GFit42()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(4) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverAnvelopaAllSeasonLaufennLh71GFit42()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(4) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickDeReviewUri177()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(4) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > div:nth-child(2) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverDeReviewUri177()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(4) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > div:nth-child(2) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickAdaugaInCos4()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(4) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)")));
        element.Click();
    }

    public void HoverAdaugaInCos4()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(4) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickButton48()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(5) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)")));
        element.Click();
    }

    public void HoverButton48()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(5) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickA49()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(5) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverA49()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(5) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public string GetHusaTelefonCompatibilaCuHonor200SmText()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(5) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1)")));
        return element.Text;
    }

    public void ClickHusaTelefonCompatibilaCuHonor200Sm2()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(5) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverHusaTelefonCompatibilaCuHonor200Sm2()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(5) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickAdaugaInCos5()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(5) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)")));
        element.Click();
    }

    public void HoverAdaugaInCos5()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(5) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickButton53()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(6) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)")));
        element.Click();
    }

    public void HoverButton53()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(6) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickSuperPret4()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(6) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverSuperPret4()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(6) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickMichaelKorsCeasDamaLexingtonMk59()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(6) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverMichaelKorsCeasDamaLexingtonMk59()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(6) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public string GetMichaelKorsCeasDamaLexingtonMk592Text()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(6) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > a:nth-child(1) > h2:nth-child(1)")));
        return element.Text;
    }

    public void ClickAdaugaInCos6()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(6) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)")));
        element.Click();
    }

    public void HoverAdaugaInCos6()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(6) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickButton58()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(7) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)")));
        element.Click();
    }

    public void HoverButton58()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(7) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickA59()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(7) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverA59()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(7) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickSkechersPantofiSportDePlasaCuIns()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(7) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverSkechersPantofiSportDePlasaCuIns()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(7) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public string GetSkechersPantofiSportDePlasaCuIns2Text()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(7) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > a:nth-child(1) > h2:nth-child(1)")));
        return element.Text;
    }

    public void ClickAdaugaInCos7()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(7) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)")));
        element.Click();
    }

    public void HoverAdaugaInCos7()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(7) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickButton63()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(8) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)")));
        element.Click();
    }

    public void HoverButton63()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(8) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickElectroWeekendSuperPret()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(8) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverElectroWeekendSuperPret()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(8) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public string GetTelefonMobilAppleIphone17Pro256gbText()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(8) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1)")));
        return element.Text;
    }

    public void ClickTelefonMobilAppleIphone17Pro256gb2()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(8) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverTelefonMobilAppleIphone17Pro256gb2()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(8) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickDeReviewUri279()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(8) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > div:nth-child(2) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverDeReviewUri279()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(8) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > div:nth-child(2) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickAdaugaInCos8()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(8) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)")));
        element.Click();
    }

    public void HoverAdaugaInCos8()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(8) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickButton69()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(9) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)")));
        element.Click();
    }

    public void HoverButton69()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(9) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickTopFavorite()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(9) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverTopFavorite()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(9) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public string GetAparatDeAerConditionatYamatoOptimumText()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(9) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1)")));
        return element.Text;
    }

    public void ClickAparatDeAerConditionatYamatoOptimum2()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(9) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverAparatDeAerConditionatYamatoOptimum2()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(9) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickDeReviewUri362()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(9) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > div:nth-child(2) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverDeReviewUri362()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(9) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > div:nth-child(2) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickAdaugaInCos9()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(9) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)")));
        element.Click();
    }

    public void HoverAdaugaInCos9()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(9) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickButton75()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(10) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)")));
        element.Click();
    }

    public void HoverButton75()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(10) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickA76()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(10) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverA76()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(10) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public string GetFiltruUleiVolvoFmIii460Din2020KwText()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(10) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1)")));
        return element.Text;
    }

    public void ClickFiltruUleiVolvoFmIii460Din2020Kw2()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(10) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverFiltruUleiVolvoFmIii460Din2020Kw2()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(10) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickAdaugaInCos10()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(10) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)")));
        element.Click();
    }

    public void HoverAdaugaInCos10()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(10) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickButton80()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(11) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)")));
        element.Click();
    }

    public void HoverButton80()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(11) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickA81()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(11) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverA81()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(11) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public string GetAspiratorRobot2in1AspirareSiMopSText()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(11) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1)")));
        return element.Text;
    }

    public void ClickAspiratorRobot2in1AspirareSiMopS2()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(11) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverAspiratorRobot2in1AspirareSiMopS2()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(11) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickAdaugaInCos11()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(11) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)")));
        element.Click();
    }

    public void HoverAdaugaInCos11()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(11) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickButton85()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(12) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)")));
        element.Click();
    }

    public void HoverButton85()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(12) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickSuperPret5()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(12) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverSuperPret5()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(12) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public string GetTelevizorSamsungLed50u8092125CmSText()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(12) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1)")));
        return element.Text;
    }

    public void ClickTelevizorSamsungLed50u8092125CmS2()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(12) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverTelevizorSamsungLed50u8092125CmS2()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(12) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickDeReviewUri296()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(12) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > div:nth-child(2) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverDeReviewUri296()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(12) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > div:nth-child(2) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickAdaugaInCos12()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(12) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)")));
        element.Click();
    }

    public void HoverAdaugaInCos12()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(12) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickButton91()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(13) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)")));
        element.Click();
    }

    public void HoverButton91()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(13) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickA92()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(13) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverA92()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(13) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public string GetLaptopDellInspiron5505CuProcesorAmText()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(13) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1)")));
        return element.Text;
    }

    public void ClickLaptopDellInspiron5505CuProcesorAm2()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(13) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverLaptopDellInspiron5505CuProcesorAm2()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(13) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickAdaugaInCos13()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(13) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)")));
        element.Click();
    }

    public void HoverAdaugaInCos13()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(13) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickButton96()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(14) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)")));
        element.Click();
    }

    public void HoverButton96()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(14) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickMultiDeals2()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(14) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverMultiDeals2()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(14) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public string GetSetApaDeParfumSalvoBarbati100mlCuText()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(14) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1)")));
        return element.Text;
    }

    public void ClickSetApaDeParfumSalvoBarbati100mlCu2()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(14) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverSetApaDeParfumSalvoBarbati100mlCu2()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(14) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickAdaugaInCos14()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(14) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)")));
        element.Click();
    }

    public void HoverAdaugaInCos14()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(14) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickButton101()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(15) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)")));
        element.Click();
    }

    public void HoverButton101()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(15) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickVoucher25ExtraFashionWeek()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(15) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverVoucher25ExtraFashionWeek()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(15) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickSarytaRochieElegantaDeSearaLunga()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(15) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverSarytaRochieElegantaDeSearaLunga()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(15) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public string GetSarytaRochieElegantaDeSearaLunga2Text()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(15) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > a:nth-child(1) > h2:nth-child(1)")));
        return element.Text;
    }

    public void ClickAdaugaInCos15()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(15) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)")));
        element.Click();
    }

    public void HoverAdaugaInCos15()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(15) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickButton106()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(16) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)")));
        element.Click();
    }

    public void HoverButton106()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(16) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickMultiDealsSuperPret()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(16) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverMultiDealsSuperPret()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(16) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public string GetSetDeConstructieLegoCreator3In1Text()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(16) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1)")));
        return element.Text;
    }

    public void ClickSetDeConstructieLegoCreator3In12()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(16) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverSetDeConstructieLegoCreator3In12()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(16) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickReviewUri10()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(16) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > div:nth-child(2) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverReviewUri10()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(16) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > div:nth-child(2) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickAdaugaInCos16()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(16) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)")));
        element.Click();
    }

    public void HoverAdaugaInCos16()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(16) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickButton112()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(17) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)")));
        element.Click();
    }

    public void HoverButton112()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(17) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickTopFavorite2()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(17) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverTopFavorite2()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(17) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickOemBrataraModelTennis4mmPietre()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(17) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverOemBrataraModelTennis4mmPietre()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(17) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public string GetOemBrataraModelTennis4mmPietre2Text()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(17) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > a:nth-child(1) > h2:nth-child(1)")));
        return element.Text;
    }

    public void ClickAdaugaInCos17()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(17) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)")));
        element.Click();
    }

    public void HoverAdaugaInCos17()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(17) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickButton117()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(18) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)")));
        element.Click();
    }

    public void HoverButton117()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(18) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickGeniusDealsTopFavorite()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(18) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverGeniusDealsTopFavorite()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(18) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public string GetCeasSmartwatchSiBrataraFitness2in1Text()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(18) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1)")));
        return element.Text;
    }

    public void ClickCeasSmartwatchSiBrataraFitness2in12()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(18) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverCeasSmartwatchSiBrataraFitness2in12()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(18) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickDeReviewUri860()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(18) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > div:nth-child(2) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverDeReviewUri860()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(18) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > div:nth-child(2) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickAdaugaInCos18()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(18) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)")));
        element.Click();
    }

    public void HoverAdaugaInCos18()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(18) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickButton123()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(19) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)")));
        element.Click();
    }

    public void HoverButton123()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(19) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickTopFavorite3()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(19) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverTopFavorite3()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(19) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickImodixGeantaDamaDinPieleNaturala()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(19) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverImodixGeantaDamaDinPieleNaturala()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(19) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public string GetImodixGeantaDamaDinPieleNaturala2Text()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(19) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > a:nth-child(1) > h2:nth-child(1)")));
        return element.Text;
    }

    public void ClickAdaugaInCos19()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(19) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)")));
        element.Click();
    }

    public void HoverAdaugaInCos19()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(19) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickButton128()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(20) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)")));
        element.Click();
    }

    public void HoverButton128()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(20) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickA129()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(20) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverA129()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(20) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickAlexanderSmithTenisiDamaNegriPi()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(20) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverAlexanderSmithTenisiDamaNegriPi()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(20) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public string GetAlexanderSmithTenisiDamaNegriPi2Text()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(20) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > a:nth-child(1) > h2:nth-child(1)")));
        return element.Text;
    }

    public void ClickAdaugaInCos20()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(20) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)")));
        element.Click();
    }

    public void HoverAdaugaInCos20()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(20) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public string GetAboneazTeLaNewsletterIAflCuPriText()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > section:nth-child(4) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > h3:nth-child(1)")));
        return element.Text;
    }

    public void FillName(string value)
    {
        var element = _wait.Until(d => d.FindElement(By.Name("name")));
        element.Clear();
        element.SendKeys(value);
    }

    public void FillEmail(string value)
    {
        var element = _wait.Until(d => d.FindElement(By.Name("email")));
        element.Clear();
        element.SendKeys(value);
    }

    public void ClickMAbonez()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > section:nth-child(4) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > form:nth-child(3) > div:nth-child(1) > div:nth-child(3) > button:nth-child(1)")));
        element.Click();
    }

    public void HoverMAbonez()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > section:nth-child(4) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > form:nth-child(3) > div:nth-child(1) > div:nth-child(3) > button:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public string GetIstoriculTauDeNavigareText()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > section:nth-child(6) > div:nth-child(1) > div:nth-child(1) > h2:nth-child(1)")));
        return element.Text;
    }

    public string GetAltiVizitatoriAuFostInteresatiSiDeText()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > section:nth-child(6) > div:nth-child(1) > div:nth-child(3) > div:nth-child(1) > h2:nth-child(1)")));
        return element.Text;
    }

    public void ClickStergeIstoriculDeNavigare()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > section:nth-child(6) > div:nth-child(1) > div:nth-child(3) > div:nth-child(1) > a:nth-child(2)")));
        element.Click();
    }

    public void HoverStergeIstoriculDeNavigare()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > section:nth-child(6) > div:nth-child(1) > div:nth-child(3) > div:nth-child(1) > a:nth-child(2)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickServiciiPentruClienti()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > footer:nth-child(8) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverServiciiPentruClienti()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > footer:nth-child(8) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickComenziSiLivrare()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > footer:nth-child(8) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverComenziSiLivrare()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > footer:nth-child(8) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickSuportClienti()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > footer:nth-child(8) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverSuportClienti()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > footer:nth-child(8) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickEmagRo()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > footer:nth-child(8) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverEmagRo()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > footer:nth-child(8) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickDanteInternational()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > footer:nth-child(8) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverDanteInternational()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > footer:nth-child(8) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickVariantaMobile()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > footer:nth-child(8) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > a:nth-child(2)")));
        element.Click();
    }

    public void HoverVariantaMobile()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > footer:nth-child(8) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > a:nth-child(2)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickA146()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > footer:nth-child(8) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverA146()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > footer:nth-child(8) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickA147()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > footer:nth-child(8) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > a:nth-child(2)")));
        element.Click();
    }

    public void HoverA147()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > footer:nth-child(8) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > a:nth-child(2)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickA148()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > footer:nth-child(8) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > a:nth-child(3)")));
        element.Click();
    }

    public void HoverA148()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > footer:nth-child(8) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > a:nth-child(3)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickA149()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > footer:nth-child(8) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > a:nth-child(4)")));
        element.Click();
    }

    public void HoverA149()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > footer:nth-child(8) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > a:nth-child(4)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickFlipRo()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > footer:nth-child(8) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverFlipRo()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > footer:nth-child(8) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickFlipBg()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > footer:nth-child(8) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(2)")));
        element.Click();
    }

    public void HoverFlipBg()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > footer:nth-child(8) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(2)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickRejoyHu()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > footer:nth-child(8) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(3)")));
        element.Click();
    }

    public void HoverRejoyHu()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > footer:nth-child(8) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(3)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickEmagBg()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > footer:nth-child(8) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(4)")));
        element.Click();
    }

    public void HoverEmagBg()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > footer:nth-child(8) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(4)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickEmagHu()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > footer:nth-child(8) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(5)")));
        element.Click();
    }

    public void HoverEmagHu()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > footer:nth-child(8) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(5)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickA155()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > footer:nth-child(8) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(8)")));
        element.Click();
    }

    public void HoverA155()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > footer:nth-child(8) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(8)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickIntraInCont()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(26) > div:nth-child(1) > div:nth-child(2) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverIntraInCont()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(26) > div:nth-child(1) > div:nth-child(2) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickContNou()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(26) > div:nth-child(1) > div:nth-child(2) > a:nth-child(2)")));
        element.Click();
    }

    public void HoverContNou()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(26) > div:nth-child(1) > div:nth-child(2) > a:nth-child(2)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickPoliticaDeUtilizareCookieUri()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(29) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > p:nth-child(3) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverPoliticaDeUtilizareCookieUri()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(29) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > p:nth-child(3) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickAcceptToate()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(29) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)")));
        element.Click();
    }

    public void HoverAcceptToate()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(29) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickRefuzToate()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(29) > div:nth-child(1) > div:nth-child(2) > button:nth-child(2)")));
        element.Click();
    }

    public void HoverRefuzToate()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(29) > div:nth-child(1) > div:nth-child(2) > button:nth-child(2)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickAdministreazPreferinEle()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(29) > div:nth-child(1) > div:nth-child(2) > button:nth-child(3)")));
        element.Click();
    }

    public void HoverAdministreazPreferinEle()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(29) > div:nth-child(1) > div:nth-child(2) > button:nth-child(3)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }
}

using System.Collections.Generic;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;

namespace Generated;

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

    public void ClickSmartDeals()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverSmartDeals()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public string GetManaraEroticOracleChakraErosAndAText()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1)")));
        return element.Text;
    }

    public void ClickManaraEroticOracleChakraErosAndA2()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverManaraEroticOracleChakraErosAndA2()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickReviewUri2()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > div:nth-child(2) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverReviewUri2()
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

    public void ClickA31()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverA31()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public string GetPasatorDeRosiiHomeHghd1200ppCompatText()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1)")));
        return element.Text;
    }

    public void ClickPasatorDeRosiiHomeHghd1200ppCompat2()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverPasatorDeRosiiHomeHghd1200ppCompat2()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1)")));
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

    public void ClickButton35()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(3) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)")));
        element.Click();
    }

    public void HoverButton35()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(3) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickSmartDeals2()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(3) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverSmartDeals2()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(3) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public string GetVoalDeMireasaQttvbtna3x15mPoliesText()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(3) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1)")));
        return element.Text;
    }

    public void ClickVoalDeMireasaQttvbtna3x15mPolies2()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(3) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverVoalDeMireasaQttvbtna3x15mPolies2()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(3) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1)")));
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

    public void ClickButton40()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(4) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)")));
        element.Click();
    }

    public void HoverButton40()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(4) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickA41()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(4) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverA41()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(4) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public string GetSetDeWalkieTalkiesPentruCopii3BuText()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(4) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1)")));
        return element.Text;
    }

    public void ClickSetDeWalkieTalkiesPentruCopii3Bu2()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(4) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverSetDeWalkieTalkiesPentruCopii3Bu2()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(4) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1)")));
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

    public void ClickButton45()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(5) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)")));
        element.Click();
    }

    public void HoverButton45()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(5) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickOfertaZilei2()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(5) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverOfertaZilei2()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(5) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public string GetFolieDeVinilPentruSpateCompatibilCText()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(5) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1)")));
        return element.Text;
    }

    public void ClickFolieDeVinilPentruSpateCompatibilC2()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(5) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverFolieDeVinilPentruSpateCompatibilC2()
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

    public void ClickButton50()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(6) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)")));
        element.Click();
    }

    public void HoverButton50()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(6) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickSuperPret()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(6) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverSuperPret()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(6) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickPumaPantofiSportFlatformDePieleM()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(6) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverPumaPantofiSportFlatformDePieleM()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(6) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public string GetPumaPantofiSportFlatformDePieleM2Text()
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

    public void ClickButton55()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(7) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)")));
        element.Click();
    }

    public void HoverButton55()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(7) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickA56()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(7) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverA56()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(7) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickOemGeantaDamaMicaNodPapionAlba()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(7) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverOemGeantaDamaMicaNodPapionAlba()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(7) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public string GetOemGeantaDamaMicaNodPapionAlba2Text()
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

    public void ClickButton60()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(8) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)")));
        element.Click();
    }

    public void HoverButton60()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(8) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickA61()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(8) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverA61()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(8) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public string GetOukitelC62688128gb4gb5150mahMovText()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(8) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1)")));
        return element.Text;
    }

    public void ClickOukitelC62688128gb4gb5150mahMov2()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(8) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverOukitelC62688128gb4gb5150mahMov2()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(8) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1)")));
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

    public void ClickButton65()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(9) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)")));
        element.Click();
    }

    public void HoverButton65()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(9) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickA66()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(9) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverA66()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(9) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public string GetHusaBestcaseSlimGlassyCompatibilaText()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(9) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1)")));
        return element.Text;
    }

    public void ClickHusaBestcaseSlimGlassyCompatibila2()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(9) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverHusaBestcaseSlimGlassyCompatibila2()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(9) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1)")));
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

    public void ClickButton70()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(10) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)")));
        element.Click();
    }

    public void HoverButton70()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(10) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickMultiDeals()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(10) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverMultiDeals()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(10) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public string GetUnitateInternAerCondiIonatTipCaseText()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(10) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1)")));
        return element.Text;
    }

    public void ClickUnitateInternAerCondiIonatTipCase2()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(10) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverUnitateInternAerCondiIonatTipCase2()
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

    public void ClickButton75()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(11) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)")));
        element.Click();
    }

    public void HoverButton75()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(11) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickA76()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(11) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverA76()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(11) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public string GetFiltruUleiVolvoFhIi540Din2012KwText()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(11) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1)")));
        return element.Text;
    }

    public void ClickFiltruUleiVolvoFhIi540Din2012Kw2()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(11) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverFiltruUleiVolvoFhIi540Din2012Kw2()
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

    public void ClickButton80()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(12) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)")));
        element.Click();
    }

    public void HoverButton80()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(12) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickA81()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(12) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverA81()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(12) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public string GetAspiratorRobotInteligentNegru7090Text()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(12) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1)")));
        return element.Text;
    }

    public void ClickAspiratorRobotInteligentNegru70902()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(12) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverAspiratorRobotInteligentNegru70902()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(12) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1)")));
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

    public void ClickButton85()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(13) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)")));
        element.Click();
    }

    public void HoverButton85()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(13) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickA86()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(13) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverA86()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(13) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public string GetTelevizorQledSmartHisense50e7qUltrText()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(13) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1)")));
        return element.Text;
    }

    public void ClickTelevizorQledSmartHisense50e7qUltr2()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(13) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverTelevizorQledSmartHisense50e7qUltr2()
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

    public void ClickButton90()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(14) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)")));
        element.Click();
    }

    public void HoverButton90()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(14) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickSuperPret2()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(14) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverSuperPret2()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(14) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public string GetLaptopAppleMacbookPro14CuProcesorText()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(14) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1)")));
        return element.Text;
    }

    public void ClickLaptopAppleMacbookPro14CuProcesor2()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(14) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverLaptopAppleMacbookPro14CuProcesor2()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(14) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickDeReviewUri21()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(14) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > div:nth-child(2) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverDeReviewUri21()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(14) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > div:nth-child(2) > a:nth-child(1)")));
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

    public void ClickButton96()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(15) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)")));
        element.Click();
    }

    public void HoverButton96()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(15) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickMultiDeals2()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(15) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverMultiDeals2()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(15) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public string GetApaDeParfumLuxeromBlueIcon100mlText()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(15) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1)")));
        return element.Text;
    }

    public void ClickApaDeParfumLuxeromBlueIcon100ml2()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(15) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverApaDeParfumLuxeromBlueIcon100ml2()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(15) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
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

    public void ClickButton101()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(16) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)")));
        element.Click();
    }

    public void HoverButton101()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(16) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickA102()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(16) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverA102()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(16) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickBaldininiPantofiSportWedgeCuGarni()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(16) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverBaldininiPantofiSportWedgeCuGarni()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(16) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public string GetBaldininiPantofiSportWedgeCuGarni2Text()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(16) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > a:nth-child(1) > h2:nth-child(1)")));
        return element.Text;
    }

    public void ClickVeziDetalii()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(16) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverVeziDetalii()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(16) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickButton106()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(17) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)")));
        element.Click();
    }

    public void HoverButton106()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(17) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickSuperPret3()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(17) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverSuperPret3()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(17) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickMaxCoRochieEvazataCuManeciScurte()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(17) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverMaxCoRochieEvazataCuManeciScurte()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(17) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public string GetMaxCoRochieEvazataCuManeciScurte2Text()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(17) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > a:nth-child(1) > h2:nth-child(1)")));
        return element.Text;
    }

    public void ClickAdaugaInCos16()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(17) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)")));
        element.Click();
    }

    public void HoverAdaugaInCos16()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(17) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickButton111()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(18) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)")));
        element.Click();
    }

    public void HoverButton111()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(18) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickSuperPret4()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(18) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverSuperPret4()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(18) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public string GetSetDeConstructie353PiesePistolBeText()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(18) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1)")));
        return element.Text;
    }

    public void ClickSetDeConstructie353PiesePistolBe2()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(18) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverSetDeConstructie353PiesePistolBe2()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(18) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickAdaugaInCos17()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(18) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)")));
        element.Click();
    }

    public void HoverAdaugaInCos17()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(18) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickButton116()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(19) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)")));
        element.Click();
    }

    public void HoverButton116()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(19) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickCadouSuperPret()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(19) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverCadouSuperPret()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(19) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickSuperTrendsBrataraTennisDinArgint()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(19) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverSuperTrendsBrataraTennisDinArgint()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(19) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public string GetSuperTrendsBrataraTennisDinArgint2Text()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(19) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > a:nth-child(1) > h2:nth-child(1)")));
        return element.Text;
    }

    public void ClickAdaugaInCos18()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(19) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)")));
        element.Click();
    }

    public void HoverAdaugaInCos18()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(19) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickButton121()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(20) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)")));
        element.Click();
    }

    public void HoverButton121()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(20) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > button:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickSuperPret5()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(20) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverSuperPret5()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(20) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public string GetAppleWatchSe2025GpsCarcasaStarText()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(20) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1)")));
        return element.Text;
    }

    public void ClickAppleWatchSe2025GpsCarcasaStar2()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(20) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverAppleWatchSe2025GpsCarcasaStar2()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(20) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > h2:nth-child(1) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickDeReviewUri87()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(20) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > div:nth-child(2) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverDeReviewUri87()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(20) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > div:nth-child(2) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickAdaugaInCos19()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > div:nth-child(5) > main:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > div:nth-child(20) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > div:nth-child(2) > form:nth-child(1) > button:nth-child(2)")));
        element.Click();
    }

    public void HoverAdaugaInCos19()
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

    public void ClickA140()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > footer:nth-child(8) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverA140()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > footer:nth-child(8) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickA141()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > footer:nth-child(8) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > a:nth-child(2)")));
        element.Click();
    }

    public void HoverA141()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > footer:nth-child(8) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > a:nth-child(2)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickA142()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > footer:nth-child(8) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > a:nth-child(3)")));
        element.Click();
    }

    public void HoverA142()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > footer:nth-child(8) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > a:nth-child(3)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickA143()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > footer:nth-child(8) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > a:nth-child(4)")));
        element.Click();
    }

    public void HoverA143()
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

    public void ClickA149()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(10) > footer:nth-child(8) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > a:nth-child(8)")));
        element.Click();
    }

    public void HoverA149()
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

    public void ClickVeziOferta()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(33) > div:nth-child(1) > a:nth-child(2)")));
        element.Click();
    }

    public void HoverVeziOferta()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(33) > div:nth-child(1) > a:nth-child(2)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }

    public void ClickPrecomandAcumNoileSamsungGalaxyVez()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(33) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        element.Click();
    }

    public void HoverPrecomandAcumNoileSamsungGalaxyVez()
    {
        var element = _wait.Until(d => d.FindElement(By.CssSelector("div:nth-child(33) > div:nth-child(1) > div:nth-child(3) > a:nth-child(1)")));
        new Actions(_driver).MoveToElement(element).Perform();
    }
}

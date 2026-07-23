using System.Collections.Generic;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Edge;

namespace GeneratedPages;

[TestFixture]
public sealed class LoginPageTests
{
    private IWebDriver _driver = null!;

    [OneTimeSetUp]
    public void VerifyLoginPage()
    {
        using var driver = new EdgeDriver();
        driver.Navigate().GoToUrl("https://the-internet.herokuapp.com/login");
        var missing = new List<string>();

        if (!TryFind(driver, By.CssSelector("div:nth-child(2) > div:nth-child(2) > div:nth-child(1) > h2:nth-child(1)"))) missing.Add("login_page (div:nth-child(2) > div:nth-child(2) > div:nth-child(1) > h2:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(2) > div:nth-child(2) > div:nth-child(1) > h4:nth-child(2)"))) missing.Add("this_is_where_you_can_log_into_the_secur (div:nth-child(2) > div:nth-child(2) > div:nth-child(1) > h4:nth-child(2))");
        if (!TryFind(driver, By.Id("username"))) missing.Add("username (#username)");
        if (!TryFind(driver, By.Id("password"))) missing.Add("password (#password)");
        if (!TryFind(driver, By.CssSelector("div:nth-child(2) > div:nth-child(2) > div:nth-child(1) > form:nth-child(3) > button:nth-child(3)"))) missing.Add("login (div:nth-child(2) > div:nth-child(2) > div:nth-child(1) > form:nth-child(3) > button:nth-child(3))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > a:nth-child(1)"))) missing.Add("elemental_selenium (div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > a:nth-child(1))");

        if (missing.Count > 0)
        {
            Assert.Fail("Page Object verification failed - missing element(s): " + string.Join(", ", missing));
        }
    }

    private static bool TryFind(IWebDriver driver, By by)
    {
        try
        {
            driver.FindElement(by);
            return true;
        }
        catch (NoSuchElementException)
        {
            return false;
        }
    }

    [SetUp]
    public void SetUp()
    {
        _driver = new EdgeDriver();
        _driver.Navigate().GoToUrl("https://the-internet.herokuapp.com/login");
    }

    [TearDown]
    public void TearDown()
    {
        _driver.Quit();
        _driver.Dispose();
    }

    [Test]
    public void LoginPage_TextMatchesSnapshot()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(2) > div:nth-child(2) > div:nth-child(1) > h2:nth-child(1)"));
        Assert.That(element.Text, Is.EqualTo("Login Page"));
    }

    [Test]
    public void ThisIsWhereYouCanLogIntoTheSecur_TextMatchesSnapshot()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(2) > div:nth-child(2) > div:nth-child(1) > h4:nth-child(2)"));
        Assert.That(element.Text, Is.EqualTo("This is where you can log into the secure area. Enter tomsmith for the username and SuperSecretPassword! for the password. If the information is wrong you should see error messages."));
    }

    [Test]
    public void Username_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.Id("username"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void Password_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.Id("password"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void Login_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(2) > div:nth-child(2) > div:nth-child(1) > form:nth-child(3) > button:nth-child(3)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void ElementalSelenium_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > a:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }
}

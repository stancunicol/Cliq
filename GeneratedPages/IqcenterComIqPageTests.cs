using System.Collections.Generic;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Edge;

namespace Generated;

[TestFixture]
public sealed class IqcenterComIqPageTests
{
    private IWebDriver _driver = null!;

    [OneTimeSetUp]
    public void VerifyIqcenterComIqPage()
    {
        using var driver = new EdgeDriver();
        driver.Navigate().GoToUrl("https://iqcenter.com/en/start/iq?utm_source=bing&utm_medium=cpc&utm_content=EN%20-%20IQ&utm_campaign=RO%20-%20IQ%20Test%20-%20MS3%20-%20Computer%20-%20English&utm_term=intelligence%20quotient%20test&campaignId=488028021&adGroupId=1233653569824328&feedItemId&targetId=kwd-77104030498236:loc-224&matchType=e&network=o&device=c&deviceType=desktop&campaignType=search&creativeId=77103493517457&keyword=iq%20test&utm_id=488028021&gaid=RO-EN-C-IQ-MS");
        var missing = new List<string>();

        if (!TryFind(driver, By.CssSelector("div:nth-child(273) > header:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > a:nth-child(1)"))) missing.Add("a_0 (div:nth-child(273) > header:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > a:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(273) > main:nth-child(3) > div:nth-child(1) > section:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > h1:nth-child(1)"))) missing.Add("discover_your (div:nth-child(273) > main:nth-child(3) > div:nth-child(1) > section:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > h1:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(273) > main:nth-child(3) > div:nth-child(1) > section:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > h1:nth-child(2)"))) missing.Add("iq_score (div:nth-child(273) > main:nth-child(3) > div:nth-child(1) > section:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > h1:nth-child(2))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(273) > main:nth-child(3) > div:nth-child(1) > section:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > h5:nth-child(2)"))) missing.Add("complete_this_5_minute_iq_test_to_measur (div:nth-child(273) > main:nth-child(3) > div:nth-child(1) > section:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > h5:nth-child(2))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(273) > main:nth-child(3) > div:nth-child(1) > section:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > h5:nth-child(3)"))) missing.Add("start_by_selecting_your_gender (div:nth-child(273) > main:nth-child(3) > div:nth-child(1) > section:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > h5:nth-child(3))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(273) > main:nth-child(3) > div:nth-child(1) > section:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > button:nth-child(1)"))) missing.Add("male (div:nth-child(273) > main:nth-child(3) > div:nth-child(1) > section:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > button:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(273) > main:nth-child(3) > div:nth-child(1) > section:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > button:nth-child(2)"))) missing.Add("female (div:nth-child(273) > main:nth-child(3) > div:nth-child(1) > section:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > button:nth-child(2))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(273) > main:nth-child(3) > div:nth-child(1) > section:nth-child(2) > div:nth-child(1) > div:nth-child(1) > h3:nth-child(1)"))) missing.Add("how_it_works (div:nth-child(273) > main:nth-child(3) > div:nth-child(1) > section:nth-child(2) > div:nth-child(1) > div:nth-child(1) > h3:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(273) > main:nth-child(3) > div:nth-child(1) > section:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(1) > h3:nth-child(1)"))) missing.Add("h3_8 (div:nth-child(273) > main:nth-child(3) > div:nth-child(1) > section:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(1) > h3:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(273) > main:nth-child(3) > div:nth-child(1) > section:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > h5:nth-child(1)"))) missing.Add("prepare_yourself (div:nth-child(273) > main:nth-child(3) > div:nth-child(1) > section:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > h5:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(273) > main:nth-child(3) > div:nth-child(1) > section:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > div:nth-child(1) > h3:nth-child(1)"))) missing.Add("h3_10 (div:nth-child(273) > main:nth-child(3) > div:nth-child(1) > section:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > div:nth-child(1) > h3:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(273) > main:nth-child(3) > div:nth-child(1) > section:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > div:nth-child(2) > h5:nth-child(1)"))) missing.Add("complete_the_test (div:nth-child(273) > main:nth-child(3) > div:nth-child(1) > section:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > div:nth-child(2) > h5:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(273) > main:nth-child(3) > div:nth-child(1) > section:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(3) > div:nth-child(1) > h3:nth-child(1)"))) missing.Add("h3_12 (div:nth-child(273) > main:nth-child(3) > div:nth-child(1) > section:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(3) > div:nth-child(1) > h3:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(273) > main:nth-child(3) > div:nth-child(1) > section:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(3) > div:nth-child(2) > h5:nth-child(1)"))) missing.Add("receive_your_insights (div:nth-child(273) > main:nth-child(3) > div:nth-child(1) > section:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(3) > div:nth-child(2) > h5:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(273) > main:nth-child(3) > div:nth-child(1) > section:nth-child(2) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > a:nth-child(1)"))) missing.Add("a_14 (div:nth-child(273) > main:nth-child(3) > div:nth-child(1) > section:nth-child(2) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > a:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(273) > main:nth-child(3) > div:nth-child(1) > section:nth-child(2) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > div:nth-child(2) > p:nth-child(1) > a:nth-child(1)"))) missing.Add("terms_of_service (div:nth-child(273) > main:nth-child(3) > div:nth-child(1) > section:nth-child(2) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > div:nth-child(2) > p:nth-child(1) > a:nth-child(1))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(273) > main:nth-child(3) > div:nth-child(1) > section:nth-child(2) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > div:nth-child(2) > p:nth-child(1) > a:nth-child(2)"))) missing.Add("privacy_policy (div:nth-child(273) > main:nth-child(3) > div:nth-child(1) > section:nth-child(2) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > div:nth-child(2) > p:nth-child(1) > a:nth-child(2))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(273) > main:nth-child(3) > div:nth-child(1) > section:nth-child(2) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > div:nth-child(2) > p:nth-child(1) > a:nth-child(3)"))) missing.Add("contact_us (div:nth-child(273) > main:nth-child(3) > div:nth-child(1) > section:nth-child(2) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > div:nth-child(2) > p:nth-child(1) > a:nth-child(3))");
        if (!TryFind(driver, By.CssSelector("div:nth-child(273) > main:nth-child(3) > div:nth-child(1) > section:nth-child(2) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > div:nth-child(2) > p:nth-child(1) > a:nth-child(4)"))) missing.Add("faq (div:nth-child(273) > main:nth-child(3) > div:nth-child(1) > section:nth-child(2) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > div:nth-child(2) > p:nth-child(1) > a:nth-child(4))");

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
        _driver.Navigate().GoToUrl("https://iqcenter.com/en/start/iq?utm_source=bing&utm_medium=cpc&utm_content=EN%20-%20IQ&utm_campaign=RO%20-%20IQ%20Test%20-%20MS3%20-%20Computer%20-%20English&utm_term=intelligence%20quotient%20test&campaignId=488028021&adGroupId=1233653569824328&feedItemId&targetId=kwd-77104030498236:loc-224&matchType=e&network=o&device=c&deviceType=desktop&campaignType=search&creativeId=77103493517457&keyword=iq%20test&utm_id=488028021&gaid=RO-EN-C-IQ-MS");
    }

    [TearDown]
    public void TearDown()
    {
        _driver.Quit();
        _driver.Dispose();
    }

    [Test]
    public void A0_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(273) > header:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > a:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void DiscoverYour_TextMatchesSnapshot()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(273) > main:nth-child(3) > div:nth-child(1) > section:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > h1:nth-child(1)"));
        Assert.That(element.Text, Is.EqualTo("Discover your"));
    }

    [Test]
    public void IqScore_TextMatchesSnapshot()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(273) > main:nth-child(3) > div:nth-child(1) > section:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > h1:nth-child(2)"));
        Assert.That(element.Text, Is.EqualTo("IQ Score"));
    }

    [Test]
    public void CompleteThis5MinuteIqTestToMeasur_TextMatchesSnapshot()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(273) > main:nth-child(3) > div:nth-child(1) > section:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > h5:nth-child(2)"));
        Assert.That(element.Text, Is.EqualTo("Complete this 5-minute IQ test to measure your intelligence and discover your unique cognitive strengths."));
    }

    [Test]
    public void StartBySelectingYourGender_TextMatchesSnapshot()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(273) > main:nth-child(3) > div:nth-child(1) > section:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > h5:nth-child(3)"));
        Assert.That(element.Text, Is.EqualTo("Start by selecting your gender:"));
    }

    [Test]
    public void Male_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(273) > main:nth-child(3) > div:nth-child(1) > section:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > button:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void Female_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(273) > main:nth-child(3) > div:nth-child(1) > section:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(4) > button:nth-child(2)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void HowItWorks_TextMatchesSnapshot()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(273) > main:nth-child(3) > div:nth-child(1) > section:nth-child(2) > div:nth-child(1) > div:nth-child(1) > h3:nth-child(1)"));
        Assert.That(element.Text, Is.EqualTo("How It Works"));
    }

    [Test]
    public void H38_TextMatchesSnapshot()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(273) > main:nth-child(3) > div:nth-child(1) > section:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(1) > h3:nth-child(1)"));
        Assert.That(element.Text, Is.EqualTo("1"));
    }

    [Test]
    public void PrepareYourself_TextMatchesSnapshot()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(273) > main:nth-child(3) > div:nth-child(1) > section:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(1) > div:nth-child(2) > h5:nth-child(1)"));
        Assert.That(element.Text, Is.EqualTo("Prepare yourself"));
    }

    [Test]
    public void H310_TextMatchesSnapshot()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(273) > main:nth-child(3) > div:nth-child(1) > section:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > div:nth-child(1) > h3:nth-child(1)"));
        Assert.That(element.Text, Is.EqualTo("2"));
    }

    [Test]
    public void CompleteTheTest_TextMatchesSnapshot()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(273) > main:nth-child(3) > div:nth-child(1) > section:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(2) > div:nth-child(2) > h5:nth-child(1)"));
        Assert.That(element.Text, Is.EqualTo("Complete the test"));
    }

    [Test]
    public void H312_TextMatchesSnapshot()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(273) > main:nth-child(3) > div:nth-child(1) > section:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(3) > div:nth-child(1) > h3:nth-child(1)"));
        Assert.That(element.Text, Is.EqualTo("3"));
    }

    [Test]
    public void ReceiveYourInsights_TextMatchesSnapshot()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(273) > main:nth-child(3) > div:nth-child(1) > section:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(3) > div:nth-child(3) > div:nth-child(2) > h5:nth-child(1)"));
        Assert.That(element.Text, Is.EqualTo("Receive your insights"));
    }

    [Test]
    public void A14_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(273) > main:nth-child(3) > div:nth-child(1) > section:nth-child(2) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > a:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void TermsOfService_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(273) > main:nth-child(3) > div:nth-child(1) > section:nth-child(2) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > div:nth-child(2) > p:nth-child(1) > a:nth-child(1)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void PrivacyPolicy_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(273) > main:nth-child(3) > div:nth-child(1) > section:nth-child(2) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > div:nth-child(2) > p:nth-child(1) > a:nth-child(2)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void ContactUs_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(273) > main:nth-child(3) > div:nth-child(1) > section:nth-child(2) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > div:nth-child(2) > p:nth-child(1) > a:nth-child(3)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }

    [Test]
    public void Faq_IsPresentAndInteractable()
    {
        var element = _driver.FindElement(By.CssSelector("div:nth-child(273) > main:nth-child(3) > div:nth-child(1) > section:nth-child(2) > div:nth-child(2) > div:nth-child(1) > div:nth-child(1) > div:nth-child(1) > div:nth-child(2) > div:nth-child(2) > p:nth-child(1) > a:nth-child(4)"));
        Assert.That(element.Displayed, Is.True);
        Assert.That(element.Enabled, Is.True);
    }
}

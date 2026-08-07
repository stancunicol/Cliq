using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Generated.PageObjects;

namespace Generated.Tests;

[TestFixture]
public sealed class CounterAndFormInteractionsTests
{
    private IWebDriver _driver = null!;

    [SetUp]
    public void SetUp() => _driver = new ChromeDriver();

    [TearDown]
    public void TearDown()
    {
        _driver.Quit();
        _driver.Dispose();
    }

    [Test]
    public void CounterAndFormInteractionsWorkflow()
    {
        var recorderFixturePage = new RecorderFixturePage(_driver);

        _driver.Navigate().GoToUrl("http://127.0.0.1:4174/");
        recorderFixturePage.ClickIncrement counter();
        Assert.That(recorderFixturePage.GetCounter outputText(), Is.EqualTo("Count: 1"));
        recorderFixturePage.Display name.Upload("Cliq demo");
        recorderFixturePage.Role.Upload("tester");
    }

    private static string RequiredSecret(string key) =>
        Environment.GetEnvironmentVariable($"CLIQ_{key}")
        ?? throw new InvalidOperationException($"Required environment variable CLIQ_{key} is missing.");
}

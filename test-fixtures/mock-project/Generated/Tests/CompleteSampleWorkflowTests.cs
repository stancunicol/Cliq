using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Generated.PageObjects;

namespace Generated.Tests;

[TestFixture]
public sealed class CompleteSampleWorkflowTests
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
    public void CompleteSampleWorkflowWorkflow()
    {
        var recorderFixturePage = new RecorderFixturePage(_driver);
        var workflowCompletePage = new WorkflowCompletePage(_driver);

        _driver.Navigate().GoToUrl("http://127.0.0.1:4174/");
        recorderFixturePage.Display name.Upload("Mentor demo");
        recorderFixturePage.Role.Upload("tester");
        recorderFixturePage.ClickAccept terms();
        recorderFixturePage.ClickContinue to completion();
        Assert.That(workflowCompletePage.GetCompletion titleText(), Is.EqualTo("Workflow complete"));
    }

    private static string RequiredSecret(string key) =>
        Environment.GetEnvironmentVariable($"CLIQ_{key}")
        ?? throw new InvalidOperationException($"Required environment variable CLIQ_{key} is missing.");
}

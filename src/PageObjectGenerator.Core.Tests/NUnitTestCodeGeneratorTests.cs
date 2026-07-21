using PageObjectGenerator.Core;

namespace PageObjectGenerator.Core.Tests;

[TestFixture]
public class NUnitTestCodeGeneratorTests
{
    [Test]
    public void Generate_Assert_ProducesSnapshotAssertion()
    {
        var elements = new List<ElementDescriptor>
        {
            new("h2", ElementActionKind.Assert, LocatorStrategy.CssSelector, "h2", "login_page", ExpectedText: "Login Page"),
        };

        var code = NUnitTestCodeGenerator.Generate("LoginPage", "Generated", "https://example.com", BrowserKind.Chrome, elements);

        Assert.That(code, Does.Contain("public void LoginPage_TextMatchesSnapshot()"));
        Assert.That(code, Does.Contain("Assert.That(element.Text, Is.EqualTo(\"Login Page\"));"));
    }

    [Test]
    public void Generate_Fill_ProducesPresenceCheck()
    {
        var elements = new List<ElementDescriptor>
        {
            new("input", ElementActionKind.Fill, LocatorStrategy.Id, "username", "username"),
        };

        var code = NUnitTestCodeGenerator.Generate("LoginPage", "Generated", "https://example.com", BrowserKind.Chrome, elements);

        Assert.That(code, Does.Contain("public void Username_IsPresentAndInteractable()"));
        Assert.That(code, Does.Contain("Assert.That(element.Displayed, Is.True);"));
        Assert.That(code, Does.Contain("Assert.That(element.Enabled, Is.True);"));
    }

    [Test]
    public void Generate_DuplicateNames_GetSuffixedTestNames()
    {
        var elements = new List<ElementDescriptor>
        {
            new("a", ElementActionKind.Click, LocatorStrategy.CssSelector, "a.one", "link"),
            new("a", ElementActionKind.Click, LocatorStrategy.CssSelector, "a.two", "link"),
        };

        var code = NUnitTestCodeGenerator.Generate("Page", "Generated", "https://example.com", BrowserKind.Chrome, elements);

        Assert.That(code, Does.Contain("public void Link_IsPresentAndInteractable()"));
        Assert.That(code, Does.Contain("public void Link_2_IsPresentAndInteractable()"));
    }

    [Test]
    public void Generate_EdgeBrowser_UsesEdgeDriver()
    {
        var code = NUnitTestCodeGenerator.Generate(
            "Page", "Generated", "https://example.com", BrowserKind.Edge, new List<ElementDescriptor>());

        Assert.That(code, Does.Contain("using OpenQA.Selenium.Edge;"));
        Assert.That(code, Does.Contain("_driver = new EdgeDriver();"));
    }

    [Test]
    public void Generate_ChromeBrowser_UsesChromeDriver()
    {
        var code = NUnitTestCodeGenerator.Generate(
            "Page", "Generated", "https://example.com", BrowserKind.Chrome, new List<ElementDescriptor>());

        Assert.That(code, Does.Contain("using OpenQA.Selenium.Chrome;"));
        Assert.That(code, Does.Contain("_driver = new ChromeDriver();"));
    }
}

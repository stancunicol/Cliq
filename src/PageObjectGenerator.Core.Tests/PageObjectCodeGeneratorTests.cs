using PageObjectGenerator.Core;

namespace PageObjectGenerator.Core.Tests;

[TestFixture]
public class PageObjectCodeGeneratorTests
{
    [Test]
    public void ToCSharpStringLiteral_EscapesQuotesAndBackslashes()
    {
        var literal = PageObjectCodeGenerator.ToCSharpStringLiteral("a\"b\\c");

        Assert.That(literal, Is.EqualTo("\"a\\\"b\\\\c\""));
    }

    [TestCase(LocatorStrategy.Id, "username", "By.Id(\"username\")")]
    [TestCase(LocatorStrategy.Name, "username", "By.Name(\"username\")")]
    [TestCase(LocatorStrategy.CssSelector, "div > a", "By.CssSelector(\"div > a\")")]
    public void ByExpression_UsesNativeStrategyWhereAvailable(LocatorStrategy strategy, string locatorValue, string expected)
    {
        var element = new ElementDescriptor("input", ElementActionKind.Fill, strategy, locatorValue, "name");

        Assert.That(PageObjectCodeGenerator.ByExpression(element), Is.EqualTo(expected));
    }

    [Test]
    public void Generate_Fill_ProducesClearAndSendKeys()
    {
        var elements = new List<ElementDescriptor>
        {
            new("input", ElementActionKind.Fill, LocatorStrategy.Id, "username", "username"),
        };

        var code = PageObjectCodeGenerator.Generate("LoginPage", "Generated", elements);

        Assert.That(code, Does.Contain("public void FillUsername(string value)"));
        Assert.That(code, Does.Contain("d.FindElement(By.Id(\"username\"))"));
        Assert.That(code, Does.Contain("element.Clear();"));
        Assert.That(code, Does.Contain("element.SendKeys(value);"));
    }

    [Test]
    public void Generate_Click_AlsoProducesHoverMethod()
    {
        var elements = new List<ElementDescriptor>
        {
            new("button", ElementActionKind.Click, LocatorStrategy.Id, "submit", "submit"),
        };

        var code = PageObjectCodeGenerator.Generate("Page", "Generated", elements);

        Assert.That(code, Does.Contain("public void ClickSubmit()"));
        Assert.That(code, Does.Contain("public void HoverSubmit()"));
        Assert.That(code, Does.Contain("new Actions(_driver).MoveToElement(element).Perform();"));
    }

    [Test]
    public void Generate_Radio_BuildsValueSelectorAtRuntimeWithoutEscapingIssues()
    {
        var elements = new List<ElementDescriptor>
        {
            new("input", ElementActionKind.Radio, LocatorStrategy.Name, "snack", "snack"),
        };

        var code = PageObjectCodeGenerator.Generate("Page", "Generated", elements);

        Assert.That(code, Does.Contain("public void SelectSnack(string value)"));
        Assert.That(code, Does.Contain("\"input[name='snack'][value='\" + value + \"']\""));
    }

    [Test]
    public void Generate_MultiSelect_ProducesDeselectAllAndForeach()
    {
        var elements = new List<ElementDescriptor>
        {
            new("select", ElementActionKind.MultiSelect, LocatorStrategy.Id, "multi", "multi"),
        };

        var code = PageObjectCodeGenerator.Generate("Page", "Generated", elements);

        Assert.That(code, Does.Contain("public void SelectMultiMultiple(IEnumerable<string> values)"));
        Assert.That(code, Does.Contain("select.DeselectAll();"));
    }

    [Test]
    public void Generate_Assert_ProducesGetTextMethod()
    {
        var elements = new List<ElementDescriptor>
        {
            new("h2", ElementActionKind.Assert, LocatorStrategy.CssSelector, "h2", "login_page", ExpectedText: "Login Page"),
        };

        var code = PageObjectCodeGenerator.Generate("Page", "Generated", elements);

        Assert.That(code, Does.Contain("public string GetLoginPageText()"));
        Assert.That(code, Does.Contain("return element.Text;"));
    }

    [Test]
    public void Generate_NoElements_ProducesNoMethods()
    {
        var code = PageObjectCodeGenerator.Generate("EmptyPage", "Generated", new List<ElementDescriptor>());

        Assert.That(code, Does.Contain("public sealed class EmptyPage"));
        Assert.That(code, Does.Not.Contain("public void"));
    }
}

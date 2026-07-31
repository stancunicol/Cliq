using Cliq.Infrastructure.Generation;

namespace Cliq.Infrastructure.Generation.Tests;

[TestFixture]
public class PageObjectCodeGeneratorTests
{
    [Test]
    public void ToCSharpStringLiteral_EscapesQuotesAndBackslashes()
    {
        var literal = PageObjectCodeGenerator.ToCSharpStringLiteral("a\"b\\c");

        Assert.That(literal, Is.EqualTo("\"a\\\"b\\\\c\""));
    }

    [Test]
    public void ToCSharpStringLiteral_EscapesEmbeddedNewlinesAndTabs()
    {
        var literal = PageObjectCodeGenerator.ToCSharpStringLiteral("speedlight -\n                Jacheta pentru femei");

        Assert.That(literal, Does.Not.Contain("\n"));
        Assert.That(literal, Does.Contain("\\n"));
        Assert.That(literal, Is.EqualTo("\"speedlight -\\n                Jacheta pentru femei\""));
    }

    [Test]
    public void ToCSharpStringLiteral_EscapesOtherControlCharactersAsUnicode()
    {
        var literal = PageObjectCodeGenerator.ToCSharpStringLiteral("ab");

        Assert.That(literal, Is.EqualTo("\"a\\u0007b\""));
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

        var code = PageObjectCodeGenerator.Generate("LoginPage", "Generated", elements, 10_000);

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

        var code = PageObjectCodeGenerator.Generate("Page", "Generated", elements, 10_000);

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

        var code = PageObjectCodeGenerator.Generate("Page", "Generated", elements, 10_000);

        Assert.That(code, Does.Contain("public void SelectSnack(string value)"));
        Assert.That(code, Does.Contain("\"input[name='snack'][value='\" + escapedValue + \"']\""));
    }

    [TestCase("snack", "snack")]
    [TestCase("o'clock", "o\\'clock")]
    [TestCase(@"back\slash", @"back\\slash")]
    public void CssEscapeSingleQuoted_EscapesBackslashAndApostrophe(string input, string expected)
    {
        Assert.That(PageObjectCodeGenerator.CssEscapeSingleQuoted(input), Is.EqualTo(expected));
    }

    [Test]
    public void Generate_Radio_EscapesApostropheInGroupName()
    {
        var elements = new List<ElementDescriptor>
        {
            new("input", ElementActionKind.Radio, LocatorStrategy.Name, "o'clock", "snack"),
        };

        var code = PageObjectCodeGenerator.Generate("Page", "Generated", elements, 10_000);
        var expectedPrefix = PageObjectCodeGenerator.ToCSharpStringLiteral(
            "input[name='" + PageObjectCodeGenerator.CssEscapeSingleQuoted("o'clock") + "'][value='");

        Assert.That(code, Does.Contain(expectedPrefix));
    }

    [Test]
    public void Generate_MultiSelect_ProducesDeselectAllAndForeach()
    {
        var elements = new List<ElementDescriptor>
        {
            new("select", ElementActionKind.MultiSelect, LocatorStrategy.Id, "multi", "multi"),
        };

        var code = PageObjectCodeGenerator.Generate("Page", "Generated", elements, 10_000);

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

        var code = PageObjectCodeGenerator.Generate("Page", "Generated", elements, 10_000);

        Assert.That(code, Does.Contain("public string GetLoginPageText()"));
        Assert.That(code, Does.Contain("return element.Text;"));
    }

    [Test]
    public void Generate_NoElements_ProducesNoMethods()
    {
        var code = PageObjectCodeGenerator.Generate("EmptyPage", "Generated", new List<ElementDescriptor>(), 10_000);

        Assert.That(code, Does.Contain("public sealed partial class EmptyPage"));
        Assert.That(code, Does.Not.Contain("public void"));
    }

    [Test]
    public void Generate_ClassDeclaration_IsPartial()
    {
        var code = PageObjectCodeGenerator.Generate("LoginPage", "Generated", new List<ElementDescriptor>(), 10_000);

        Assert.That(code, Does.Contain("public sealed partial class LoginPage"));
    }

    [Test]
    public void GenerateCustomStub_ProducesEmptyMatchingPartialClass()
    {
        var stub = PageObjectCodeGenerator.GenerateCustomStub("LoginPage", "Generated");

        Assert.That(stub, Does.Contain("namespace Generated;"));
        Assert.That(stub, Does.Contain("public sealed partial class LoginPage"));
        Assert.That(stub, Does.Not.Contain("public void"));
    }
}

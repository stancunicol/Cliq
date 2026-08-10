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
        // Built from a char code rather than embedding a raw BEL byte in source, which editors
        // and text-based tooling can silently drop or mangle.
        var literal = PageObjectCodeGenerator.ToCSharpStringLiteral("a" + (char)7 + "b");

        Assert.That(literal, Is.EqualTo("\"a\\u0007b\""));
    }

    [TestCase(LocatorStrategy.CssSelector, "div > a", "By.CssSelector(\"div > a\")")]
    [TestCase(LocatorStrategy.XPath, "//a", "By.XPath(\"//a\")")]
    public void ByExpression_UsesNativeStrategyWhereAvailable(LocatorStrategy strategy, string locatorValue, string expected)
    {
        var element = new ElementDescriptor("input", ElementActionKind.Fill, strategy, locatorValue, "name");

        Assert.That(PageObjectCodeGenerator.ByExpression(element), Is.EqualTo(expected));
    }

    [Test]
    public void ByExpression_RejectsName_NotAStaticDiscoveryStrategy()
    {
        var element = new ElementDescriptor("input", ElementActionKind.Fill, LocatorStrategy.Name, "username", "name");

        Assert.Throws<ArgumentOutOfRangeException>(() => PageObjectCodeGenerator.ByExpression(element));
    }

    [Test]
    public void Generate_XPathElement_EmitsFullXPathInAPrecedingComment()
    {
        const string fullXPath = "//label[contains(normalize-space(string(.)), \"Username\")]/following::input[1]";
        var elements = new List<ElementDescriptor>
        {
            new("input", ElementActionKind.Fill, LocatorStrategy.XPath, fullXPath, "username"),
        };

        var code = PageObjectCodeGenerator.Generate("LoginPage", "Generated", elements, 10_000, []);
        var commentIndex = code.IndexOf($"// Locator (xpath): {fullXPath}", StringComparison.Ordinal);
        var initIndex = code.IndexOf("Username = new ElementWrapper<string>(", StringComparison.Ordinal);

        Assert.Multiple(() =>
        {
            Assert.That(commentIndex, Is.GreaterThanOrEqualTo(0), "expected the full xpath in a comment above the wrapper init");
            Assert.That(commentIndex, Is.LessThan(initIndex), "the comment should precede the wrapper init it documents");
        });
    }

    [Test]
    public void Generate_LocatorValueWithEmbeddedNewline_StaysOnOneCommentLine()
    {
        // Defensive: nothing upstream is expected to produce a locator value containing a raw
        // newline, but if it ever did, an unescaped one here would end the // comment early and
        // turn the rest of the line into code - collapsing it to a single line avoids that.
        var elements = new List<ElementDescriptor>
        {
            new("input", ElementActionKind.Fill, LocatorStrategy.XPath, "//input[@aria-label=\"line one\nline two\"]", "username"),
        };

        var code = PageObjectCodeGenerator.Generate("LoginPage", "Generated", elements, 10_000, []);
        var commentLine = code.Split('\n').Single(line => line.Contains("// Locator"));

        Assert.Multiple(() =>
        {
            Assert.That(commentLine, Does.Contain("line one line two"));
            Assert.That(code, Does.Contain("Username = new ElementWrapper<string>("));
        });
    }

    [Test]
    public void Generate_Fill_ProducesElementWrapperPropertyWithClearAndSendKeys()
    {
        var elements = new List<ElementDescriptor>
        {
            new("input", ElementActionKind.Fill, LocatorStrategy.CssSelector, "username", "username"),
        };

        var code = PageObjectCodeGenerator.Generate("LoginPage", "Generated", elements, 10_000, []);

        Assert.That(code, Does.Contain("private static readonly By UsernameLocator = By.CssSelector(\"username\");"));
        Assert.That(code, Does.Contain("public ElementWrapper<string> Username { get; }"));
        Assert.That(code, Does.Contain("Username = new ElementWrapper<string>("));
        Assert.That(code, Does.Contain("d.FindElement(UsernameLocator)"));
        Assert.That(code, Does.Contain("element.Clear();"));
        Assert.That(code, Does.Contain("element.SendKeys(value);"));
        Assert.That(code, Does.Contain("return element.GetAttribute(\"value\") ?? \"\";"));
    }

    [Test]
    public void Generate_FillWithIntDataType_ParsesAndFormatsAsInt()
    {
        var elements = new List<ElementDescriptor>
        {
            new("input", ElementActionKind.Fill, LocatorStrategy.CssSelector, "age", "age", DataType: "int"),
        };

        var code = PageObjectCodeGenerator.Generate("Page", "Generated", elements, 10_000, []);

        Assert.That(code, Does.Contain("public ElementWrapper<int> Age { get; }"));
        Assert.That(code, Does.Contain("element.SendKeys(value.ToString());"));
        Assert.That(code, Does.Contain("return int.Parse(element.GetAttribute(\"value\")!);"));
    }

    [Test]
    public void Generate_FillWithDateTimeDataType_ParsesAndFormatsAsIsoDate()
    {
        var elements = new List<ElementDescriptor>
        {
            new("input", ElementActionKind.Fill, LocatorStrategy.CssSelector, "dob", "dob", DataType: "DateTime"),
        };

        var code = PageObjectCodeGenerator.Generate("Page", "Generated", elements, 10_000, []);

        Assert.That(code, Does.Contain("public ElementWrapper<DateTime> Dob { get; }"));
        Assert.That(code, Does.Contain("element.SendKeys(value.ToString(\"yyyy-MM-dd\"));"));
        Assert.That(code, Does.Contain("return DateTime.Parse(element.GetAttribute(\"value\")!);"));
    }

    [Test]
    public void Generate_Select_ProducesElementWrapperPropertyWithSelectByValue()
    {
        var elements = new List<ElementDescriptor>
        {
            new("select", ElementActionKind.Select, LocatorStrategy.CssSelector, "country", "country"),
        };

        var code = PageObjectCodeGenerator.Generate("Page", "Generated", elements, 10_000, []);

        Assert.That(code, Does.Contain("public ElementWrapper<string> Country { get; }"));
        Assert.That(code, Does.Contain("new SelectElement(element).SelectByValue(value);"));
        Assert.That(code, Does.Contain("return new SelectElement(element).SelectedOption.GetAttribute(\"value\") ?? \"\";"));
    }

    [Test]
    public void Generate_Click_AlsoProducesHoverMethod()
    {
        var elements = new List<ElementDescriptor>
        {
            new("button", ElementActionKind.Click, LocatorStrategy.CssSelector, "submit", "submit"),
        };

        var code = PageObjectCodeGenerator.Generate("Page", "Generated", elements, 10_000, []);

        Assert.That(code, Does.Contain("public void ClickSubmit()"));
        Assert.That(code, Does.Contain("public void HoverSubmit()"));
        Assert.That(code, Does.Contain("new Actions(_driver).MoveToElement(element).Perform();"));
    }

    [Test]
    public void Generate_EmitsAClickWithFallbackHelper_UsedByClickCheckAndRadio()
    {
        var elements = new List<ElementDescriptor>
        {
            new("button", ElementActionKind.Click, LocatorStrategy.CssSelector, "submit", "submit"),
            new("input", ElementActionKind.Check, LocatorStrategy.CssSelector, "agree", "agree"),
            new("input", ElementActionKind.Radio, LocatorStrategy.Name, "snack", "snack"),
        };

        var code = PageObjectCodeGenerator.Generate("Page", "Generated", elements, 10_000, []);

        Assert.Multiple(() =>
        {
            Assert.That(code, Does.Contain("private void ClickWithFallback(IWebElement element)"));
            Assert.That(code, Does.Contain("catch (ElementClickInterceptedException)"));
            Assert.That(
                code,
                Does.Contain("((IJavaScriptExecutor)_driver).ExecuteScript(\"arguments[0].click();\", element);"));
            // Click/Check/Radio all route their click through the helper - none call
            // element.Click() directly except the helper's own body.
            Assert.That(CountOccurrences(code, "element.Click();"), Is.EqualTo(1));
            Assert.That(code, Does.Contain("        ClickWithFallback(element);"));
            Assert.That(code, Does.Contain("            ClickWithFallback(element);"));
        });
    }

    [Test]
    public void Generate_DynamicClick_UsesTheSameClickWithFallbackHelper()
    {
        var dynamicElements = new List<DynamicElementDescriptor>
        {
            new(ElementActionKind.Click, LocatorStrategy.XPath, "//tr[{param}]/button", "rowButton"),
        };

        var code = PageObjectCodeGenerator.Generate("Page", "Generated", [], 10_000, [], dynamicElements);

        Assert.That(code, Does.Contain("        ClickWithFallback(element);"));
    }

    [Test]
    public void Generate_Click_LocatorIsAStaticFieldSharedByClickAndHover()
    {
        var elements = new List<ElementDescriptor>
        {
            new("button", ElementActionKind.Click, LocatorStrategy.CssSelector, "submit", "submit"),
        };

        var code = PageObjectCodeGenerator.Generate("Page", "Generated", elements, 10_000, []);

        Assert.Multiple(() =>
        {
            Assert.That(code, Does.Contain("// Locator (css): submit"));
            Assert.That(code, Does.Contain("private static readonly By SubmitLocator = By.CssSelector(\"submit\");"));
            // Exactly one By.CssSelector("submit") anywhere in the file - the field - not one per method.
            Assert.That(
                CountOccurrences(code, "By.CssSelector(\"submit\")"),
                Is.EqualTo(1),
                "Click and Hover should both reference the static field, not rebuild the By themselves");
            Assert.That(code, Does.Contain("d.FindElement(SubmitLocator)"));
        });
    }

    [Test]
    public void Generate_Radio_BuildsValueSelectorAtRuntimeWithoutEscapingIssues()
    {
        var elements = new List<ElementDescriptor>
        {
            new("input", ElementActionKind.Radio, LocatorStrategy.Name, "snack", "snack"),
        };

        var code = PageObjectCodeGenerator.Generate("Page", "Generated", elements, 10_000, []);

        Assert.That(code, Does.Contain("public void SelectSnack(string value)"));
        Assert.That(code, Does.Contain("\"input[name='snack'][value='\" + escapedValue + \"']\""));
    }

    [Test]
    public void Generate_Radio_HasNoStaticLocatorField()
    {
        var elements = new List<ElementDescriptor>
        {
            new("input", ElementActionKind.Radio, LocatorStrategy.Name, "snack", "snack"),
        };

        var code = PageObjectCodeGenerator.Generate("Page", "Generated", elements, 10_000, []);

        Assert.That(code, Does.Not.Contain("SnackLocator"));
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

        var code = PageObjectCodeGenerator.Generate("Page", "Generated", elements, 10_000, []);
        var expectedPrefix = PageObjectCodeGenerator.ToCSharpStringLiteral(
            "input[name='" + PageObjectCodeGenerator.CssEscapeSingleQuoted("o'clock") + "'][value='");

        Assert.That(code, Does.Contain(expectedPrefix));
    }

    [Test]
    public void Generate_MultiSelect_ProducesElementWrapperPropertyWithDeselectAllAndForeach()
    {
        var elements = new List<ElementDescriptor>
        {
            new("select", ElementActionKind.MultiSelect, LocatorStrategy.CssSelector, "multi", "multi"),
        };

        var code = PageObjectCodeGenerator.Generate("Page", "Generated", elements, 10_000, []);

        Assert.That(code, Does.Contain("public ElementWrapper<string[]> Multi { get; }"));
        Assert.That(code, Does.Contain("select.DeselectAll();"));
        Assert.That(code, Does.Contain("foreach (var item in value)"));
        Assert.That(code, Does.Contain("AllSelectedOptions.Select(o => o.GetAttribute(\"value\") ?? \"\").ToArray();"));
    }

    [Test]
    public void Generate_Assert_ProducesGetTextMethod()
    {
        var elements = new List<ElementDescriptor>
        {
            new("h2", ElementActionKind.Assert, LocatorStrategy.CssSelector, "h2", "login_page", ExpectedText: "Login Page"),
        };

        var code = PageObjectCodeGenerator.Generate("Page", "Generated", elements, 10_000, []);

        Assert.That(code, Does.Contain("public string GetLoginPageText()"));
        Assert.That(code, Does.Contain("return element.Text;"));
    }

    [Test]
    public void Generate_NoElementsOrCustomMethods_ProducesNoMethods()
    {
        var code = PageObjectCodeGenerator.Generate("EmptyPage", "Generated", new List<ElementDescriptor>(), 10_000, []);

        Assert.That(code, Does.Contain("public sealed class EmptyPage"));
        Assert.That(code, Does.Not.Contain("public void"));
    }

    [Test]
    public void Generate_ClassDeclaration_IsSealedNotPartial()
    {
        var code = PageObjectCodeGenerator.Generate("LoginPage", "Generated", new List<ElementDescriptor>(), 10_000, []);

        Assert.That(code, Does.Contain("public sealed class LoginPage"));
        Assert.That(code, Does.Not.Contain("partial"));
    }

    [Test]
    public void Generate_WithCustomMethods_AppendsThemVerbatimAfterGeneratedMethods()
    {
        const string customMethod = """
                [CliqCustomMethod]
                public void WaitForSpinnerToClear()
                {
                    _wait.Until(d => !d.FindElements(By.CssSelector(".spinner")).Any());
                }
            """;
        var elements = new List<ElementDescriptor>
        {
            new("input", ElementActionKind.Fill, LocatorStrategy.CssSelector, "username", "username"),
        };

        var code = PageObjectCodeGenerator.Generate("LoginPage", "Generated", elements, 10_000, [customMethod]);

        Assert.Multiple(() =>
        {
            Assert.That(code, Does.Contain("Username = new ElementWrapper<string>("));
            Assert.That(code, Does.Contain("[CliqCustomMethod]"));
            Assert.That(code, Does.Contain("public void WaitForSpinnerToClear()"));
            Assert.That(
                code.IndexOf("Username", StringComparison.Ordinal),
                Is.LessThan(code.IndexOf("WaitForSpinnerToClear", StringComparison.Ordinal)),
                "generated members should come before preserved custom ones");
        });
    }

    [Test]
    public void GenerateAttributeFile_ProducesTheMarkerAttributeDefinition()
    {
        var code = PageObjectCodeGenerator.GenerateAttributeFile("Generated");

        Assert.Multiple(() =>
        {
            Assert.That(code, Does.Contain("namespace Generated;"));
            Assert.That(code, Does.Contain("[AttributeUsage(AttributeTargets.Method)]"));
            Assert.That(code, Does.Contain("class CliqCustomMethodAttribute : Attribute"));
        });
    }

    [Test]
    public void GenerateElementWrapperFile_ProducesTheGenericWrapperDefinition()
    {
        var code = PageObjectCodeGenerator.GenerateElementWrapperFile("Generated");

        Assert.Multiple(() =>
        {
            Assert.That(code, Does.Contain("namespace Generated;"));
            Assert.That(code, Does.Contain("public sealed class ElementWrapper<TData>"));
            Assert.That(code, Does.Contain("public void Upload(TData value) => _upload(value);"));
            Assert.That(code, Does.Contain("public TData Read() => _read();"));
        });
    }

    [Test]
    public void ToInterpolatedCSharpStringLiteral_KeepsThePlaceholderAsARealInterpolationHole()
    {
        var literal = PageObjectCodeGenerator.ToInterpolatedCSharpStringLiteral("//table/tbody/tr[{param}]/td[2]");

        Assert.That(literal, Is.EqualTo("$\"//table/tbody/tr[{param}]/td[2]\""));
    }

    [Test]
    public void ToInterpolatedCSharpStringLiteral_EscapesOtherBracesSoTheyAreNotMistakenForHoles()
    {
        var literal = PageObjectCodeGenerator.ToInterpolatedCSharpStringLiteral("//div[@style='{color:red}']//tr[{param}]");

        Assert.That(literal, Is.EqualTo("$\"//div[@style='{{color:red}}']//tr[{param}]\""));
    }

    [Test]
    public void ToInterpolatedCSharpStringLiteral_EscapesQuotesAndBackslashesLikeThePlainVariant()
    {
        var literal = PageObjectCodeGenerator.ToInterpolatedCSharpStringLiteral("//a[@title=\"x\\y\"]/tr[{param}]");

        Assert.That(literal, Is.EqualTo("$\"//a[@title=\\\"x\\\\y\\\"]/tr[{param}]\""));
    }

    [Test]
    public void Generate_DynamicClick_ProducesFactoryMethodsTakingAnIntParameter()
    {
        var dynamicElements = new List<DynamicElementDescriptor>
        {
            new(ElementActionKind.Click, LocatorStrategy.XPath, "//table/tbody/tr[{param}]/td[1]/button", "rowButton"),
        };

        var code = PageObjectCodeGenerator.Generate("Page", "Generated", [], 10_000, [], dynamicElements);

        Assert.Multiple(() =>
        {
            Assert.That(code, Does.Contain("// Locator (xpath, template): //table/tbody/tr[{param}]/td[1]/button"));
            Assert.That(code, Does.Contain("public void ClickRowButton(int param)"));
            Assert.That(code, Does.Contain("public void HoverRowButton(int param)"));
            Assert.That(code, Does.Contain("By.XPath($\"//table/tbody/tr[{param}]/td[1]/button\")"));
        });
    }

    [Test]
    public void InterpolatedByExpression_RejectsName_NotASupportedDynamicStrategy()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PageObjectCodeGenerator.InterpolatedByExpression(
            new DynamicElementDescriptor(ElementActionKind.Click, LocatorStrategy.Name, "row-{param}", "rowButton")));
    }

    [Test]
    public void Generate_DynamicFill_ProducesAFactoryMethodReturningAnElementWrapper()
    {
        var dynamicElements = new List<DynamicElementDescriptor>
        {
            new(ElementActionKind.Fill, LocatorStrategy.XPath, "//tr[{param}]/td[2]/input", "rowInput", DataType: "int"),
        };

        var code = PageObjectCodeGenerator.Generate("Page", "Generated", [], 10_000, [], dynamicElements);

        Assert.Multiple(() =>
        {
            Assert.That(code, Does.Contain("public ElementWrapper<int> RowInput(int param) =>"));
            Assert.That(code, Does.Contain("new ElementWrapper<int>("));
            Assert.That(code, Does.Contain("element.SendKeys(value.ToString());"));
            Assert.That(code, Does.Contain("return int.Parse(element.GetAttribute(\"value\")!);"));
        });
    }

    [Test]
    public void Generate_DynamicSelect_ProducesAFactoryMethodWithAStringParameter()
    {
        var dynamicElements = new List<DynamicElementDescriptor>
        {
            new(ElementActionKind.Select, LocatorStrategy.XPath, "//tr[@data-key='{param}']/td/select", "rowSelect", ParameterType: "string"),
        };

        var code = PageObjectCodeGenerator.Generate("Page", "Generated", [], 10_000, [], dynamicElements);

        Assert.Multiple(() =>
        {
            Assert.That(code, Does.Contain("public ElementWrapper<string> RowSelect(string param) =>"));
            Assert.That(code, Does.Contain("new SelectElement(element).SelectByValue(value);"));
        });
    }

    [Test]
    public void Generate_DynamicAssertText_ProducesAFactoryMethodReturningString()
    {
        var dynamicElements = new List<DynamicElementDescriptor>
        {
            new(ElementActionKind.Assert, LocatorStrategy.XPath, "//tr[{param}]/td[3]", "rowStatus"),
        };

        var code = PageObjectCodeGenerator.Generate("Page", "Generated", [], 10_000, [], dynamicElements);

        Assert.That(code, Does.Contain("public string GetRowStatusText(int param)"));
    }

    private static int CountOccurrences(string text, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }
}

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
    public void Generate_NoBasePageClassName_ClassIsSelfContained()
    {
        var code = PageObjectCodeGenerator.Generate("LoginPage", "Generated", [], 10_000, []);

        Assert.That(code, Does.Contain("public sealed class LoginPage\r\n").Or.Contain("public sealed class LoginPage\n"));
        Assert.That(code, Does.Contain("public LoginPage(IWebDriver driver)\r\n").Or.Contain("public LoginPage(IWebDriver driver)\n"));
    }

    [Test]
    public void Generate_WithBasePageFieldNames_InheritsAndDropsLocalDriverWaitFields()
    {
        var elements = new List<ElementDescriptor>
        {
            new("button", ElementActionKind.Click, LocatorStrategy.CssSelector, "#submit", "submit"),
            new("input", ElementActionKind.Check, LocatorStrategy.CssSelector, "#agree", "agree"),
        };
        var fieldNames = new BasePageFieldNames("driver", "_driver", "_wait");

        var code = PageObjectCodeGenerator.Generate("LoginPage", "Generated", elements, 10_000, [], basePageFieldNames: fieldNames);

        Assert.Multiple(() =>
        {
            Assert.That(code, Does.Contain("public sealed class LoginPage : BasePage"));
            Assert.That(code, Does.Contain("public LoginPage(IWebDriver driver) : base(driver)"));
            // The class relies on BasePage's inherited members instead of declaring its own.
            Assert.That(code, Does.Not.Contain("private readonly IWebDriver"));
            Assert.That(code, Does.Not.Contain("private readonly WebDriverWait"));
            Assert.That(code, Does.Not.Contain("private void ClickWithFallback"));
            // Method bodies call the shared BasePage helpers instead of building the wait/hover
            // logic inline - see GenerateBasePageFile's Locate/Hover/SetChecked.
            Assert.That(code, Does.Contain("var element = Locate(SubmitLocator);"));
            Assert.That(code, Does.Contain("ClickWithFallback(element);"));
            Assert.That(code, Does.Contain("Hover(element);"));
            Assert.That(code, Does.Contain("SetChecked(element, isChecked);"));
            Assert.That(code, Does.Not.Contain("Actions("));
            Assert.That(code, Does.Not.Contain("if (element.Selected != isChecked)"));
        });
    }

    // The whole point of Locate/Hover/SetChecked living on the shared BasePage: a derived page's
    // own method bodies no longer need to know the driver/wait field names at all, regardless of
    // what they're called - only BasePage.cs itself (see GenerateBasePageFile) uses the mirrored
    // names, in its own field declarations and helper bodies.
    [Test]
    public void Generate_WithMirroredFieldNames_MethodBodiesDoNotReferenceFieldNamesAtAll()
    {
        var elements = new List<ElementDescriptor>
        {
            new("button", ElementActionKind.Click, LocatorStrategy.CssSelector, "#submit", "submit"),
        };
        // Mirrors the real PageFactory-style fixture (test-fixtures/.../LoginPage.cs): the driver
        // is stored as a plain `driver` field, no leading underscore, no wait field of its own.
        var fieldNames = new BasePageFieldNames("driver", "driver", "wait");

        var code = PageObjectCodeGenerator.Generate("LoginPage", "Generated", elements, 10_000, [], basePageFieldNames: fieldNames);

        Assert.Multiple(() =>
        {
            Assert.That(code, Does.Contain("var element = Locate(SubmitLocator);"));
            Assert.That(code, Does.Not.Contain("_wait"));
            Assert.That(code, Does.Not.Contain("_driver"));
        });
    }

    [Test]
    public void GenerateBasePageFile_EmitsFieldsConstructorAndClickWithFallback()
    {
        var style = new ExamplePageStyle("driver", "driver");

        var code = PageObjectCodeGenerator.GenerateBasePageFile("Generated", style, 10_000);

        Assert.Multiple(() =>
        {
            Assert.That(code, Does.Contain("public abstract class BasePage"));
            Assert.That(code, Does.Contain("protected readonly IWebDriver driver;"));
            Assert.That(code, Does.Contain("protected readonly WebDriverWait wait;"));
            Assert.That(code, Does.Contain("protected BasePage(IWebDriver driver)"));
            Assert.That(code, Does.Contain("driver = driver;"));
            Assert.That(code, Does.Contain("wait = new WebDriverWait(driver, TimeSpan.FromMilliseconds(10000));"));
            Assert.That(code, Does.Contain("protected virtual void ClickWithFallback(IWebElement element)"));
            Assert.That(code, Does.Contain("((IJavaScriptExecutor)driver).ExecuteScript(\"arguments[0].click();\", element);"));
            Assert.That(code, Does.Contain("protected virtual IWebElement Locate(By locator) =>"));
            Assert.That(code, Does.Contain("wait.Until(d => d.FindElement(locator));"));
            // TryLocate mirrors Locate but swallows a timeout into a null return instead of
            // propagating it - for a caller that treats "not there" as a legitimate outcome.
            Assert.That(code, Does.Contain("protected virtual IWebElement? TryLocate(By locator)"));
            Assert.That(code, Does.Contain("catch (WebDriverTimeoutException)"));
            Assert.That(code, Does.Contain("return null;"));
            Assert.That(code, Does.Contain("protected virtual void Hover(IWebElement element) =>"));
            Assert.That(code, Does.Contain("new Actions(driver).MoveToElement(element).Perform();"));
            Assert.That(code, Does.Contain("protected virtual void SetChecked(IWebElement element, bool isChecked)"));
        });
    }

    // Regression guard for a real bug: CustomMethodExtractor.CountMarkerOccurrences is a plain,
    // comment-blind substring scan for the literal "[CliqCustomMethod]" text, used as a
    // cross-check against the brace-aware extractor (see WorkspaceCodeGeneration.WriteBasePageAsync).
    // A generator-authored comment that spelled the marker as real bracketed attribute syntax
    // (explaining that a page COULD override ClickWithFallback with one) used to inflate that
    // count on every single generated BasePage.cs, even with zero real custom methods present -
    // permanently tripping the "needs your decision" conflict path with nothing real behind it
    // for the merge UI to ever show, so BasePage.cs could never regenerate again.
    [Test]
    public void GenerateBasePageFile_NeverSpellsTheMarkerAsLiteralBracketedTextOutsideARealCustomMethod()
    {
        var style = new ExamplePageStyle("driver", "driver");

        var code = PageObjectCodeGenerator.GenerateBasePageFile("Generated", style, 10_000);

        Assert.That(code, Does.Not.Contain($"[{PageObjectCodeGenerator.CustomMethodAttributeName}]"));
    }

    [Test]
    public void GenerateBasePageFile_WithCustomMethods_EmitsThemWithTheSameMarkerCommentAsGenerate()
    {
        var style = new ExamplePageStyle("driver", "driver");
        const string customMethod = """
            [CliqCustomMethod]
            public void TakeScreenshot()
            {
                DoSomethingInstitutionSpecific();
            }
            """;

        var code = PageObjectCodeGenerator.GenerateBasePageFile("Generated", style, 10_000, [customMethod]);

        Assert.Multiple(() =>
        {
            Assert.That(code, Does.Contain("// Added by hand - kept across regeneration because each method below carries"));
            Assert.That(code, Does.Contain("public void TakeScreenshot()"));
        });
    }

    [Test]
    public void GenerateBasePageFile_ExampleStyleWithExplicitWaitFieldName_UsesItInsteadOfDeriving()
    {
        var style = new ExamplePageStyle("driver", "driver", "pageWait");

        var code = PageObjectCodeGenerator.GenerateBasePageFile("Generated", style, 10_000);

        Assert.Multiple(() =>
        {
            Assert.That(code, Does.Contain("protected readonly WebDriverWait pageWait;"));
            Assert.That(code, Does.Contain("pageWait.Until(d => d.FindElement(locator));"));
            Assert.That(code, Does.Not.Contain("protected readonly WebDriverWait wait;"));
        });
    }

    [Test]
    public void Generate_SelfContained_EmitsVisibilityCheckThatCatchesTimeoutInstead()
    {
        var elements = new List<ElementDescriptor>
        {
            new("div", ElementActionKind.Assert, LocatorStrategy.CssSelector, "#banner", "banner"),
        };

        var code = PageObjectCodeGenerator.Generate("LoginPage", "Generated", elements, 10_000, []);

        Assert.Multiple(() =>
        {
            Assert.That(code, Does.Contain("public bool IsBannerVisible()"));
            Assert.That(code, Does.Contain("_wait.Until(d => d.FindElement(BannerLocator));"));
            Assert.That(code, Does.Contain("catch (WebDriverTimeoutException)"));
            Assert.That(code, Does.Contain("return false;"));
        });
    }

    [Test]
    public void Generate_WithBasePageFieldNames_VisibilityCheckUsesTryLocateInstead()
    {
        var elements = new List<ElementDescriptor>
        {
            new("div", ElementActionKind.Assert, LocatorStrategy.CssSelector, "#banner", "banner"),
        };
        var fieldNames = new BasePageFieldNames("driver", "_driver", "_wait");

        var code = PageObjectCodeGenerator.Generate("LoginPage", "Generated", elements, 10_000, [], basePageFieldNames: fieldNames);

        Assert.Multiple(() =>
        {
            Assert.That(code, Does.Contain("public bool IsBannerVisible()"));
            Assert.That(code, Does.Contain("return TryLocate(BannerLocator) is not null;"));
            Assert.That(code, Does.Not.Contain("catch (WebDriverTimeoutException)"));
        });
    }

    [Test]
    public void Generate_WithBasePageFieldNames_AnnotatesEveryGeneratedMemberWithABasePageUsageComment()
    {
        var elements = new List<ElementDescriptor>
        {
            new("button", ElementActionKind.Click, LocatorStrategy.CssSelector, "#submit", "submit"),
            new("input", ElementActionKind.Check, LocatorStrategy.CssSelector, "#agree", "agree"),
            new("div", ElementActionKind.Assert, LocatorStrategy.CssSelector, "#banner", "banner"),
        };
        var fieldNames = new BasePageFieldNames("driver", "_driver", "_wait");

        var code = PageObjectCodeGenerator.Generate("LoginPage", "Generated", elements, 10_000, [], basePageFieldNames: fieldNames);

        Assert.Multiple(() =>
        {
            AssertImmediatelyPrecedes(code,
                "    // BasePage generation: uses the inherited BasePage.Locate, ClickWithFallback.",
                "    public void ClickSubmit()");
            AssertImmediatelyPrecedes(code,
                "    // BasePage generation: uses the inherited BasePage.Locate, Hover.",
                "    public void HoverSubmit()");
            AssertImmediatelyPrecedes(code,
                "    // BasePage generation: uses the inherited BasePage.Locate, SetChecked.",
                "    public void SetAgree(bool isChecked)");
            AssertImmediatelyPrecedes(code,
                "    // BasePage generation: uses the inherited BasePage.Locate.",
                "    public string GetBannerText()");
            AssertImmediatelyPrecedes(code,
                "    // BasePage generation: uses the inherited BasePage.TryLocate.",
                "    public bool IsSubmitVisible()");
        });
    }

    [Test]
    public void Generate_SelfContained_NeverEmitsBasePageUsageComments()
    {
        var elements = new List<ElementDescriptor>
        {
            new("button", ElementActionKind.Click, LocatorStrategy.CssSelector, "#submit", "submit"),
            new("input", ElementActionKind.Check, LocatorStrategy.CssSelector, "#agree", "agree"),
            new("div", ElementActionKind.Assert, LocatorStrategy.CssSelector, "#banner", "banner"),
            new("input", ElementActionKind.Fill, LocatorStrategy.CssSelector, "#username", "username"),
        };

        var code = PageObjectCodeGenerator.Generate("LoginPage", "Generated", elements, 10_000, []);

        Assert.That(code, Does.Not.Contain("BasePage generation:"));
    }

    [Test]
    public void Generate_FillElement_WithBasePageFieldNames_AnnotatesThePropertyAndItsConstructorInit()
    {
        var elements = new List<ElementDescriptor>
        {
            new("input", ElementActionKind.Fill, LocatorStrategy.CssSelector, "#username", "username"),
        };
        var fieldNames = new BasePageFieldNames("driver", "_driver", "_wait");

        var code = PageObjectCodeGenerator.Generate("LoginPage", "Generated", elements, 10_000, [], basePageFieldNames: fieldNames);

        Assert.Multiple(() =>
        {
            AssertImmediatelyPrecedes(code,
                "    // BasePage generation: uses the inherited BasePage.Locate.",
                "    public ElementWrapper<string> Username { get; }");
            AssertImmediatelyPrecedes(code,
                "        // BasePage generation: uses the inherited BasePage.Locate.",
                "        Username = new ElementWrapper<string>(");
        });
    }

    [Test]
    public void Generate_RadioElement_WithBasePageFieldNames_AnnotatesSelectMethodAfterTheLocatorComment()
    {
        var elements = new List<ElementDescriptor>
        {
            new("input", ElementActionKind.Radio, LocatorStrategy.Name, "plan", "plan"),
        };
        var fieldNames = new BasePageFieldNames("driver", "_driver", "_wait");

        var code = PageObjectCodeGenerator.Generate("LoginPage", "Generated", elements, 10_000, [], basePageFieldNames: fieldNames);

        Assert.That(code, Does.Contain(
                "    // Locator (name): Plan = plan\r\n    // BasePage generation: uses the inherited BasePage.Locate, ClickWithFallback.\r\n    public void SelectPlan(string value)")
            .Or.Contain(
                "    // Locator (name): Plan = plan\n    // BasePage generation: uses the inherited BasePage.Locate, ClickWithFallback.\n    public void SelectPlan(string value)"));
    }

    [Test]
    public void Generate_DynamicElement_WithBasePageFieldNames_AlsoGetsABasePageUsageComment()
    {
        var dynamicElements = new List<DynamicElementDescriptor>
        {
            new(ElementActionKind.Click, LocatorStrategy.XPath, "//tr[{param}]/td[2]/button", "rowButton"),
        };
        var fieldNames = new BasePageFieldNames("driver", "_driver", "_wait");

        var code = PageObjectCodeGenerator.Generate("LoginPage", "Generated", [], 10_000, [], dynamicElements, fieldNames);

        Assert.Multiple(() =>
        {
            AssertImmediatelyPrecedes(code,
                "    // BasePage generation: uses the inherited BasePage.Locate, ClickWithFallback.",
                "    public void ClickRowButton(int param)");
            AssertImmediatelyPrecedes(code,
                "    // BasePage generation: uses the inherited BasePage.TryLocate.",
                "    public bool IsRowButtonVisible(int param)");
        });
    }

    [Test]
    public void GenerateBasePageFile_NeverEmitsBasePageUsageComments()
    {
        var style = new ExamplePageStyle("driver", "driver");

        var code = PageObjectCodeGenerator.GenerateBasePageFile("Generated", style, 10_000);

        Assert.That(code, Does.Not.Contain("BasePage generation:"));
    }

    // Radio has no fixed locator (see AppendVisibilityCheck's remarks / staticLocatorElements in
    // Generate) so it's excluded, same as its own static-locator-field exclusion.
    [Test]
    public void Generate_RadioElement_DoesNotGetAVisibilityCheck()
    {
        var elements = new List<ElementDescriptor>
        {
            new("input", ElementActionKind.Radio, LocatorStrategy.Name, "plan", "plan"),
        };

        var code = PageObjectCodeGenerator.Generate("LoginPage", "Generated", elements, 10_000, []);

        Assert.That(code, Does.Not.Contain("Visible"));
    }

    [Test]
    public void Generate_WrappedElement_AlsoGetsAVisibilityCheck()
    {
        var elements = new List<ElementDescriptor>
        {
            new("input", ElementActionKind.Fill, LocatorStrategy.CssSelector, "#username", "username"),
        };

        var code = PageObjectCodeGenerator.Generate("LoginPage", "Generated", elements, 10_000, []);

        Assert.That(code, Does.Contain("public bool IsUsernameVisible()"));
    }

    [Test]
    public void Generate_DynamicElement_GetsAParameterizedVisibilityCheck()
    {
        var dynamicElements = new List<DynamicElementDescriptor>
        {
            new(ElementActionKind.Click, LocatorStrategy.XPath, "//tr[{param}]/td[2]/button", "rowButton"),
        };

        var code = PageObjectCodeGenerator.Generate("LoginPage", "Generated", [], 10_000, [], dynamicElements);

        Assert.That(code, Does.Contain("public bool IsRowButtonVisible(int param)"));
    }

    [Test]
    public void Generate_XPathElement_EmitsFullXPathInAPrecedingComment()
    {
        const string fullXPath = "//label[contains(normalize-space(string(.)), \"Username\")]/following::input[1]";
        var elements = new List<ElementDescriptor>
        {
            new("input", ElementActionKind.Fill, LocatorStrategy.XPath, fullXPath, "username", AbsoluteXpath: "/html[1]/body[1]/div[2]/input[1]"),
        };

        var code = PageObjectCodeGenerator.Generate("LoginPage", "Generated", elements, 10_000, []);
        var commentIndex = code.IndexOf("// Locator (xpath): Username = /html[1]/body[1]/div[2]/input[1]", StringComparison.Ordinal);
        var initIndex = code.IndexOf("Username = new ElementWrapper<string>(", StringComparison.Ordinal);

        Assert.Multiple(() =>
        {
            Assert.That(commentIndex, Is.GreaterThanOrEqualTo(0), "expected the full xpath in a comment above the wrapper init");
            Assert.That(commentIndex, Is.LessThan(initIndex), "the comment should precede the wrapper init it documents");
            // The generated name is part of the comment, and the runtime locator still uses the
            // relative xpath (the absolute path is descriptive only).
            Assert.That(code, Does.Contain("private static readonly By UsernameLocator = By.XPath(\"//label[contains(normalize-space(string(.)), \\\"Username\\\")]/following::input[1]\");"));
        });
    }

    [Test]
    public void Generate_XPathElement_WithoutAbsolutePath_FallsBackToRelativeValueInComment()
    {
        const string fullXPath = "//button[@type='submit']";
        var elements = new List<ElementDescriptor>
        {
            new("button", ElementActionKind.Click, LocatorStrategy.XPath, fullXPath, "submit"),
        };

        var code = PageObjectCodeGenerator.Generate("Page", "Generated", elements, 10_000, []);

        Assert.That(code, Does.Contain("// Locator (xpath): Submit = //button[@type='submit']"));
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
            Assert.That(code, Does.Contain("// Locator (css): Submit = submit"));
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
            Assert.That(code, Does.Contain("// Locator (xpath, template): RowButton = //table/tbody/tr[{param}]/td[1]/button"));
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

    // Cross-platform line-ending safe check that `nextLine` is the line immediately following
    // `precedingLine` - same \r\n-or-\n tolerance as Generate_NoBasePageClassName_ClassIsSelfContained,
    // used here to tie a BasePage-usage comment to the exact member it documents rather than just
    // asserting both substrings exist somewhere in the file.
    private static void AssertImmediatelyPrecedes(string code, string precedingLine, string nextLine)
    {
        Assert.That(code, Does.Contain($"{precedingLine}\r\n{nextLine}").Or.Contain($"{precedingLine}\n{nextLine}"));
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

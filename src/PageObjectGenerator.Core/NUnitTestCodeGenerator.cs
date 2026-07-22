using System.Text;

namespace PageObjectGenerator.Core;

/// <summary>
/// Generates a smoke-test NUnit fixture: one [Test] per discovered element.
/// For Fill/Click/Select/Check elements this asserts presence/visibility/
/// enabled state - deliberately not exercising the action with made-up values,
/// since the generator has no domain knowledge of what a "valid" login is, and
/// invoking a submit button would navigate the page out from under the other
/// generated tests. For Assert elements (headings) it instead asserts the text
/// equals the value captured at generation time - a real snapshot check.
///
/// A [OneTimeSetUp] verifies the whole Page Object once, before any individual
/// [Test] runs: if the page has drifted and three elements are now missing,
/// this fails once with all three named, instead of three separate, less
/// informative per-test failures further down the same run.
/// </summary>
public static class NUnitTestCodeGenerator
{
    public static string Generate(
        string className,
        string @namespace,
        string url,
        BrowserKind browser,
        IReadOnlyList<ElementDescriptor> elements)
    {
        var driverType = browser == BrowserKind.Edge ? "EdgeDriver" : "ChromeDriver";
        var driverUsing = browser == BrowserKind.Edge ? "OpenQA.Selenium.Edge" : "OpenQA.Selenium.Chrome";

        var sb = new StringBuilder();
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using NUnit.Framework;");
        sb.AppendLine("using OpenQA.Selenium;");
        sb.AppendLine($"using {driverUsing};");
        sb.AppendLine();
        sb.AppendLine($"namespace {@namespace};");
        sb.AppendLine();
        sb.AppendLine("[TestFixture]");
        sb.AppendLine($"public sealed class {className}Tests");
        sb.AppendLine("{");
        sb.AppendLine("    private IWebDriver _driver = null!;");
        sb.AppendLine();
        AppendOneTimeSetUp(sb, className, url, driverType, elements);
        sb.AppendLine();
        sb.AppendLine("    [SetUp]");
        sb.AppendLine("    public void SetUp()");
        sb.AppendLine("    {");
        sb.AppendLine($"        _driver = new {driverType}();");
        sb.AppendLine($"        _driver.Navigate().GoToUrl({PageObjectCodeGenerator.ToCSharpStringLiteral(url)});");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    [TearDown]");
        sb.AppendLine("    public void TearDown()");
        sb.AppendLine("    {");
        sb.AppendLine("        _driver.Quit();");
        sb.AppendLine("        _driver.Dispose();");
        sb.AppendLine("    }");

        var usedTestNames = new Dictionary<string, int>();
        foreach (var element in elements)
        {
            sb.AppendLine();
            AppendTest(sb, element, usedTestNames);
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    private static void AppendOneTimeSetUp(
        StringBuilder sb, string className, string url, string driverType, IReadOnlyList<ElementDescriptor> elements)
    {
        sb.AppendLine("    [OneTimeSetUp]");
        sb.AppendLine($"    public void Verify{className}()");
        sb.AppendLine("    {");
        sb.AppendLine($"        using var driver = new {driverType}();");
        sb.AppendLine($"        driver.Navigate().GoToUrl({PageObjectCodeGenerator.ToCSharpStringLiteral(url)});");
        sb.AppendLine("        var missing = new List<string>();");
        sb.AppendLine();

        foreach (var element in elements)
        {
            var by = PageObjectCodeGenerator.ByExpression(element);
            var label = PageObjectCodeGenerator.ToCSharpStringLiteral($"{element.SuggestedName} ({element.Selector})");
            sb.AppendLine($"        if (!TryFind(driver, {by})) missing.Add({label});");
        }

        sb.AppendLine();
        sb.AppendLine("        if (missing.Count > 0)");
        sb.AppendLine("        {");
        sb.AppendLine(
            "            Assert.Fail(\"Page Object verification failed - missing element(s): \" + string.Join(\", \", missing));");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    private static bool TryFind(IWebDriver driver, By by)");
        sb.AppendLine("    {");
        sb.AppendLine("        try");
        sb.AppendLine("        {");
        sb.AppendLine("            driver.FindElement(by);");
        sb.AppendLine("            return true;");
        sb.AppendLine("        }");
        sb.AppendLine("        catch (NoSuchElementException)");
        sb.AppendLine("        {");
        sb.AppendLine("            return false;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
    }

    private static void AppendTest(StringBuilder sb, ElementDescriptor element, Dictionary<string, int> usedTestNames)
    {
        var name = IdentifierNaming.ToPascalCase(element.SuggestedName);
        usedTestNames.TryGetValue(name, out var seen);
        usedTestNames[name] = seen + 1;
        var testName = seen > 0 ? $"{name}_{seen + 1}" : name;

        var by = PageObjectCodeGenerator.ByExpression(element);

        sb.AppendLine("    [Test]");

        if (element.Kind == ElementActionKind.Assert)
        {
            var expected = PageObjectCodeGenerator.ToCSharpStringLiteral(element.ExpectedText ?? "");
            sb.AppendLine($"    public void {testName}_TextMatchesSnapshot()");
            sb.AppendLine("    {");
            sb.AppendLine($"        var element = _driver.FindElement({by});");
            sb.AppendLine($"        Assert.That(element.Text, Is.EqualTo({expected}));");
            sb.AppendLine("    }");
            return;
        }

        sb.AppendLine($"    public void {testName}_IsPresentAndInteractable()");
        sb.AppendLine("    {");
        sb.AppendLine($"        var element = _driver.FindElement({by});");
        sb.AppendLine("        Assert.That(element.Displayed, Is.True);");
        sb.AppendLine("        Assert.That(element.Enabled, Is.True);");
        sb.AppendLine("    }");
    }
}

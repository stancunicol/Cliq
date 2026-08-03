using System.Text;

namespace Cliq.Infrastructure.Generation;

public static class PageObjectCodeGenerator
{
    /// <summary>Attribute name (without the "Attribute" suffix) a hand-written method must carry
    /// to survive regeneration - see <see cref="CustomMethodExtractor"/> and
    /// <see cref="GenerateAttributeFile"/>.</summary>
    public const string CustomMethodAttributeName = "CliqCustomMethod";

    public static string Generate(
        string className,
        string @namespace,
        IReadOnlyList<ElementDescriptor> elements,
        int defaultStepTimeoutMs,
        IReadOnlyList<string> customMethods)
    {
        var sb = new StringBuilder();
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using OpenQA.Selenium;");
        sb.AppendLine("using OpenQA.Selenium.Interactions;");
        sb.AppendLine("using OpenQA.Selenium.Support.UI;");
        sb.AppendLine();
        sb.AppendLine($"namespace {@namespace};");
        sb.AppendLine();
        sb.AppendLine($"public sealed class {className}");
        sb.AppendLine("{");
        sb.AppendLine("    private readonly IWebDriver _driver;");
        sb.AppendLine("    private readonly WebDriverWait _wait;");
        sb.AppendLine();
        sb.AppendLine($"    public {className}(IWebDriver driver)");
        sb.AppendLine("    {");
        sb.AppendLine("        _driver = driver;");
        sb.AppendLine($"        _wait = new WebDriverWait(driver, TimeSpan.FromMilliseconds({defaultStepTimeoutMs}));");
        sb.AppendLine("    }");

        foreach (var element in elements)
        {
            sb.AppendLine();
            AppendMethod(sb, element);
        }

        if (customMethods.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("    // Added by hand - kept across regeneration because each method below carries");
            sb.AppendLine($"    // the {CustomMethodAttributeName} attribute. Remove it to let a method be dropped.");
            foreach (var method in customMethods)
            {
                sb.AppendLine();
                sb.AppendLine(method);
            }
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    /// <summary>
    /// The marker attribute's own definition, generated once per namespace (not per page) since
    /// every Page Object class in the same generated namespace shares it.
    /// </summary>
    public static string GenerateAttributeFile(string @namespace)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"namespace {@namespace};");
        sb.AppendLine();
        sb.AppendLine("// Put this on a hand-written method inside a generated Page Object class to keep");
        sb.AppendLine("// it across regeneration - anything not marked with it is replaced on the next");
        sb.AppendLine("// generation along with the rest of the class.");
        sb.AppendLine("[AttributeUsage(AttributeTargets.Method)]");
        sb.AppendLine($"public sealed class {CustomMethodAttributeName}Attribute : Attribute;");
        return sb.ToString();
    }

    private static void AppendMethod(StringBuilder sb, ElementDescriptor element)
    {
        var name = IdentifierNaming.ToPascalCase(element.SuggestedName);
        var by = ByExpression(element);

        switch (element.Kind)
        {
            case ElementActionKind.Fill:
                sb.AppendLine($"    public void Fill{name}(string value)");
                sb.AppendLine("    {");
                sb.AppendLine($"        var element = _wait.Until(d => d.FindElement({by}));");
                sb.AppendLine("        element.Clear();");
                sb.AppendLine("        element.SendKeys(value);");
                sb.AppendLine("    }");
                break;

            case ElementActionKind.Click:
                sb.AppendLine($"    public void Click{name}()");
                sb.AppendLine("    {");
                sb.AppendLine($"        var element = _wait.Until(d => d.FindElement({by}));");
                sb.AppendLine("        element.Click();");
                sb.AppendLine("    }");
                sb.AppendLine();
                sb.AppendLine($"    public void Hover{name}()");
                sb.AppendLine("    {");
                sb.AppendLine($"        var element = _wait.Until(d => d.FindElement({by}));");
                sb.AppendLine("        new Actions(_driver).MoveToElement(element).Perform();");
                sb.AppendLine("    }");
                break;

            case ElementActionKind.Select:
                sb.AppendLine($"    public void Select{name}(string value)");
                sb.AppendLine("    {");
                sb.AppendLine($"        var element = _wait.Until(d => d.FindElement({by}));");
                sb.AppendLine("        new SelectElement(element).SelectByValue(value);");
                sb.AppendLine("    }");
                break;

            case ElementActionKind.MultiSelect:
                sb.AppendLine($"    public void Select{name}Multiple(IEnumerable<string> values)");
                sb.AppendLine("    {");
                sb.AppendLine($"        var element = _wait.Until(d => d.FindElement({by}));");
                sb.AppendLine("        var select = new SelectElement(element);");
                sb.AppendLine("        select.DeselectAll();");
                sb.AppendLine("        foreach (var value in values)");
                sb.AppendLine("        {");
                sb.AppendLine("            select.SelectByValue(value);");
                sb.AppendLine("        }");
                sb.AppendLine("    }");
                break;

            case ElementActionKind.Check:
                sb.AppendLine($"    public void Set{name}(bool isChecked)");
                sb.AppendLine("    {");
                sb.AppendLine($"        var element = _wait.Until(d => d.FindElement({by}));");
                sb.AppendLine("        if (element.Selected != isChecked)");
                sb.AppendLine("        {");
                sb.AppendLine("            element.Click();");
                sb.AppendLine("        }");
                sb.AppendLine("    }");
                break;

            case ElementActionKind.Radio:
                // No native By overload covers "name AND value", so this one stays
                // CSS - built from a compile-time-known prefix (the group name) plus
                // the runtime value. Single quotes for the CSS attribute value avoid
                // colliding with the double quotes a C# string literal uses; both the
                // group name (escaped here, at generation time) and the runtime value
                // (escaped inline, below) are CSS-escaped so an apostrophe in either
                // can't break the selector.
                var radioPrefix = ToCSharpStringLiteral($"input[name='{CssEscapeSingleQuoted(element.LocatorValue)}'][value='");
                sb.AppendLine($"    public void Select{name}(string value)");
                sb.AppendLine("    {");
                sb.AppendLine(@"        var escapedValue = value.Replace(""\\"", ""\\\\"").Replace(""'"", ""\\'"");");
                sb.AppendLine($"        var element = _wait.Until(d => d.FindElement(By.CssSelector({radioPrefix} + escapedValue + \"']\")));");
                sb.AppendLine("        element.Click();");
                sb.AppendLine("    }");
                break;

            case ElementActionKind.Assert:
                sb.AppendLine($"    public string Get{name}Text()");
                sb.AppendLine("    {");
                sb.AppendLine($"        var element = _wait.Until(d => d.FindElement({by}));");
                sb.AppendLine("        return element.Text;");
                sb.AppendLine("    }");
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(element));
        }
    }

    /// <summary>
    /// Escapes a value for safe embedding inside a single-quoted CSS attribute
    /// selector (e.g. <c>[value='...']</c>) - an unescaped apostrophe in the
    /// value would otherwise terminate the selector early. Used both at
    /// generation time for a Radio group's compile-time-known name.
    /// </summary>
    internal static string CssEscapeSingleQuoted(string value) =>
        value.Replace("\\", "\\\\").Replace("'", "\\'");

    /// <summary>Emits the Selenium locator call as C# source text - By.Id/By.Name where a native strategy applies, By.CssSelector otherwise.</summary>
    internal static string ByExpression(ElementDescriptor element)
    {
        var valueLiteral = ToCSharpStringLiteral(element.LocatorValue);
        return element.Strategy switch
        {
            LocatorStrategy.Id => $"By.Id({valueLiteral})",
            LocatorStrategy.Name => $"By.Name({valueLiteral})",
            LocatorStrategy.CssSelector => $"By.CssSelector({valueLiteral})",
            LocatorStrategy.XPath => $"By.XPath({valueLiteral})",
            _ => throw new ArgumentOutOfRangeException(nameof(element)),
        };
    }

    // Must escape everything a normal (non-verbatim) C# string literal can't
    // contain literally - not just backslash/quote. A page's visible text
    // (e.g. an <h2> snapshot) can carry a real newline from the source HTML's
    // own formatting; embedding that raw would split the generated string
    // literal across two physical lines, which doesn't compile.
    internal static string ToCSharpStringLiteral(string value)
    {
        var sb = new StringBuilder(value.Length + 2);
        sb.Append('"');
        foreach (var c in value)
        {
            switch (c)
            {
                case '\\':
                    sb.Append("\\\\");
                    break;
                case '"':
                    sb.Append("\\\"");
                    break;
                case '\r':
                    sb.Append("\\r");
                    break;
                case '\n':
                    sb.Append("\\n");
                    break;
                case '\t':
                    sb.Append("\\t");
                    break;
                default:
                    if (char.IsControl(c))
                    {
                        sb.Append("\\u").Append(((int)c).ToString("x4"));
                    }
                    else
                    {
                        sb.Append(c);
                    }

                    break;
            }
        }

        sb.Append('"');
        return sb.ToString();
    }
}

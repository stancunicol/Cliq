using System.Text;

namespace PageObjectGenerator.Core;

public static class PageObjectCodeGenerator
{
    public static string Generate(string className, string @namespace, IReadOnlyList<ElementDescriptor> elements)
    {
        var sb = new StringBuilder();
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using OpenQA.Selenium;");
        sb.AppendLine("using OpenQA.Selenium.Interactions;");
        sb.AppendLine("using OpenQA.Selenium.Support.UI;");
        sb.AppendLine();
        sb.AppendLine($"namespace {@namespace};");
        sb.AppendLine();
        sb.AppendLine($"public sealed partial class {className}");
        sb.AppendLine("{");
        sb.AppendLine("    private readonly IWebDriver _driver;");
        sb.AppendLine("    private readonly WebDriverWait _wait;");
        sb.AppendLine();
        sb.AppendLine($"    public {className}(IWebDriver driver)");
        sb.AppendLine("    {");
        sb.AppendLine("        _driver = driver;");
        sb.AppendLine("        _wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));");
        sb.AppendLine("    }");

        foreach (var element in elements)
        {
            sb.AppendLine();
            AppendMethod(sb, element);
        }

        sb.AppendLine("}");
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
    /// The other half of the "{className}.cs" partial class - written only once
    /// (see PageObjectFileWriter.Write) and never touched again, so a developer
    /// can add hand-written methods here without losing them on regeneration.
    /// </summary>
    public static string GenerateCustomStub(string className, string @namespace)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"namespace {@namespace};");
        sb.AppendLine();
        sb.AppendLine($"// Hand-written additions to {className} - this file is never regenerated");
        sb.AppendLine("// or overwritten by the tool, so it's safe to add your own methods here.");
        sb.AppendLine($"public sealed partial class {className}");
        sb.AppendLine("{");
        sb.AppendLine("}");
        return sb.ToString();
    }

    /// <summary>
    /// Escapes a value for safe embedding inside a single-quoted CSS attribute
    /// selector (e.g. <c>[value='...']</c>) - an unescaped apostrophe in the
    /// value would otherwise terminate the selector early. Used both at
    /// generation time (for a Radio group's compile-time-known name) and at
    /// runtime (<see cref="PageObjectRunner"/>, same assembly).
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

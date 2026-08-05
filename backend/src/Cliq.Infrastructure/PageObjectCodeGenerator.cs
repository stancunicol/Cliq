using System.Text;

namespace Cliq.Infrastructure.Generation;

public static class PageObjectCodeGenerator
{
    /// <summary>Attribute name (without the "Attribute" suffix) a hand-written method must carry
    /// to survive regeneration - see <see cref="CustomMethodExtractor"/> and
    /// <see cref="GenerateAttributeFile"/>.</summary>
    public const string CustomMethodAttributeName = "CliqCustomMethod";

    /// <summary>Class name of the generated helper that pairs a locator with typed read/write
    /// access - see <see cref="GenerateElementWrapperFile"/>.</summary>
    public const string ElementWrapperClassName = "ElementWrapper";

    public static string Generate(
        string className,
        string @namespace,
        IReadOnlyList<ElementDescriptor> elements,
        int defaultStepTimeoutMs,
        IReadOnlyList<string> customMethods)
    {
        var wrapped = elements.Where(IsWrapped).ToList();
        var plain = elements.Where(element => !IsWrapped(element)).ToList();

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

        // Fill/Select elements get a locator-and-data wrapper property instead of a plain
        // action method - see ElementWrapper<TData> in GenerateElementWrapperFile. Everything
        // else (Click, Assert, Check, Radio) has no associated data to read or write, so it
        // keeps the plain-method shape below rather than being forced through a wrapper with an
        // unused/meaningless data type.
        foreach (var element in wrapped)
        {
            var name = IdentifierNaming.ToPascalCase(element.SuggestedName);
            sb.AppendLine($"    public {ElementWrapperClassName}<{WrapperDataType(element)}> {name} {{ get; }}");
        }

        sb.AppendLine();
        sb.AppendLine($"    public {className}(IWebDriver driver)");
        sb.AppendLine("    {");
        sb.AppendLine("        _driver = driver;");
        sb.AppendLine($"        _wait = new WebDriverWait(driver, TimeSpan.FromMilliseconds({defaultStepTimeoutMs}));");

        foreach (var element in wrapped)
        {
            sb.AppendLine();
            AppendWrappedInit(sb, element);
        }

        sb.AppendLine("    }");

        foreach (var element in plain)
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

    /// <summary>
    /// The <c>ElementWrapper&lt;TData&gt;</c> definition, generated once per namespace (not per
    /// page, same reasoning as <see cref="GenerateAttributeFile"/>). Pairs a locator with typed
    /// read/write access to its current value; each Fill/Select property in a generated Page
    /// Object constructs one with element-specific upload/read delegates (see
    /// <see cref="AppendWrappedInit"/>), so the type-specific Selenium calls live in the
    /// generated Page Object, not in this generic shell.
    /// </summary>
    public static string GenerateElementWrapperFile(string @namespace)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"namespace {@namespace};");
        sb.AppendLine();
        sb.AppendLine("// Pairs a locator with typed read/write access to its current value - navigate to");
        sb.AppendLine($"// the property on a generated Page Object ({{page}}.{{ElementName}}) to reach both.");
        sb.AppendLine($"public sealed class {ElementWrapperClassName}<TData>");
        sb.AppendLine("{");
        sb.AppendLine("    private readonly Action<TData> _upload;");
        sb.AppendLine("    private readonly Func<TData> _read;");
        sb.AppendLine();
        sb.AppendLine($"    public {ElementWrapperClassName}(Action<TData> upload, Func<TData> read)");
        sb.AppendLine("    {");
        sb.AppendLine("        _upload = upload;");
        sb.AppendLine("        _read = read;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    public void Upload(TData value) => _upload(value);");
        sb.AppendLine();
        sb.AppendLine("    public TData Read() => _read();");
        sb.AppendLine("}");
        return sb.ToString();
    }

    private static bool IsWrapped(ElementDescriptor element) =>
        element.Kind is ElementActionKind.Fill or ElementActionKind.Select or ElementActionKind.MultiSelect;

    private static string WrapperDataType(ElementDescriptor element) => element.Kind switch
    {
        ElementActionKind.Fill => element.DataType,
        ElementActionKind.Select => "string",
        ElementActionKind.MultiSelect => "string[]",
        _ => throw new ArgumentOutOfRangeException(nameof(element)),
    };

    private static void AppendWrappedInit(StringBuilder sb, ElementDescriptor element)
    {
        var name = IdentifierNaming.ToPascalCase(element.SuggestedName);
        var by = ByExpression(element);
        var dataType = WrapperDataType(element);
        var (upload, read) = WrapperBody(element, by);

        sb.AppendLine($"        // Locator ({StrategyLabel(element.Strategy)}): {SingleLine(element.LocatorValue)}");
        sb.AppendLine($"        {name} = new {ElementWrapperClassName}<{dataType}>(");
        sb.AppendLine("            value =>");
        sb.AppendLine("            {");
        foreach (var line in upload)
        {
            sb.AppendLine($"                {line}");
        }

        sb.AppendLine("            },");
        sb.AppendLine("            () =>");
        sb.AppendLine("            {");
        foreach (var line in read)
        {
            sb.AppendLine($"                {line}");
        }

        sb.AppendLine("            });");
    }

    /// <summary>Upload/read body lines (without leading indentation - the caller supplies it) for
    /// a wrapped element's constructor initializer.</summary>
    private static (string[] Upload, string[] Read) WrapperBody(ElementDescriptor element, string by)
    {
        var locate = $"var element = _wait.Until(d => d.FindElement({by}));";
        return element.Kind switch
        {
            ElementActionKind.Fill when element.DataType == "int" => (
                [locate, "element.Clear();", "element.SendKeys(value.ToString());"],
                [locate, "return int.Parse(element.GetAttribute(\"value\")!);"]),
            ElementActionKind.Fill when element.DataType == "DateTime" => (
                [locate, "element.Clear();", "element.SendKeys(value.ToString(\"yyyy-MM-dd\"));"],
                [locate, "return DateTime.Parse(element.GetAttribute(\"value\")!);"]),
            ElementActionKind.Fill => (
                [locate, "element.Clear();", "element.SendKeys(value);"],
                [locate, "return element.GetAttribute(\"value\") ?? \"\";"]),
            ElementActionKind.Select => (
                [locate, "new SelectElement(element).SelectByValue(value);"],
                [locate, "return new SelectElement(element).SelectedOption.GetAttribute(\"value\") ?? \"\";"]),
            ElementActionKind.MultiSelect => (
                [
                    locate,
                    "var select = new SelectElement(element);",
                    "select.DeselectAll();",
                    "foreach (var item in value)",
                    "{",
                    "    select.SelectByValue(item);",
                    "}",
                ],
                [locate, "return new SelectElement(element).AllSelectedOptions.Select(o => o.GetAttribute(\"value\") ?? \"\").ToArray();"]),
            _ => throw new ArgumentOutOfRangeException(nameof(element)),
        };
    }

    private static void AppendMethod(StringBuilder sb, ElementDescriptor element)
    {
        var name = IdentifierNaming.ToPascalCase(element.SuggestedName);
        var by = ByExpression(element);

        // One line of provenance per generated element - for an xpath-strategy element this is
        // the full xpath, so the identifier stays short and readable while the exact expression
        // it resolves to is still visible right above it. A raw newline in the value would end
        // the // comment early and turn the rest of it into (very likely non-compiling) code, so
        // it's collapsed to a single line first regardless of how unlikely that input is upstream.
        sb.AppendLine($"    // Locator ({StrategyLabel(element.Strategy)}): {SingleLine(element.LocatorValue)}");

        switch (element.Kind)
        {
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

    private static string StrategyLabel(LocatorStrategy strategy) => strategy switch
    {
        LocatorStrategy.Id => "id",
        LocatorStrategy.Name => "name",
        LocatorStrategy.CssSelector => "css",
        LocatorStrategy.XPath => "xpath",
        _ => strategy.ToString(),
    };

    private static string SingleLine(string value) => value.Replace("\r", "").Replace("\n", " ");

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

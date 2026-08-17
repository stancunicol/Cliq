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
        IReadOnlyList<string> customMethods,
        // Nullable/no-default-collection so every existing call site (mostly tests) keeps
        // compiling unchanged - see DynamicElementDescriptor.
        IReadOnlyList<DynamicElementDescriptor>? dynamicElements = null)
    {
        var wrapped = elements.Where(element => IsWrapped(element.Kind)).ToList();
        var plain = elements.Where(element => !IsWrapped(element.Kind)).ToList();
        // Every kind except Radio has a fixed locator, knowable entirely at generation time, so
        // it becomes a shared `private static readonly By` field instead of being rebuilt inline
        // every time a method/wrapper runs (previously e.g. Click+Hover each constructed their
        // own By.X(...) for the same element). Radio's locator depends on a runtime value (the
        // option to select), so it's excluded here and keeps building its By inline - see
        // AppendMethod's Radio case.
        var staticLocatorElements = elements.Where(element => element.Kind != ElementActionKind.Radio).ToList();

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

        foreach (var element in staticLocatorElements)
        {
            sb.AppendLine();
            AppendStaticLocatorField(sb, element);
        }

        // Fill/Select elements get a locator-and-data wrapper property instead of a plain
        // action method - see ElementWrapper<TData> in GenerateElementWrapperFile. Everything
        // else (Click, Assert, Check, Radio) has no associated data to read or write, so it
        // keeps the plain-method shape below rather than being forced through a wrapper with an
        // unused/meaningless data type.
        foreach (var element in wrapped)
        {
            var name = IdentifierNaming.ToPascalCase(element.SuggestedName);
            sb.AppendLine($"    public {ElementWrapperClassName}<{WrapperDataType(element.Kind, element.DataType)}> {name} {{ get; }}");
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

        sb.AppendLine();
        sb.AppendLine("    // A transient overlay (an ad iframe, a tooltip, a toast) can sit on top of an");
        sb.AppendLine("    // otherwise interactable element for a moment, which Selenium's real-mouse-click");
        sb.AppendLine("    // simulation refuses to click through even though the element itself is fine. A");
        sb.AppendLine("    // JS-dispatched click bypasses that check, so it's used only as a fallback.");
        sb.AppendLine("    private void ClickWithFallback(IWebElement element)");
        sb.AppendLine("    {");
        sb.AppendLine("        try");
        sb.AppendLine("        {");
        sb.AppendLine("            element.Click();");
        sb.AppendLine("        }");
        sb.AppendLine("        catch (ElementClickInterceptedException)");
        sb.AppendLine("        {");
        sb.AppendLine("            ((IJavaScriptExecutor)_driver).ExecuteScript(\"arguments[0].click();\", element);");
        sb.AppendLine("        }");
        sb.AppendLine("    }");

        foreach (var element in plain)
        {
            sb.AppendLine();
            AppendMethod(sb, element);
        }

        foreach (var element in dynamicElements ?? [])
        {
            sb.AppendLine();
            AppendDynamicFactory(sb, element);
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

    private static bool IsWrapped(ElementActionKind kind) =>
        kind is ElementActionKind.Fill or ElementActionKind.Select or ElementActionKind.MultiSelect;

    private static string WrapperDataType(ElementActionKind kind, string dataType) => kind switch
    {
        ElementActionKind.Fill => dataType,
        ElementActionKind.Select => "string",
        ElementActionKind.MultiSelect => "string[]",
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    /// <summary>Emits the `// Locator (...)` comment plus the `private static readonly By
    /// {Name}Locator = ...;` field it documents - shared by every non-Radio element, whether it
    /// ends up as an <see cref="ElementWrapper{TData}"/> property (<see cref="AppendWrappedInit"/>)
    /// or a plain method (<see cref="AppendMethod"/>).</summary>
    private static void AppendStaticLocatorField(StringBuilder sb, ElementDescriptor element)
    {
        var name = IdentifierNaming.ToPascalCase(element.SuggestedName);
        sb.AppendLine($"    // Locator ({StrategyLabel(element.Strategy)}): {SingleLine(element.LocatorValue)}");
        sb.AppendLine($"    private static readonly By {name}Locator = {ByExpression(element)};");
    }

    private static void AppendWrappedInit(StringBuilder sb, ElementDescriptor element)
    {
        var name = IdentifierNaming.ToPascalCase(element.SuggestedName);
        var locatorRef = $"{name}Locator";
        var dataType = WrapperDataType(element.Kind, element.DataType);
        var body = WrapperBody(element.Kind, element.DataType, locatorRef);

        AppendWrapperConstruction(sb, "        ", $"{name} = ", dataType, body);
    }

    /// <summary>
    /// Emits the <c>new ElementWrapper&lt;T&gt;(value => {...}, () => {...});</c> block shared by
    /// a static element's field initializer (<see cref="AppendWrappedInit"/>, which assigns it to
    /// <paramref name="prefix"/>) and a dynamic element's factory method (the wrapped branch of
    /// <see cref="AppendDynamicFactory"/>, which returns it instead - <paramref name="prefix"/>
    /// empty there since the preceding <c>=&gt;</c> line already supplies the "return").
    /// </summary>
    private static void AppendWrapperConstruction(
        StringBuilder sb,
        string indent,
        string prefix,
        string dataType,
        (string[] Upload, string[] Read) body)
    {
        sb.AppendLine($"{indent}{prefix}new {ElementWrapperClassName}<{dataType}>(");
        sb.AppendLine($"{indent}    value =>");
        sb.AppendLine($"{indent}    {{");
        foreach (var line in body.Upload)
        {
            sb.AppendLine($"{indent}        {line}");
        }

        sb.AppendLine($"{indent}    }},");
        sb.AppendLine($"{indent}    () =>");
        sb.AppendLine($"{indent}    {{");
        foreach (var line in body.Read)
        {
            sb.AppendLine($"{indent}        {line}");
        }

        sb.AppendLine($"{indent}    }});");
    }

    /// <summary>Upload/read body lines (without leading indentation - the caller supplies it) for
    /// a wrapped element's constructor initializer (static) or factory method (dynamic).</summary>
    private static (string[] Upload, string[] Read) WrapperBody(ElementActionKind kind, string dataType, string locatorExpr)
    {
        var locate = $"var element = _wait.Until(d => d.FindElement({locatorExpr}));";
        return (kind, dataType) switch
        {
            (ElementActionKind.Fill, "int") => (
                [locate, "element.Clear();", "element.SendKeys(value.ToString());"],
                [locate, "return int.Parse(element.GetAttribute(\"value\")!);"]),
            (ElementActionKind.Fill, "DateTime") => (
                [locate, "element.Clear();", "element.SendKeys(value.ToString(\"yyyy-MM-dd\"));"],
                [locate, "return DateTime.Parse(element.GetAttribute(\"value\")!);"]),
            (ElementActionKind.Fill, _) => (
                [locate, "element.Clear();", "element.SendKeys(value);"],
                [locate, "return element.GetAttribute(\"value\") ?? \"\";"]),
            (ElementActionKind.Select, _) => (
                [locate, "new SelectElement(element).SelectByValue(value);"],
                [locate, "return new SelectElement(element).SelectedOption.GetAttribute(\"value\") ?? \"\";"]),
            (ElementActionKind.MultiSelect, _) => (
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
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };
    }

    private static void AppendMethod(StringBuilder sb, ElementDescriptor element)
    {
        var name = IdentifierNaming.ToPascalCase(element.SuggestedName);
        var locatorRef = $"{name}Locator";

        switch (element.Kind)
        {
            case ElementActionKind.Click:
                sb.AppendLine($"    public void Click{name}()");
                sb.AppendLine("    {");
                sb.AppendLine($"        var element = _wait.Until(d => d.FindElement({locatorRef}));");
                sb.AppendLine("        ClickWithFallback(element);");
                sb.AppendLine("    }");
                sb.AppendLine();
                sb.AppendLine($"    public void Hover{name}()");
                sb.AppendLine("    {");
                sb.AppendLine($"        var element = _wait.Until(d => d.FindElement({locatorRef}));");
                sb.AppendLine("        new Actions(_driver).MoveToElement(element).Perform();");
                sb.AppendLine("    }");
                break;

            case ElementActionKind.Check:
                sb.AppendLine($"    public void Set{name}(bool isChecked)");
                sb.AppendLine("    {");
                sb.AppendLine($"        var element = _wait.Until(d => d.FindElement({locatorRef}));");
                sb.AppendLine("        if (element.Selected != isChecked)");
                sb.AppendLine("        {");
                sb.AppendLine("            ClickWithFallback(element);");
                sb.AppendLine("        }");
                sb.AppendLine("    }");
                break;

            case ElementActionKind.Radio:
                // Radio has no static locator field (unlike every other kind, see
                // staticLocatorElements in Generate) - its By depends on the runtime value passed
                // to Select{name}, built from a compile-time-known prefix (the group name) plus
                // that value, so it can't be a single fixed By and keeps its own comment here.
                // No native By overload covers "name AND value", so this one stays CSS. Single
                // quotes for the CSS attribute value avoid colliding with the double quotes a C#
                // string literal uses; both the group name (escaped here, at generation time) and
                // the runtime value (escaped inline, below) are CSS-escaped so an apostrophe in
                // either can't break the selector.
                sb.AppendLine($"    // Locator ({StrategyLabel(element.Strategy)}): {SingleLine(element.LocatorValue)}");
                var radioPrefix = ToCSharpStringLiteral($"input[name='{CssEscapeSingleQuoted(element.LocatorValue)}'][value='");
                sb.AppendLine($"    public void Select{name}(string value)");
                sb.AppendLine("    {");
                sb.AppendLine(@"        var escapedValue = value.Replace(""\\"", ""\\\\"").Replace(""'"", ""\\'"");");
                sb.AppendLine($"        var element = _wait.Until(d => d.FindElement(By.CssSelector({radioPrefix} + escapedValue + \"']\")));");
                sb.AppendLine("        ClickWithFallback(element);");
                sb.AppendLine("    }");
                break;

            case ElementActionKind.Assert:
                sb.AppendLine($"    public string Get{name}Text()");
                sb.AppendLine("    {");
                sb.AppendLine($"        var element = _wait.Until(d => d.FindElement({locatorRef}));");
                sb.AppendLine("        return element.Text;");
                sb.AppendLine("    }");
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(element));
        }
    }

    /// <summary>
    /// Emits a dynamic element's generated member: a parameterized factory instead of a fixed
    /// field/property, since its locator depends on the caller-supplied <c>param</c> and can't be
    /// precomputed. Fill/Select reuse the same <see cref="ElementWrapper{TData}"/> shape as static
    /// elements, just returned from a method instead of exposed as a property; Click/Assert reuse
    /// the same plain-method shape, with <c>param</c> added as a leading parameter.
    /// </summary>
    private static void AppendDynamicFactory(StringBuilder sb, DynamicElementDescriptor element)
    {
        var name = IdentifierNaming.ToPascalCase(element.SuggestedName);
        var paramType = element.ParameterType == "string" ? "string" : "int";
        var locatorExpr = InterpolatedByExpression(element);

        sb.AppendLine($"    // Locator ({StrategyLabel(element.Strategy)}, template): {SingleLine(element.LocatorTemplate)}");

        if (IsWrapped(element.Kind))
        {
            var dataType = WrapperDataType(element.Kind, element.DataType);
            var body = WrapperBody(element.Kind, element.DataType, locatorExpr);

            sb.AppendLine($"    public {ElementWrapperClassName}<{dataType}> {name}({paramType} param) =>");
            AppendWrapperConstruction(sb, "        ", string.Empty, dataType, body);
            return;
        }

        switch (element.Kind)
        {
            case ElementActionKind.Click:
                sb.AppendLine($"    public void Click{name}({paramType} param)");
                sb.AppendLine("    {");
                sb.AppendLine($"        var element = _wait.Until(d => d.FindElement({locatorExpr}));");
                sb.AppendLine("        ClickWithFallback(element);");
                sb.AppendLine("    }");
                sb.AppendLine();
                sb.AppendLine($"    public void Hover{name}({paramType} param)");
                sb.AppendLine("    {");
                sb.AppendLine($"        var element = _wait.Until(d => d.FindElement({locatorExpr}));");
                sb.AppendLine("        new Actions(_driver).MoveToElement(element).Perform();");
                sb.AppendLine("    }");
                break;

            case ElementActionKind.Assert:
                sb.AppendLine($"    public string Get{name}Text({paramType} param)");
                sb.AppendLine("    {");
                sb.AppendLine($"        var element = _wait.Until(d => d.FindElement({locatorExpr}));");
                sb.AppendLine("        return element.Text;");
                sb.AppendLine("    }");
                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(element), $"Dynamic elements do not support {element.Kind}.");
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

    /// <summary>Emits the Selenium locator call as C# source text. Only css/xpath are handled -
    /// discovery only ever produces testId (itself emitted as a css attribute selector - see
    /// WorkspaceCodeGeneration.ToDescriptor), xpath, or a structural css fallback for a regular
    /// element; Id/Name are not real identification strategies here (id is excluded entirely per
    /// elementIdentification settings, kept only as a naming hint, and name is reserved for a
    /// Radio group's own selector, built separately in AppendMethod rather than through this
    /// method) so they're rejected instead of silently emitting a locator nothing produces.</summary>
    internal static string ByExpression(ElementDescriptor element)
    {
        var valueLiteral = ToCSharpStringLiteral(element.LocatorValue);
        return element.Strategy switch
        {
            LocatorStrategy.CssSelector => $"By.CssSelector({valueLiteral})",
            LocatorStrategy.XPath => $"By.XPath({valueLiteral})",
            _ => throw new ArgumentOutOfRangeException(
                nameof(element), $"Locator strategy '{element.Strategy}' is not supported - only css and xpath are."),
        };
    }

    /// <summary>Like <see cref="ByExpression"/> but for a dynamic element's template - the
    /// resulting By.X(...) call takes an interpolated string instead of a plain literal, so the
    /// generated factory method's own `param` fills in the variable part at call time. Same css/
    /// xpath-only restriction as <see cref="ByExpression"/>, enforced upstream too (a dynamic
    /// element's Strategy is validated at save time - see WorkspaceRepository.ValidateDynamicElements).</summary>
    internal static string InterpolatedByExpression(DynamicElementDescriptor element)
    {
        var literal = ToInterpolatedCSharpStringLiteral(element.LocatorTemplate);
        return element.Strategy switch
        {
            LocatorStrategy.CssSelector => $"By.CssSelector({literal})",
            LocatorStrategy.XPath => $"By.XPath({literal})",
            _ => throw new ArgumentOutOfRangeException(
                nameof(element), $"Locator strategy '{element.Strategy}' is not supported - only css and xpath are."),
        };
    }

    internal static string StrategyLabel(LocatorStrategy strategy) => strategy switch
    {
        LocatorStrategy.Name => "name",
        LocatorStrategy.CssSelector => "css",
        LocatorStrategy.XPath => "xpath",
        _ => strategy.ToString(),
    };

    internal static string SingleLine(string value) => value.Replace("\r", "").Replace("\n", " ");

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
            AppendEscapedChar(sb, c);
        }

        sb.Append('"');
        return sb.ToString();
    }

    // Escapes everything a normal (non-verbatim) C# string literal can't contain literally.
    // Shared by ToCSharpStringLiteral and ToInterpolatedCSharpStringLiteral - the latter handles
    // '{'/'}' itself first (an interpolation concern this method knows nothing about) and routes
    // every other character through here.
    private static void AppendEscapedChar(StringBuilder sb, char c)
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

    /// <summary>
    /// Like <see cref="ToCSharpStringLiteral"/> but produces a C# interpolated string
    /// (<c>$"..."</c>) instead of a plain one, for a dynamic element's user-authored locator
    /// template (see <see cref="DynamicElementDescriptor"/>). The literal token <c>{param}</c> in
    /// <paramref name="template"/> is left as-is so it becomes a real interpolation hole bound to
    /// the generated method's own <c>param</c> parameter; any other <c>{</c>/<c>}</c> in the
    /// template is escaped as <c>{{</c>/<c>}}</c> so it can't be mistaken for one.
    /// </summary>
    internal static string ToInterpolatedCSharpStringLiteral(string template)
    {
        const string placeholder = "{param}";
        var sb = new StringBuilder(template.Length + 3);
        sb.Append("$\"");
        var i = 0;
        while (i < template.Length)
        {
            if (i + placeholder.Length <= template.Length
                && template.Substring(i, placeholder.Length) == placeholder)
            {
                sb.Append(placeholder);
                i += placeholder.Length;
                continue;
            }

            var c = template[i];
            if (c == '{')
            {
                sb.Append("{{");
            }
            else if (c == '}')
            {
                sb.Append("}}");
            }
            else
            {
                AppendEscapedChar(sb, c);
            }

            i++;
        }

        sb.Append('"');
        return sb.ToString();
    }
}

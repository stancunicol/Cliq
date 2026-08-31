using System.Text;

namespace Cliq.Infrastructure.Generation;

/// <summary>
/// Field/parameter names a generated Page Object should use for its driver/wait access - either
/// today's Cliq defaults (self-contained class, no shared base) or names mirrored from an
/// example page object when <see cref="Cliq.Domain.CliqSettings.ExamplePageObjectFilePath"/> is
/// set, in which case they're also what the shared, Cliq-generated <c>BasePage</c> class itself
/// declares (see <see cref="PageObjectCodeGenerator.GenerateBasePageFile"/>) - every derived page
/// then just uses the inherited fields under these same names instead of declaring its own.
/// </summary>
public sealed record BasePageFieldNames(string DriverParameterName, string DriverField, string WaitField)
{
    public static BasePageFieldNames FromStyle(ExamplePageStyle style) => new(
        style.DriverParameterName,
        style.DriverFieldName,
        style.WaitFieldName ?? ExamplePageStyleReader.DeriveWaitFieldName(style.DriverFieldName));
}

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
        IReadOnlyList<DynamicElementDescriptor>? dynamicElements = null,
        // Non-null when CliqSettings.ExamplePageObjectFilePath is set: the class inherits from
        // the shared, Cliq-generated `BasePage` (see GenerateBasePageFile) instead of being
        // self-contained, and uses these field names (mirrored from the example) in place of the
        // defaults below wherever it would otherwise reference its own driver/wait.
        BasePageFieldNames? basePageFieldNames = null)
    {
        var driverParam = basePageFieldNames?.DriverParameterName ?? "driver";
        var driverField = basePageFieldNames?.DriverField ?? "_driver";
        var waitField = basePageFieldNames?.WaitField ?? "_wait";

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
        var inheritanceSuffix = basePageFieldNames is not null ? " : BasePage" : "";
        sb.AppendLine($"public sealed class {className}{inheritanceSuffix}");
        sb.AppendLine("{");

        // In base-page mode these live on the shared BasePage instead (see
        // GenerateBasePageFile) - every method below still reads/writes them, just through the
        // inherited protected fields (same names, driverField/waitField) rather than local ones.
        if (basePageFieldNames is null)
        {
            sb.AppendLine($"    private readonly IWebDriver {driverField};");
            sb.AppendLine($"    private readonly WebDriverWait {waitField};");
        }

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
            AppendBasePageUsageComment(sb, basePageFieldNames, "    ", "Locate");
            sb.AppendLine($"    public {ElementWrapperClassName}<{WrapperDataType(element.Kind, element.DataType)}> {name} {{ get; }}");
        }

        var baseConstructorCall = basePageFieldNames is not null ? $" : base({driverParam})" : "";
        sb.AppendLine();
        sb.AppendLine($"    public {className}(IWebDriver {driverParam}){baseConstructorCall}");
        sb.AppendLine("    {");
        if (basePageFieldNames is null)
        {
            sb.AppendLine($"        {driverField} = {driverParam};");
            sb.AppendLine($"        {waitField} = new WebDriverWait({driverParam}, TimeSpan.FromMilliseconds({defaultStepTimeoutMs}));");
        }

        foreach (var element in wrapped)
        {
            sb.AppendLine();
            AppendWrappedInit(sb, element, basePageFieldNames);
        }

        sb.AppendLine("    }");

        if (basePageFieldNames is null)
        {
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
            sb.AppendLine($"            ((IJavaScriptExecutor){driverField}).ExecuteScript(\"arguments[0].click();\", element);");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
        }

        foreach (var element in plain)
        {
            sb.AppendLine();
            AppendMethod(sb, element, basePageFieldNames);
        }

        // A visibility check for every element with a fixed locator (Radio excluded, same
        // reasoning as staticLocatorElements above - its locator isn't fixed) - lets a caller
        // treat "not there" as a legitimate outcome instead of an exception, e.g. a conditionally
        // shown banner or an optional field. Covers wrapped elements too, not just `plain`, since
        // their static locator field exists regardless of which loop emits their primary member.
        foreach (var element in staticLocatorElements)
        {
            sb.AppendLine();
            AppendVisibilityCheck(sb, element, basePageFieldNames);
        }

        foreach (var element in dynamicElements ?? [])
        {
            sb.AppendLine();
            AppendDynamicFactory(sb, element, basePageFieldNames);
        }

        AppendCustomMethods(sb, customMethods);

        sb.AppendLine("}");
        return sb.ToString();
    }

    /// <summary>Emits the hand-written methods a caller chose to carry over (see
    /// <see cref="Generate"/>'s and <see cref="GenerateBasePageFile"/>'s own <c>customMethods</c>
    /// parameters) with the comment explaining why they're there - shared so both call sites emit
    /// the identical block instead of it being duplicated.</summary>
    private static void AppendCustomMethods(StringBuilder sb, IReadOnlyList<string> customMethods)
    {
        if (customMethods.Count == 0)
        {
            return;
        }

        sb.AppendLine();
        sb.AppendLine("    // Added by hand - kept across regeneration because each method below carries");
        sb.AppendLine($"    // the {CustomMethodAttributeName} attribute. Remove it to let a method be dropped.");
        foreach (var method in customMethods)
        {
            sb.AppendLine();
            sb.AppendLine(method);
        }
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

    /// <summary>
    /// The shared <c>BasePage</c> definition, generated once per namespace (not per page, same
    /// reasoning as <see cref="GenerateAttributeFile"/>) - only produced when
    /// <see cref="Cliq.Domain.CliqSettings.ExamplePageObjectFilePath"/> is set. Holds exactly what
    /// every self-contained generated page would otherwise duplicate (driver/wait fields,
    /// <c>ClickWithFallback</c>), under names mirrored from the example
    /// (<paramref name="style"/>) rather than Cliq's own defaults. Deliberately always keeps
    /// `WebDriverWait`-based lookup regardless of whether the example itself waits for elements -
    /// see PageObjectCodeGenerator.Generate's basePageFieldNames remarks: only naming is a style
    /// choice here, not this reliability behavior.
    /// </summary>
    public static string GenerateBasePageFile(
        string @namespace,
        ExamplePageStyle style,
        int defaultStepTimeoutMs,
        // Hand-written methods (marked [CliqCustomMethod]) recovered from the previously
        // generated BasePage.cs - see WorkspaceCodeGeneration.GenerateAsync, which runs this file
        // through the same recovery/decision flow as every per-page file before calling here.
        // Empty by default so existing callers (tests) keep compiling unchanged.
        IReadOnlyList<string>? customMethods = null)
    {
        var fieldNames = BasePageFieldNames.FromStyle(style);

        var sb = new StringBuilder();
        sb.AppendLine("using OpenQA.Selenium;");
        sb.AppendLine("using OpenQA.Selenium.Interactions;");
        sb.AppendLine("using OpenQA.Selenium.Support.UI;");
        sb.AppendLine();
        sb.AppendLine($"namespace {@namespace};");
        sb.AppendLine();
        sb.AppendLine("public abstract class BasePage");
        sb.AppendLine("{");
        sb.AppendLine($"    protected readonly IWebDriver {fieldNames.DriverField};");
        sb.AppendLine($"    protected readonly WebDriverWait {fieldNames.WaitField};");
        sb.AppendLine();
        sb.AppendLine($"    protected BasePage(IWebDriver {fieldNames.DriverParameterName})");
        sb.AppendLine("    {");
        sb.AppendLine($"        {fieldNames.DriverField} = {fieldNames.DriverParameterName};");
        sb.AppendLine($"        {fieldNames.WaitField} = new WebDriverWait({fieldNames.DriverParameterName}, TimeSpan.FromMilliseconds({defaultStepTimeoutMs}));");
        sb.AppendLine("    }");
        // This comment block must never spell the marker as the literal bracketed attribute text
        // ("[" + CustomMethodAttributeName + "]") - CustomMethodExtractor.CountMarkerOccurrences is
        // a plain, comment-blind substring scan for exactly that text, used as a cross-check
        // against the brace-aware extractor. A mention of it as real attribute syntax here would
        // inflate that count on every single generated BasePage.cs (this comment is unconditional),
        // permanently tripping WorkspaceCodeGeneration's "needs your decision" conflict path with
        // nothing real behind it for the merge UI to ever show - i.e. BasePage.cs could never
        // regenerate again. Referring to the attribute by name without the brackets (as below) is
        // safe.
        sb.AppendLine();
        sb.AppendLine("    // A transient overlay (an ad iframe, a tooltip, a toast) can sit on top of an");
        sb.AppendLine("    // otherwise interactable element for a moment, which Selenium's real-mouse-click");
        sb.AppendLine("    // simulation refuses to click through even though the element itself is fine. A");
        sb.AppendLine("    // JS-dispatched click bypasses that check, so it's used only as a fallback. Virtual");
        sb.AppendLine("    // so a specific page can override it with a hand-written, CliqCustomMethod-tagged");
        sb.AppendLine("    // method if it ever needs different behavior - see WorkspaceCodeGeneration's");
        sb.AppendLine("    // BasePage.cs merge handling.");
        sb.AppendLine("    protected virtual void ClickWithFallback(IWebElement element)");
        sb.AppendLine("    {");
        sb.AppendLine("        try");
        sb.AppendLine("        {");
        sb.AppendLine("            element.Click();");
        sb.AppendLine("        }");
        sb.AppendLine("        catch (ElementClickInterceptedException)");
        sb.AppendLine("        {");
        sb.AppendLine($"            ((IJavaScriptExecutor){fieldNames.DriverField}).ExecuteScript(\"arguments[0].click();\", element);");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    protected virtual IWebElement Locate(By locator) =>");
        sb.AppendLine($"        {fieldNames.WaitField}.Until(d => d.FindElement(locator));");
        sb.AppendLine();
        sb.AppendLine("    // Same lookup as Locate, but returns null on timeout instead of throwing - for a");
        sb.AppendLine("    // caller that treats \"not there\" as a legitimate outcome rather than a failure.");
        sb.AppendLine("    protected virtual IWebElement? TryLocate(By locator)");
        sb.AppendLine("    {");
        sb.AppendLine("        try");
        sb.AppendLine("        {");
        sb.AppendLine($"            return {fieldNames.WaitField}.Until(d => d.FindElement(locator));");
        sb.AppendLine("        }");
        sb.AppendLine("        catch (WebDriverTimeoutException)");
        sb.AppendLine("        {");
        sb.AppendLine("            return null;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    protected virtual void Hover(IWebElement element) =>");
        sb.AppendLine($"        new Actions({fieldNames.DriverField}).MoveToElement(element).Perform();");
        sb.AppendLine();
        sb.AppendLine("    protected virtual void SetChecked(IWebElement element, bool isChecked)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (element.Selected != isChecked)");
        sb.AppendLine("        {");
        sb.AppendLine("            ClickWithFallback(element);");
        sb.AppendLine("        }");
        sb.AppendLine("    }");

        AppendCustomMethods(sb, customMethods ?? []);

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
        // The comment carries the locator's generated name (editable by hand - see
        // WorkspaceCodeGeneration's rename read-back) and, for an xpath element, its absolute
        // /html/body/... path. The runtime locator is still the relative value (ByExpression) -
        // an absolute path is too brittle to actually locate by, so it's comment-only.
        var commentValue = element.Strategy == LocatorStrategy.XPath && !string.IsNullOrEmpty(element.AbsoluteXpath)
            ? element.AbsoluteXpath!
            : element.LocatorValue;
        sb.AppendLine($"    // Locator ({StrategyLabel(element.Strategy)}): {name} = {SingleLine(commentValue)}");
        sb.AppendLine($"    private static readonly By {name}Locator = {ByExpression(element)};");
    }

    private static void AppendWrappedInit(StringBuilder sb, ElementDescriptor element, BasePageFieldNames? basePageFieldNames)
    {
        var name = IdentifierNaming.ToPascalCase(element.SuggestedName);
        var locatorRef = $"{name}Locator";
        var dataType = WrapperDataType(element.Kind, element.DataType);
        var body = WrapperBody(element.Kind, element.DataType, locatorRef, basePageFieldNames);

        AppendBasePageUsageComment(sb, basePageFieldNames, "        ", "Locate");
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
    private static (string[] Upload, string[] Read) WrapperBody(ElementActionKind kind, string dataType, string locatorExpr, BasePageFieldNames? basePageFieldNames)
    {
        var locate = $"var element = {LocateExpression(basePageFieldNames, locatorExpr)};";
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

    private static void AppendMethod(StringBuilder sb, ElementDescriptor element, BasePageFieldNames? basePageFieldNames)
    {
        var name = IdentifierNaming.ToPascalCase(element.SuggestedName);
        var locatorRef = $"{name}Locator";
        var driverField = basePageFieldNames?.DriverField ?? "_driver";

        switch (element.Kind)
        {
            case ElementActionKind.Click:
                AppendBasePageUsageComment(sb, basePageFieldNames, "    ", "Locate, ClickWithFallback");
                sb.AppendLine($"    public void Click{name}()");
                sb.AppendLine("    {");
                sb.AppendLine($"        var element = {LocateExpression(basePageFieldNames, locatorRef)};");
                sb.AppendLine("        ClickWithFallback(element);");
                sb.AppendLine("    }");
                sb.AppendLine();
                AppendBasePageUsageComment(sb, basePageFieldNames, "    ", "Locate, Hover");
                sb.AppendLine($"    public void Hover{name}()");
                sb.AppendLine("    {");
                sb.AppendLine($"        var element = {LocateExpression(basePageFieldNames, locatorRef)};");
                sb.AppendLine($"        {HoverStatement(basePageFieldNames, driverField)}");
                sb.AppendLine("    }");
                break;

            case ElementActionKind.Check:
                AppendBasePageUsageComment(sb, basePageFieldNames, "    ", "Locate, SetChecked");
                sb.AppendLine($"    public void Set{name}(bool isChecked)");
                sb.AppendLine("    {");
                sb.AppendLine($"        var element = {LocateExpression(basePageFieldNames, locatorRef)};");
                if (basePageFieldNames is not null)
                {
                    sb.AppendLine("        SetChecked(element, isChecked);");
                }
                else
                {
                    sb.AppendLine("        if (element.Selected != isChecked)");
                    sb.AppendLine("        {");
                    sb.AppendLine("            ClickWithFallback(element);");
                    sb.AppendLine("        }");
                }

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
                sb.AppendLine($"    // Locator ({StrategyLabel(element.Strategy)}): {IdentifierNaming.ToPascalCase(element.SuggestedName)} = {SingleLine(element.LocatorValue)}");
                AppendBasePageUsageComment(sb, basePageFieldNames, "    ", "Locate, ClickWithFallback");
                var radioPrefix = ToCSharpStringLiteral($"input[name='{CssEscapeSingleQuoted(element.LocatorValue)}'][value='");
                sb.AppendLine($"    public void Select{name}(string value)");
                sb.AppendLine("    {");
                sb.AppendLine(@"        var escapedValue = value.Replace(""\\"", ""\\\\"").Replace(""'"", ""\\'"");");
                var radioLocatorExpr = $"By.CssSelector({radioPrefix} + escapedValue + \"']\")";
                sb.AppendLine($"        var element = {LocateExpression(basePageFieldNames, radioLocatorExpr)};");
                sb.AppendLine("        ClickWithFallback(element);");
                sb.AppendLine("    }");
                break;

            case ElementActionKind.Assert:
                AppendBasePageUsageComment(sb, basePageFieldNames, "    ", "Locate");
                sb.AppendLine($"    public string Get{name}Text()");
                sb.AppendLine("    {");
                sb.AppendLine($"        var element = {LocateExpression(basePageFieldNames, locatorRef)};");
                sb.AppendLine("        return element.Text;");
                sb.AppendLine("    }");
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(element));
        }
    }

    /// <summary>Emits an <c>Is{Name}Visible()</c> check for an element with a fixed locator - a
    /// timeout means "not there" rather than a failure, so this never throws, unlike every other
    /// generated member. In base-page mode this is just <c>TryLocate(x) is not null</c> (see
    /// <see cref="PageObjectCodeGenerator.GenerateBasePageFile"/>'s own remarks on why it returns
    /// null on timeout instead of throwing); self-contained mode inlines the same try/catch
    /// locally since there's no shared base to hold it.</summary>
    private static void AppendVisibilityCheck(StringBuilder sb, ElementDescriptor element, BasePageFieldNames? basePageFieldNames)
    {
        var name = IdentifierNaming.ToPascalCase(element.SuggestedName);
        var locatorRef = $"{name}Locator";

        AppendBasePageUsageComment(sb, basePageFieldNames, "    ", "TryLocate");
        sb.AppendLine($"    public bool Is{name}Visible()");
        sb.AppendLine("    {");
        if (basePageFieldNames is not null)
        {
            sb.AppendLine($"        return TryLocate({locatorRef}) is not null;");
        }
        else
        {
            sb.AppendLine("        try");
            sb.AppendLine("        {");
            sb.AppendLine($"            _wait.Until(d => d.FindElement({locatorRef}));");
            sb.AppendLine("            return true;");
            sb.AppendLine("        }");
            sb.AppendLine("        catch (WebDriverTimeoutException)");
            sb.AppendLine("        {");
            sb.AppendLine("            return false;");
            sb.AppendLine("        }");
        }

        sb.AppendLine("    }");
    }

    /// <summary>One-line comment noting which inherited <c>BasePage</c> member(s) the
    /// method/property emitted right after it relies on - emitted only in base-page mode
    /// (<paramref name="basePageFieldNames"/> non-null), at every generated member, per request:
    /// self-contained pages stay uncommented, matching their existing self-documenting-by-naming
    /// style, since there's no inherited behavior there to call out. <paramref name="indent"/>
    /// accounts for callers at different nesting depths (a class member at four spaces, a
    /// constructor-body statement at eight).</summary>
    private static void AppendBasePageUsageComment(
        StringBuilder sb, BasePageFieldNames? basePageFieldNames, string indent, string members)
    {
        if (basePageFieldNames is not null)
        {
            sb.AppendLine($"{indent}// BasePage generation: uses the inherited BasePage.{members}.");
        }
    }

    /// <summary>Right-hand-side expression for locating an element - <c>Locate(x)</c> when
    /// generating against a shared <c>BasePage</c> (see GenerateBasePageFile), or the same inline
    /// wait-and-find Cliq has always used otherwise. Shared by every call site that needs "find
    /// this locator" text: <see cref="AppendMethod"/>, <see cref="AppendDynamicFactory"/>, and
    /// <see cref="WrapperBody"/>.</summary>
    private static string LocateExpression(BasePageFieldNames? basePageFieldNames, string locatorExpr) =>
        basePageFieldNames is not null
            ? $"Locate({locatorExpr})"
            : $"_wait.Until(d => d.FindElement({locatorExpr}))";

    /// <summary>Full statement for hovering an already-located <c>element</c> - calls the shared
    /// <c>BasePage.Hover</c> when generating against one, or builds the <c>Actions</c> call inline
    /// otherwise (today's self-contained shape).</summary>
    private static string HoverStatement(BasePageFieldNames? basePageFieldNames, string driverField) =>
        basePageFieldNames is not null
            ? "Hover(element);"
            : $"new Actions({driverField}).MoveToElement(element).Perform();";

    /// <summary>
    /// Emits a dynamic element's generated member: a parameterized factory instead of a fixed
    /// field/property, since its locator depends on the caller-supplied <c>param</c> and can't be
    /// precomputed. Fill/Select reuse the same <see cref="ElementWrapper{TData}"/> shape as static
    /// elements, just returned from a method instead of exposed as a property; Click/Assert reuse
    /// the same plain-method shape, with <c>param</c> added as a leading parameter.
    /// </summary>
    private static void AppendDynamicFactory(StringBuilder sb, DynamicElementDescriptor element, BasePageFieldNames? basePageFieldNames)
    {
        var name = IdentifierNaming.ToPascalCase(element.SuggestedName);
        var paramType = element.ParameterType == "string" ? "string" : "int";
        var locatorExpr = InterpolatedByExpression(element);
        var driverField = basePageFieldNames?.DriverField ?? "_driver";

        sb.AppendLine($"    // Locator ({StrategyLabel(element.Strategy)}, template): {IdentifierNaming.ToPascalCase(element.SuggestedName)} = {SingleLine(element.LocatorTemplate)}");

        if (IsWrapped(element.Kind))
        {
            var dataType = WrapperDataType(element.Kind, element.DataType);
            var body = WrapperBody(element.Kind, element.DataType, locatorExpr, basePageFieldNames);

            AppendBasePageUsageComment(sb, basePageFieldNames, "    ", "Locate");
            sb.AppendLine($"    public {ElementWrapperClassName}<{dataType}> {name}({paramType} param) =>");
            AppendWrapperConstruction(sb, "        ", string.Empty, dataType, body);
            sb.AppendLine();
            AppendDynamicVisibilityCheck(sb, name, paramType, locatorExpr, basePageFieldNames);
            return;
        }

        switch (element.Kind)
        {
            case ElementActionKind.Click:
                AppendBasePageUsageComment(sb, basePageFieldNames, "    ", "Locate, ClickWithFallback");
                sb.AppendLine($"    public void Click{name}({paramType} param)");
                sb.AppendLine("    {");
                sb.AppendLine($"        var element = {LocateExpression(basePageFieldNames, locatorExpr)};");
                sb.AppendLine("        ClickWithFallback(element);");
                sb.AppendLine("    }");
                sb.AppendLine();
                AppendBasePageUsageComment(sb, basePageFieldNames, "    ", "Locate, Hover");
                sb.AppendLine($"    public void Hover{name}({paramType} param)");
                sb.AppendLine("    {");
                sb.AppendLine($"        var element = {LocateExpression(basePageFieldNames, locatorExpr)};");
                sb.AppendLine($"        {HoverStatement(basePageFieldNames, driverField)}");
                sb.AppendLine("    }");
                break;

            case ElementActionKind.Assert:
                AppendBasePageUsageComment(sb, basePageFieldNames, "    ", "Locate");
                sb.AppendLine($"    public string Get{name}Text({paramType} param)");
                sb.AppendLine("    {");
                sb.AppendLine($"        var element = {LocateExpression(basePageFieldNames, locatorExpr)};");
                sb.AppendLine("        return element.Text;");
                sb.AppendLine("    }");
                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(element), $"Dynamic elements do not support {element.Kind}.");
        }

        sb.AppendLine();
        AppendDynamicVisibilityCheck(sb, name, paramType, locatorExpr, basePageFieldNames);
    }

    /// <summary>Dynamic-element counterpart to <see cref="AppendVisibilityCheck"/> - same
    /// never-throws shape, with <c>param</c> added since the locator isn't fixed.</summary>
    private static void AppendDynamicVisibilityCheck(
        StringBuilder sb, string name, string paramType, string locatorExpr, BasePageFieldNames? basePageFieldNames)
    {
        AppendBasePageUsageComment(sb, basePageFieldNames, "    ", "TryLocate");
        sb.AppendLine($"    public bool Is{name}Visible({paramType} param)");
        sb.AppendLine("    {");
        if (basePageFieldNames is not null)
        {
            sb.AppendLine($"        return TryLocate({locatorExpr}) is not null;");
        }
        else
        {
            sb.AppendLine("        try");
            sb.AppendLine("        {");
            sb.AppendLine($"            _wait.Until(d => d.FindElement({locatorExpr}));");
            sb.AppendLine("            return true;");
            sb.AppendLine("        }");
            sb.AppendLine("        catch (WebDriverTimeoutException)");
            sb.AppendLine("        {");
            sb.AppendLine("            return false;");
            sb.AppendLine("        }");
        }

        sb.AppendLine("    }");
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

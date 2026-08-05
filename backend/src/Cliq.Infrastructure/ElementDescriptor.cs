namespace Cliq.Infrastructure.Generation;

public enum ElementActionKind
{
    Fill,
    Click,
    Select,
    Check,
    Assert,
    Radio,
    MultiSelect,
}

public enum LocatorStrategy
{
    Id,
    Name,
    CssSelector,
    XPath,
}

public sealed record ElementDescriptor(
    string Tag,
    ElementActionKind Kind,
    LocatorStrategy Strategy,
    string LocatorValue,
    string SuggestedName,
    string? ExpectedText = null,
    string? GroupName = null,
    // Only meaningful for Fill - "string"/"int"/"DateTime". Select/MultiSelect decide their own
    // wrapper data type from Kind alone (see PageObjectCodeGenerator.WrapperDataType).
    string DataType = "string");

/// <summary>
/// A user-authored locator template for a repeating element - see
/// <c>Cliq.Domain.DynamicElement</c>, which this mirrors in the generation namespace the same
/// way <see cref="ElementDescriptor"/> mirrors <c>Cliq.Domain.PageElement</c>. Generated as a
/// parameterized factory method instead of a fixed field/property - only <see cref="ElementActionKind.Fill"/>,
/// <see cref="ElementActionKind.Click"/>, <see cref="ElementActionKind.Select"/>, and
/// <see cref="ElementActionKind.Assert"/> are supported.
/// </summary>
public sealed record DynamicElementDescriptor(
    ElementActionKind Kind,
    LocatorStrategy Strategy,
    string LocatorTemplate,
    string SuggestedName,
    // "int" or "string" - the type of the generated factory method's parameter.
    string ParameterType = "int",
    // Only meaningful when Kind is Fill - same meaning as ElementDescriptor.DataType.
    string DataType = "string");

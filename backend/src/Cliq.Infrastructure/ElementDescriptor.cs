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
    string DataType = "string")
{
    public string Selector => Strategy switch
    {
        LocatorStrategy.Id => $"#{LocatorValue}",
        LocatorStrategy.Name => $"[name=\"{LocatorValue}\"]",
        _ => LocatorValue,
    };
}

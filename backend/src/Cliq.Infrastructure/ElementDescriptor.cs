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
    string? GroupName = null)
{
    public string Selector => Strategy switch
    {
        LocatorStrategy.Id => $"#{LocatorValue}",
        LocatorStrategy.Name => $"[name=\"{LocatorValue}\"]",
        _ => LocatorValue,
    };
}

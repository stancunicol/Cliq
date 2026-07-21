namespace PageObjectGenerator.Core;

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

/// <summary>
/// Which Selenium locator ultimately finds the element. Id/Name map to
/// Selenium's own By.Id/By.Name; CssSelector is the fallback for anything
/// without a native strategy (data-testid, structural nth-child paths).
/// </summary>
public enum LocatorStrategy
{
    Id,
    Name,
    CssSelector,
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
    /// <summary>Human-readable CSS-equivalent, for display/metadata only - actual Selenium calls use Strategy+LocatorValue.</summary>
    public string Selector => Strategy switch
    {
        LocatorStrategy.Id => $"#{LocatorValue}",
        LocatorStrategy.Name => $"[name=\"{LocatorValue}\"]",
        _ => LocatorValue,
    };
}

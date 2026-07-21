using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Edge;

namespace PageObjectGenerator.Core;

public enum BrowserKind
{
    Chrome,
    Edge,
}

public static class BrowserFactory
{
    // Selenium Manager (bundled since Selenium 4.6) resolves the matching
    // driver binary for the installed browser automatically - no manual
    // driver downloads or PATH setup required.
    public static IWebDriver Create(BrowserKind kind, bool headless = false)
    {
        return kind switch
        {
            BrowserKind.Chrome => CreateChrome(headless),
            BrowserKind.Edge => CreateEdge(headless),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
        };
    }

    private static IWebDriver CreateChrome(bool headless)
    {
        var options = new ChromeOptions();
        if (headless)
        {
            options.AddArgument("--headless=new");
        }

        return new ChromeDriver(options);
    }

    private static IWebDriver CreateEdge(bool headless)
    {
        var options = new EdgeOptions();
        if (headless)
        {
            options.AddArgument("--headless=new");
        }

        return new EdgeDriver(options);
    }
}

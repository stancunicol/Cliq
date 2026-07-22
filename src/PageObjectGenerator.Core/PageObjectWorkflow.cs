namespace PageObjectGenerator.Core;

public sealed record GenerationResult(
    string ClassName,
    string Url,
    BrowserKind Browser,
    string Namespace,
    string PageObjectCode,
    string TestCode,
    IReadOnlyList<ElementDescriptor> Elements,
    string? ScreenshotPath)
{
    public int ElementCount => Elements.Count;
}

/// <summary>
/// The end-to-end "scan a URL, generate a Page Object + NUnit test" flow shared
/// by the CLI and the API, so the two hosts can't drift out of sync.
/// </summary>
public static class PageObjectWorkflow
{
    public static GenerationResult Generate(
        string url,
        BrowserKind browser,
        bool headless,
        string className,
        string @namespace,
        string? screenshotDir = null)
    {
        if (!IdentifierNaming.IsSafeIdentifier(className))
        {
            throw new ArgumentException($"'{className}' is not a valid C# class name.", nameof(className));
        }

        if (!IdentifierNaming.IsSafeNamespace(@namespace))
        {
            throw new ArgumentException($"'{@namespace}' is not a valid C# namespace.", nameof(@namespace));
        }

        screenshotDir ??= SolutionPaths.DefaultScreenshotDirectory;

        using var driver = BrowserFactory.Create(browser, headless);
        try
        {
            driver.Navigate().GoToUrl(url);
            PageReadiness.WaitForComplete(driver);

            // Baseline of the page as it looked when its element tree was scanned -
            // a stable reference point, independent of whatever a later /run does.
            var screenshotPath = ScreenshotCapture.TryCapture(driver, screenshotDir, $"{className}_generated");

            var elements = ElementScanner.Scan(driver);
            var pageObjectCode = PageObjectCodeGenerator.Generate(className, @namespace, elements);
            var testCode = NUnitTestCodeGenerator.Generate(className, @namespace, url, browser, elements);
            return new GenerationResult(className, url, browser, @namespace, pageObjectCode, testCode, elements, screenshotPath);
        }
        finally
        {
            driver.Quit();
        }
    }

    /// <summary>
    /// Includes the host, not just the last path segment - two different
    /// domains that both happen to have a "/login" page would otherwise derive
    /// the same class name and silently overwrite each other's generated files.
    /// </summary>
    public static string DeriveClassName(string url)
    {
        var uri = new Uri(url);
        var lastSegment = uri.Segments.LastOrDefault(s => s != "/")?.Trim('/');
        var basis = string.IsNullOrEmpty(lastSegment) ? uri.Host : $"{uri.Host}_{lastSegment}";
        var sanitized = IdentifierNaming.Sanitize(basis, "generated");
        return IdentifierNaming.ToPascalCase(sanitized) + "Page";
    }

    public static BrowserKind ParseBrowserKind(string? value) =>
        string.Equals(value, "edge", StringComparison.OrdinalIgnoreCase) ? BrowserKind.Edge : BrowserKind.Chrome;
}

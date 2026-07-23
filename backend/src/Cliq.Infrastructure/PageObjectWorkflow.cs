namespace Cliq.Infrastructure.Generation;

public sealed record DiscoveryResult(
    string ClassName,
    string Url,
    BrowserKind Browser,
    IReadOnlyList<ElementDescriptor> Elements,
    string? ScreenshotPath);

public static class PageObjectWorkflow
{
    public static DiscoveryResult Discover(
        string url,
        BrowserKind browser,
        bool headless,
        string className,
        string screenshotDirectory)
    {
        if (!IdentifierNaming.IsSafeIdentifier(className))
        {
            throw new ArgumentException($"'{className}' is not a valid C# class name.", nameof(className));
        }

        using var driver = BrowserFactory.Create(browser, headless);
        try
        {
            driver.Navigate().GoToUrl(url);
            PageReadiness.WaitForComplete(driver);
            var screenshotPath = ScreenshotCapture.TryCapture(
                driver,
                screenshotDirectory,
                $"{className}_discovered");
            return new DiscoveryResult(className, url, browser, ElementScanner.Scan(driver), screenshotPath);
        }
        finally
        {
            driver.Quit();
        }
    }

    public static string DeriveClassName(string url)
    {
        var uri = new Uri(url);
        var lastSegment = uri.Segments.LastOrDefault(segment => segment != "/")?.Trim('/');
        var basis = string.IsNullOrEmpty(lastSegment) ? uri.Host : $"{uri.Host}_{lastSegment}";
        var sanitized = IdentifierNaming.Sanitize(basis, "generated");
        return IdentifierNaming.ToPascalCase(sanitized) + "Page";
    }
}

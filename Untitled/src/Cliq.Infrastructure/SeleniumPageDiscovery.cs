using Cliq.Application;
using Cliq.Domain;
using Cliq.Infrastructure.Generation;
using DomainBrowser = Cliq.Domain.BrowserKind;
using DomainLocator = Cliq.Domain.LocatorStrategy;
using GenerationBrowser = Cliq.Infrastructure.Generation.BrowserKind;
using GenerationKind = Cliq.Infrastructure.Generation.ElementActionKind;
using GenerationLocator = Cliq.Infrastructure.Generation.LocatorStrategy;

namespace Cliq.Infrastructure;

public sealed class SeleniumPageDiscovery(WorkspaceRepository repository) : IPageDiscovery
{
    public async Task<PageDefinition> ScanAsync(
        string url,
        string name,
        DomainBrowser browser,
        bool headless,
        CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new WorkspaceValidationException("Only absolute HTTP and HTTPS URLs can be scanned.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        var screenshotDirectory = Path.Combine(
            repository.RootPath ?? throw new WorkspaceNotOpenException(),
            ".cliq",
            "runs",
            "discovery");
        var className = PageObjectWorkflow.DeriveClassName(url);
        var result = await Task.Run(
            () => PageObjectWorkflow.Discover(
                url,
                browser == DomainBrowser.Edge ? GenerationBrowser.Edge : GenerationBrowser.Chrome,
                headless,
                className,
                screenshotDirectory),
            cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        var elements = result.Elements.Select((element, index) => new PageElement(
            $"{Slug(element.SuggestedName)}-{index + 1}",
            element.SuggestedName,
            element.Tag,
            ToAction(element.Kind),
            ToLocator(element.Strategy, element.LocatorValue, element.SuggestedName),
            element.ExpectedText)).ToList();

        return new PageDefinition(
            PageDefinition.CurrentSchemaVersion,
            Slug(name),
            name,
            url,
            elements,
            DateTimeOffset.UtcNow);
    }

    private static StepAction ToAction(GenerationKind kind) => kind switch
    {
        GenerationKind.Fill => StepAction.Fill,
        GenerationKind.Click => StepAction.Click,
        GenerationKind.Select or GenerationKind.MultiSelect or GenerationKind.Radio => StepAction.Select,
        GenerationKind.Check => StepAction.Click,
        GenerationKind.Assert => StepAction.AssertText,
        _ => StepAction.Click,
    };

    private static Locator ToLocator(GenerationLocator strategy, string value, string name)
    {
        if (strategy == GenerationLocator.CssSelector
            && value.StartsWith("[data-testid=\"", StringComparison.Ordinal)
            && value.EndsWith("\"]", StringComparison.Ordinal))
        {
            return new Locator(DomainLocator.TestId, value[14..^2], name);
        }

        return new Locator(
            strategy switch
            {
                GenerationLocator.Id => DomainLocator.Id,
                GenerationLocator.Name => DomainLocator.Name,
                GenerationLocator.XPath => DomainLocator.XPath,
                _ => DomainLocator.Css,
            },
            value,
            name);
    }

    private static string Slug(string value)
    {
        var chars = value.ToLowerInvariant().Select(character => char.IsLetterOrDigit(character) ? character : '-');
        var slug = string.Join(string.Empty, chars).Trim('-');
        return string.IsNullOrEmpty(slug) ? "page" : slug;
    }
}

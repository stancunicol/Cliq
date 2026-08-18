using Cliq.Application;
using Cliq.Domain;
using GenerationIdentifierNaming = Cliq.Infrastructure.Generation.IdentifierNaming;

namespace Cliq.Infrastructure;

public sealed partial class WorkspaceRepository
{
    public Task<IReadOnlyList<PageDefinition>> ListPagesAsync(CancellationToken cancellationToken) =>
        ReadDirectoryAsync<PageDefinition>(PagesDirectory, PageDefinition.CurrentSchemaVersion, cancellationToken);

    public async Task<PageDefinition?> GetPageAsync(string id, CancellationToken cancellationToken) =>
        await ReadOptionalAsync<PageDefinition>(EntityPath(PagesDirectory, id), cancellationToken);

    public async Task<PageDefinition> SavePageAsync(PageDefinition page, CancellationToken cancellationToken)
    {
        EnsureOpen();
        ValidateVersion(page.SchemaVersion, PageDefinition.CurrentSchemaVersion, "page");
        ValidateDynamicElements(page);
        await ValidateUniqueClassNameAsync(page, cancellationToken);
        var updated = page with { UpdatedAt = DateTimeOffset.UtcNow };
        await WriteAtomicAsync(EntityPath(PagesDirectory, page.Id), updated, cancellationToken);
        return updated;
    }

    // Two pages whose names sanitize to the same class name (e.g. "Login" and "log in" both ->
    // LoginPage) would otherwise silently overwrite each other's generated file on the next
    // Generate - WorkspaceCodeGeneration addresses pages by class name only (see its
    // pageClassNames dictionary), with no way to tell them apart once collided.
    private async Task ValidateUniqueClassNameAsync(PageDefinition page, CancellationToken cancellationToken)
    {
        var className = GenerationIdentifierNaming.ToPageObjectClassName(page.Name);
        var others = await ListPagesAsync(cancellationToken);
        var collision = others.FirstOrDefault(other =>
            !string.Equals(other.Id, page.Id, StringComparison.Ordinal) &&
            string.Equals(
                GenerationIdentifierNaming.ToPageObjectClassName(other.Name), className, StringComparison.Ordinal));
        if (collision is not null)
        {
            throw new WorkspaceValidationException(
                $"Page name '{page.Name}' generates the same class name ('{className}') as page " +
                $"'{collision.Name}'. Choose a different name.");
        }
    }

    // Only these four actions have a generated factory method for a dynamic element - see
    // PageObjectCodeGenerator.AppendDynamicFactory. Anything else would silently fall back to a
    // Click-shaped method at generation time (WorkspaceCodeGeneration.ToDynamicDescriptor), which
    // is wrong for e.g. a Radio/MultiSelect action, so it's rejected here instead.
    private static readonly HashSet<StepAction> SupportedDynamicActions =
    [
        StepAction.Click,
        StepAction.Fill,
        StepAction.Select,
        StepAction.AssertText,
    ];

    // Mirrors PageObjectCodeGenerator.ByExpression/InterpolatedByExpression's own restriction -
    // testId, xpath, and css are the only strategies discovery ever produces for a regular element,
    // and per the 2026-08-03 identification decision, id/name are never a valid locator strategy
    // (id is a naming hint only; name exists solely for a Radio group, which has no dynamic
    // equivalent) - so a dynamic element can't use them either, even though it's hand-authored.
    private static readonly HashSet<LocatorStrategy> SupportedDynamicStrategies =
    [
        LocatorStrategy.TestId,
        LocatorStrategy.XPath,
        LocatorStrategy.Css,
    ];

    private static void ValidateDynamicElements(PageDefinition page)
    {
        var dynamicElements = page.DynamicElements ?? [];
        if (dynamicElements.Count == 0)
        {
            return;
        }

        // Seeded with every static element's generated member name so a dynamic element can't
        // silently collide with one - both become a real C# member on the same Page Object class
        // (PageObjectCodeGenerator.Generate emits static-element members and dynamic-element
        // factories into the same class), and a collision would only surface as a compiler error
        // at generation time otherwise.
        var usedMemberNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var element in page.Elements)
        {
            usedMemberNames.Add(GenerationIdentifierNaming.ToPascalCase(element.Name));
        }

        foreach (var element in dynamicElements)
        {
            if (!SupportedDynamicActions.Contains(element.Action))
            {
                throw new WorkspaceValidationException(
                    $"Dynamic element '{element.Name}' on page '{page.Id}' has unsupported action " +
                    $"'{element.Action}'. Only click, fill, select, and assertText are supported.");
            }

            if (!SupportedDynamicStrategies.Contains(element.Strategy))
            {
                throw new WorkspaceValidationException(
                    $"Dynamic element '{element.Name}' on page '{page.Id}' has unsupported locator " +
                    $"strategy '{element.Strategy}'. Only testId, xpath, and css are supported.");
            }

            if (!element.LocatorTemplate.Contains("{param}", StringComparison.Ordinal))
            {
                throw new WorkspaceValidationException(
                    $"Dynamic element '{element.Name}' on page '{page.Id}' has a locator template " +
                    "with no '{param}' placeholder.");
            }

            var memberName = GenerationIdentifierNaming.ToPascalCase(WorkspaceCodeGeneration.DynamicMemberName(element));
            if (!usedMemberNames.Add(memberName))
            {
                throw new WorkspaceValidationException(
                    $"Dynamic element '{element.Name}' on page '{page.Id}' generates the member name " +
                    $"'{memberName}', which collides with another element on the same page.");
            }
        }
    }

    public Task DeletePageAsync(string id, CancellationToken cancellationToken) =>
        DeleteAsync(EntityPath(PagesDirectory, id), cancellationToken);
}

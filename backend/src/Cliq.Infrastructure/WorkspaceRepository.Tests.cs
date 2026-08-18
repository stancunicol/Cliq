using Cliq.Application;
using Cliq.Domain;

namespace Cliq.Infrastructure;

public sealed partial class WorkspaceRepository
{
    public Task<IReadOnlyList<TestCase>> ListTestsAsync(CancellationToken cancellationToken) =>
        ReadDirectoryAsync<TestCase>(TestsDirectory, TestCase.CurrentSchemaVersion, cancellationToken);

    public async Task<TestCase?> GetTestAsync(string id, CancellationToken cancellationToken) =>
        await ReadOptionalAsync<TestCase>(EntityPath(TestsDirectory, id), cancellationToken);

    public async Task<TestCase> SaveTestAsync(TestCase test, CancellationToken cancellationToken)
    {
        EnsureOpen();
        ValidateVersion(test.SchemaVersion, TestCase.CurrentSchemaVersion, "test");
        await ValidateStepReferencesAsync(test, cancellationToken);
        var updated = test with { UpdatedAt = DateTimeOffset.UtcNow };
        await WriteAtomicAsync(EntityPath(TestsDirectory, test.Id), updated, cancellationToken);
        return updated;
    }

    private static readonly HashSet<StepAction> ActionsRequiringElement =
    [
        StepAction.Click,
        StepAction.Fill,
        StepAction.Select,
        StepAction.PressKey,
        StepAction.AssertVisible,
        StepAction.AssertText,
    ];

    private async Task ValidateStepReferencesAsync(TestCase test, CancellationToken cancellationToken)
    {
        var pageCache = new Dictionary<string, PageDefinition?>(StringComparer.Ordinal);

        foreach (var step in test.Steps)
        {
            if (!ActionsRequiringElement.Contains(step.Action))
            {
                continue;
            }

            if (string.IsNullOrEmpty(step.PageId))
            {
                throw new WorkspaceValidationException($"Step '{step.Id}' has no page reference.");
            }

            if (!pageCache.TryGetValue(step.PageId, out var page))
            {
                page = await ReadOptionalAsync<PageDefinition>(EntityPath(PagesDirectory, step.PageId), cancellationToken);
                pageCache[step.PageId] = page;
            }

            if (page is null)
            {
                throw new WorkspaceValidationException($"Step '{step.Id}' references page '{step.PageId}', which does not exist.");
            }

            // A dynamic-element step has no ElementId (it targets a DynamicElement instead - see
            // TestStep.DynamicElementId) and is validated against the page's dynamic elements, not
            // its scanned ones.
            if (!string.IsNullOrEmpty(step.DynamicElementId))
            {
                if ((page.DynamicElements ?? []).All(element => element.Id != step.DynamicElementId))
                {
                    throw new WorkspaceValidationException(
                        $"Step '{step.Id}' references dynamic element '{step.DynamicElementId}' on page '{step.PageId}', which does not exist.");
                }

                continue;
            }

            if (string.IsNullOrEmpty(step.ElementId) || page.Elements.All(element => element.Id != step.ElementId))
            {
                throw new WorkspaceValidationException(
                    $"Step '{step.Id}' references element '{step.ElementId}' on page '{step.PageId}', which does not exist.");
            }
        }
    }

    public Task DeleteTestAsync(string id, CancellationToken cancellationToken) =>
        DeleteAsync(EntityPath(TestsDirectory, id), cancellationToken);
}

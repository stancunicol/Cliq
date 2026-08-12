using Cliq.Domain;

namespace Cliq.Api;

/// <summary>
/// Resolves which pages and tests a <see cref="GenerationRequest"/> actually targets, given the
/// full workspace snapshot. Split out of the <c>/api/v1/generation</c> minimal API handler in
/// Program.cs purely so this filtering logic - which decides what a partial request touches - can
/// be unit tested directly without a running host.
/// </summary>
internal static class GenerationRequestPlanner
{
    public static (IReadOnlyList<PageDefinition> Pages, IReadOnlyList<TestCase> Tests) Plan(
        GenerationRequest request,
        WorkspaceSnapshot snapshot)
    {
        var testIds = request.TestIds?.ToHashSet(StringComparer.Ordinal);
        var pageIds = request.PageIds?.ToHashSet(StringComparer.Ordinal);
        IReadOnlyList<TestCase> tests;
        if (testIds is not null)
        {
            tests = snapshot.Tests.Where(test => testIds.Contains(test.Id)).ToList();
        }
        else if (pageIds is not null)
        {
            // Only page ids were requested (regenerating a single page object, or resolving a
            // custom-method merge) - leave tests untouched. Regenerating every test here would
            // silently fail every step that references a page outside this request, since
            // pageClassNames below is only populated from the filtered `pages` list.
            tests = Array.Empty<TestCase>();
        }
        else
        {
            tests = snapshot.Tests;
        }

        if (pageIds is null && testIds is not null)
        {
            pageIds = tests
                .SelectMany(test => test.Steps)
                .Select(step => step.PageId)
                .Where(pageId => !string.IsNullOrWhiteSpace(pageId))
                .Cast<string>()
                .ToHashSet(StringComparer.Ordinal);
        }

        var pages = pageIds is null
            ? snapshot.Pages
            : snapshot.Pages.Where(page => pageIds.Contains(page.Id)).ToList();

        return (pages, tests);
    }
}

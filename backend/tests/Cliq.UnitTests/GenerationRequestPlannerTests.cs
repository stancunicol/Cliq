using Cliq.Api;
using Cliq.Domain;

namespace Cliq.UnitTests;

[TestFixture]
public sealed class GenerationRequestPlannerTests
{
    private static PageDefinition Page(string id) =>
        new(PageDefinition.CurrentSchemaVersion, id, id, $"https://example.com/{id}", [], DateTimeOffset.UtcNow);

    private static TestCase Test(string id, params string[] pageIds) =>
        new(
            TestCase.CurrentSchemaVersion,
            id,
            id,
            null,
            [],
            pageIds.Select((pageId, index) => new TestStep($"{id}-step-{index}", StepAction.AssertVisible, "Step", true, pageId)).ToArray(),
            DateTimeOffset.UtcNow);

    private static WorkspaceSnapshot Snapshot(IReadOnlyList<PageDefinition> pages, IReadOnlyList<TestCase> tests) =>
        new(
            "/workspace",
            new ProjectManifest(
                ProjectManifest.CurrentSchemaVersion, "proj", "Project", "https://example.com",
                BrowserKind.Chrome, new OutputSettings(), DateTimeOffset.UtcNow),
            pages,
            tests);

    [Test]
    public void Plan_PageIdsOnlyRequested_ReturnsNoTests()
    {
        // Regression: regenerating a single page (e.g. the Workbench "Regenerate" button, or the
        // merge UI resolving/including a custom method) must not touch any test, since none of the
        // other pages those tests might reference would be included - see
        // WorkspaceCodeGeneration.GenerateAsync's pageClassNames dictionary, which is only
        // populated from the filtered `pages` list. Regenerating every test here (the old
        // behavior) silently corrupted every step referencing a page outside this request into an
        // Assert.Fail.
        var pageA = Page("page-a");
        var pageB = Page("page-b");
        var testOnPageB = Test("test-1", "page-b");
        var snapshot = Snapshot([pageA, pageB], [testOnPageB]);
        var request = new GenerationRequest(PageIds: ["page-a"]);

        var (pages, tests) = GenerationRequestPlanner.Plan(request, snapshot);

        Assert.Multiple(() =>
        {
            Assert.That(pages, Has.Count.EqualTo(1));
            Assert.That(pages[0].Id, Is.EqualTo("page-a"));
            Assert.That(tests, Is.Empty);
        });
    }

    [Test]
    public void Plan_TestIdsOnlyRequested_ExpandsPagesToEveryPageThoseTestsReference()
    {
        var pageA = Page("page-a");
        var pageB = Page("page-b");
        var pageC = Page("page-c");
        var test = Test("test-1", "page-a", "page-b");
        var unrelatedTest = Test("test-2", "page-c");
        var snapshot = Snapshot([pageA, pageB, pageC], [test, unrelatedTest]);
        var request = new GenerationRequest(TestIds: ["test-1"]);

        var (pages, tests) = GenerationRequestPlanner.Plan(request, snapshot);
        var pageIds = pages.Select(page => page.Id).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(tests, Has.Count.EqualTo(1));
            Assert.That(tests[0].Id, Is.EqualTo("test-1"));
            Assert.That(pageIds, Has.Count.EqualTo(2));
            Assert.That(pageIds, Does.Contain("page-a"));
            Assert.That(pageIds, Does.Contain("page-b"));
        });
    }

    [Test]
    public void Plan_NeitherIdsRequested_ReturnsEveryPageAndTest()
    {
        var pageA = Page("page-a");
        var test = Test("test-1", "page-a");
        var snapshot = Snapshot([pageA], [test]);

        var (pages, tests) = GenerationRequestPlanner.Plan(new GenerationRequest(), snapshot);

        Assert.Multiple(() =>
        {
            Assert.That(pages, Is.EqualTo(snapshot.Pages));
            Assert.That(tests, Is.EqualTo(snapshot.Tests));
        });
    }
}

using Cliq.Application;
using Cliq.Domain;
using Cliq.Infrastructure;

namespace Cliq.UnitTests;

[TestFixture]
public sealed class ScanReviewServiceTests
{
    private string _root = null!;

    [SetUp]
    public void SetUp()
    {
        _root = Path.Combine(Path.GetTempPath(), "cliq-scan-review-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    [Test]
    public async Task ScanAsync_FirstAndCleanScansSaveImmediatelyWithoutAReview()
    {
        using var repository = new WorkspaceRepository();
        await repository.OpenAsync(_root, CancellationToken.None);
        var firstPage = Page("page", [Element("keep", "keep", "one")]);
        var firstResult = new PageScanResult(
            firstPage,
            new ScanReconciliationReport(0, 1, 0, IsFirstScan: true));
        var cleanPage = firstPage with
        {
            Name = "Updated page",
            Elements = [Element("keep", "updated_name", "one")],
            UpdatedAt = DateTimeOffset.UtcNow,
        };
        var cleanResult = new PageScanResult(
            cleanPage,
            new ScanReconciliationReport(1, 0, 0));
        using var service = new ScanReviewService(
            repository,
            new SequenceDiscovery(firstResult, cleanResult));

        var first = await service.ScanAsync(
            firstPage.Url,
            firstPage.Name,
            BrowserKind.Chrome,
            true,
            CancellationToken.None);
        var clean = await service.ScanAsync(
            cleanPage.Url,
            cleanPage.Name,
            BrowserKind.Chrome,
            true,
            CancellationToken.None);
        var reviews = await service.ListAsync(null, null, CancellationToken.None);
        var saved = await repository.GetPageAsync("page", CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(first.Review, Is.Null);
            Assert.That(clean.Review, Is.Null);
            Assert.That(reviews, Is.Empty);
            Assert.That(saved?.Name, Is.EqualTo("Updated page"));
            Assert.That(saved?.Elements[0].Name, Is.EqualTo("updated_name"));
        });
        await repository.CloseAsync(CancellationToken.None);
    }

    [Test]
    public async Task ScanAsync_ChangedPageCreatesPersistentReviewWithoutChangingActivePage()
    {
        using var repository = new WorkspaceRepository();
        await repository.OpenAsync(_root, CancellationToken.None);
        var baseline = await repository.SavePageAsync(
            Page("page", [Element("keep", "keep", "one"), Element("missing", "missing", "two")]),
            CancellationToken.None);
        var changed = ChangedScan(baseline);
        using var service = new ScanReviewService(repository, new SequenceDiscovery(changed));

        var result = await service.ScanAsync(
            baseline.Url,
            baseline.Name,
            BrowserKind.Chrome,
            true,
            CancellationToken.None);
        var active = await repository.GetPageAsync(baseline.Id, CancellationToken.None);

        using var reopenedService = new ScanReviewService(repository, new SequenceDiscovery());
        var persisted = await reopenedService.GetAsync(result.Review!.Id, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.ActivePage.UpdatedAt, Is.EqualTo(baseline.UpdatedAt));
            Assert.That(result.ActivePage.Elements, Is.EqualTo(baseline.Elements));
            Assert.That(active?.UpdatedAt, Is.EqualTo(baseline.UpdatedAt));
            Assert.That(active?.Elements, Is.EqualTo(baseline.Elements));
            Assert.That(persisted.Status, Is.EqualTo(ScanReviewStatus.Pending));
            Assert.That(persisted.Items, Has.Count.EqualTo(2));
            Assert.That(persisted.Items.All(item => item.Decision == ScanReviewDecision.Wait), Is.True);
            Assert.That(
                File.Exists(Path.Combine(
                    _root,
                    ".cliq",
                    "runs",
                    "scan-reviews",
                    $"{persisted.Id}.json")),
                Is.True);
        });
        await repository.CloseAsync(CancellationToken.None);
    }

    [Test]
    public async Task ApplyAsync_AcceptAndDenyMergeTheProposedPageAtomically()
    {
        using var repository = new WorkspaceRepository();
        await repository.OpenAsync(_root, CancellationToken.None);
        var baseline = await repository.SavePageAsync(
            Page(
                "page",
                [
                    Element("keep", "old_name", "one"),
                    Element("remove", "remove", "two"),
                    Element("retain", "retain", "three"),
                ]),
            CancellationToken.None);
        var proposed = baseline with
        {
            Name = "Proposed name",
            Elements =
            [
                Element("keep", "new_name", "one"),
                Element("new-accept", "new_accept", "four"),
                Element("new-deny", "new_deny", "five"),
                Element("remove", "remove", "two", notFound: true),
                Element("retain", "retain", "three", notFound: true),
            ],
            UpdatedAt = DateTimeOffset.UtcNow,
        };
        var scan = ScanResult(
            proposed,
            [proposed.Elements[1], proposed.Elements[2]],
            [baseline.Elements[1], baseline.Elements[2]]);
        using var service = new ScanReviewService(repository, new SequenceDiscovery(scan));
        var staged = await service.ScanAsync(
            baseline.Url,
            baseline.Name,
            BrowserKind.Chrome,
            true,
            CancellationToken.None);
        var review = staged.Review!;
        var decisions = review.Items.Select(item =>
        {
            var decision = item.Element.Id switch
            {
                "new-accept" or "remove" => ScanReviewDecision.Accept,
                _ => ScanReviewDecision.Deny,
            };
            return new ScanReviewDecisionUpdate(item.Id, decision);
        }).ToList();

        var decided = await service.UpdateDecisionsAsync(review.Id, decisions, CancellationToken.None);
        var applied = await service.ApplyAsync(review.Id, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(decided.Items, Has.None.Matches<ScanReviewItem>(item =>
                item.Decision == ScanReviewDecision.Wait));
            Assert.That(applied.Review.Status, Is.EqualTo(ScanReviewStatus.Applied));
            Assert.That(applied.Review.CompletedAt, Is.Not.Null);
            Assert.That(applied.Page.Name, Is.EqualTo("Proposed name"));
            Assert.That(
                applied.Page.Elements.Select(element => element.Id),
                Is.EqualTo(new List<string> { "keep", "new-accept", "retain" }));
            Assert.That(applied.Page.Elements[0].Name, Is.EqualTo("new_name"));
            Assert.That(applied.Page.Elements[2], Is.EqualTo(baseline.Elements[2]));
            Assert.That(applied.Page.Elements.All(element => !element.NotFound), Is.True);
        });
        await repository.CloseAsync(CancellationToken.None);
    }

    [Test]
    public async Task ApplyAsync_WaitItemsAndStalePagesReturnConflicts()
    {
        using var repository = new WorkspaceRepository();
        await repository.OpenAsync(_root, CancellationToken.None);
        var baseline = await repository.SavePageAsync(
            Page("page", [Element("keep", "keep", "one"), Element("missing", "missing", "two")]),
            CancellationToken.None);
        using var service = new ScanReviewService(repository, new SequenceDiscovery(ChangedScan(baseline)));
        var review = (await service.ScanAsync(
            baseline.Url,
            baseline.Name,
            BrowserKind.Chrome,
            true,
            CancellationToken.None)).Review!;

        var unresolved = Assert.ThrowsAsync<ScanReviewConflictException>(async () =>
            await service.ApplyAsync(review.Id, CancellationToken.None));
        await service.UpdateDecisionsAsync(
            review.Id,
            review.Items.Select(item =>
                new ScanReviewDecisionUpdate(item.Id, ScanReviewDecision.Deny)).ToList(),
            CancellationToken.None);
        await Task.Delay(2);
        await repository.SavePageAsync(baseline with { Name = "Manually changed" }, CancellationToken.None);
        var stale = Assert.ThrowsAsync<ScanReviewConflictException>(async () =>
            await service.ApplyAsync(review.Id, CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That(unresolved!.Code, Is.EqualTo("scan_review_unresolved"));
            Assert.That(stale!.Code, Is.EqualTo("scan_review_stale"));
        });
        await repository.CloseAsync(CancellationToken.None);
    }

    [Test]
    public async Task ApplyAsync_AcceptedRemovalReferencedByTestReportsEveryReference()
    {
        using var repository = new WorkspaceRepository();
        await repository.OpenAsync(_root, CancellationToken.None);
        var baseline = await repository.SavePageAsync(
            Page("page", [Element("keep", "keep", "one"), Element("missing", "missing", "two")]),
            CancellationToken.None);
        await repository.SaveTestAsync(
            new TestCase(
                TestCase.CurrentSchemaVersion,
                "dependent-test",
                "Dependent test",
                null,
                [],
                [
                    new TestStep("use-missing", StepAction.Click, "Use missing", true, "page", "missing"),
                ],
                DateTimeOffset.UtcNow),
            CancellationToken.None);
        using var service = new ScanReviewService(repository, new SequenceDiscovery(ChangedScan(baseline)));
        var review = (await service.ScanAsync(
            baseline.Url,
            baseline.Name,
            BrowserKind.Chrome,
            true,
            CancellationToken.None)).Review!;
        await service.UpdateDecisionsAsync(
            review.Id,
            review.Items.Select(item => new ScanReviewDecisionUpdate(
                item.Id,
                item.Kind == ScanReviewChangeKind.Missing
                    ? ScanReviewDecision.Accept
                    : ScanReviewDecision.Deny)).ToList(),
            CancellationToken.None);

        var conflict = Assert.ThrowsAsync<ScanReviewConflictException>(async () =>
            await service.ApplyAsync(review.Id, CancellationToken.None));
        var stillPending = await service.GetAsync(review.Id, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(conflict!.Code, Is.EqualTo("scan_review_removal_referenced"));
            Assert.That(
                conflict.References,
                Is.EqualTo(new[] { new ScanReviewReference("dependent-test", "use-missing") }));
            Assert.That(stillPending.Status, Is.EqualTo(ScanReviewStatus.Pending));
        });
        await repository.CloseAsync(CancellationToken.None);
    }

    [Test]
    public async Task ScanAsync_SuccessSupersedesPendingReviewButFailureLeavesItPending()
    {
        using var repository = new WorkspaceRepository();
        await repository.OpenAsync(_root, CancellationToken.None);
        var baseline = await repository.SavePageAsync(
            Page("page", [Element("keep", "keep", "one"), Element("missing", "missing", "two")]),
            CancellationToken.None);
        var discovery = new SequenceDiscovery(
            ChangedScan(baseline, "new-one"),
            new WorkspaceValidationException("scan failed"),
            ChangedScan(baseline, "new-two"));
        using var service = new ScanReviewService(repository, discovery);
        var first = (await service.ScanAsync(
            baseline.Url,
            baseline.Name,
            BrowserKind.Chrome,
            true,
            CancellationToken.None)).Review!;

        Assert.ThrowsAsync<WorkspaceValidationException>(async () =>
            await service.ScanAsync(
                baseline.Url,
                baseline.Name,
                BrowserKind.Chrome,
                true,
                CancellationToken.None));
        var afterFailure = await service.GetAsync(first.Id, CancellationToken.None);
        var second = (await service.ScanAsync(
            baseline.Url,
            baseline.Name,
            BrowserKind.Chrome,
            true,
            CancellationToken.None)).Review!;
        var superseded = await service.GetAsync(first.Id, CancellationToken.None);
        var pending = await service.ListAsync("page", ScanReviewStatus.Pending, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(afterFailure.Status, Is.EqualTo(ScanReviewStatus.Pending));
            Assert.That(superseded.Status, Is.EqualTo(ScanReviewStatus.Superseded));
            Assert.That(superseded.CompletedAt, Is.Not.Null);
            Assert.That(pending.Select(review => review.Id), Is.EqualTo(new[] { second.Id }));
        });
        await repository.CloseAsync(CancellationToken.None);
    }

    [Test]
    public async Task DiscardAsync_PreservesPageAndMakesReviewImmutable()
    {
        using var repository = new WorkspaceRepository();
        await repository.OpenAsync(_root, CancellationToken.None);
        var baseline = await repository.SavePageAsync(
            Page("page", [Element("keep", "keep", "one"), Element("missing", "missing", "two")]),
            CancellationToken.None);
        using var service = new ScanReviewService(repository, new SequenceDiscovery(ChangedScan(baseline)));
        var review = (await service.ScanAsync(
            baseline.Url,
            baseline.Name,
            BrowserKind.Chrome,
            true,
            CancellationToken.None)).Review!;

        var discarded = await service.DiscardAsync(review.Id, CancellationToken.None);
        var active = await repository.GetPageAsync("page", CancellationToken.None);
        var mutation = Assert.ThrowsAsync<ScanReviewConflictException>(async () =>
            await service.UpdateDecisionsAsync(
                review.Id,
                [new ScanReviewDecisionUpdate(review.Items[0].Id, ScanReviewDecision.Accept)],
                CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That(discarded.Status, Is.EqualTo(ScanReviewStatus.Discarded));
            Assert.That(active?.UpdatedAt, Is.EqualTo(baseline.UpdatedAt));
            Assert.That(active?.Elements, Is.EqualTo(baseline.Elements));
            Assert.That(mutation!.Code, Is.EqualTo("scan_review_not_pending"));
        });
        await repository.CloseAsync(CancellationToken.None);
    }

    [Test]
    public async Task UpdateDecisionsAsync_RejectsEmptyDuplicateAndUnknownUpdates()
    {
        using var repository = new WorkspaceRepository();
        await repository.OpenAsync(_root, CancellationToken.None);
        var baseline = await repository.SavePageAsync(
            Page("page", [Element("keep", "keep", "one"), Element("missing", "missing", "two")]),
            CancellationToken.None);
        using var service = new ScanReviewService(repository, new SequenceDiscovery(ChangedScan(baseline)));
        var review = (await service.ScanAsync(
            baseline.Url,
            baseline.Name,
            BrowserKind.Chrome,
            true,
            CancellationToken.None)).Review!;
        var itemId = review.Items[0].Id;

        Assert.Multiple(() =>
        {
            Assert.ThrowsAsync<WorkspaceValidationException>(async () =>
                await service.UpdateDecisionsAsync(review.Id, [], CancellationToken.None));
            Assert.ThrowsAsync<WorkspaceValidationException>(async () =>
                await service.UpdateDecisionsAsync(
                    review.Id,
                    [
                        new ScanReviewDecisionUpdate(itemId, ScanReviewDecision.Accept),
                        new ScanReviewDecisionUpdate(itemId, ScanReviewDecision.Deny),
                    ],
                    CancellationToken.None));
            Assert.ThrowsAsync<WorkspaceValidationException>(async () =>
                await service.UpdateDecisionsAsync(
                    review.Id,
                    [new ScanReviewDecisionUpdate(Guid.NewGuid().ToString("N"), ScanReviewDecision.Accept)],
                    CancellationToken.None));
            Assert.ThrowsAsync<WorkspaceValidationException>(async () =>
                await service.UpdateDecisionsAsync(
                    review.Id,
                    [new ScanReviewDecisionUpdate(itemId, (ScanReviewDecision)99)],
                    CancellationToken.None));
            Assert.ThrowsAsync<WorkspaceValidationException>(async () =>
                await service.GetAsync("../outside", CancellationToken.None));
        });
        await repository.CloseAsync(CancellationToken.None);
    }

    [Test]
    public async Task ListAsync_RejectsCorruptAndUnsupportedReviewFiles()
    {
        using var repository = new WorkspaceRepository();
        await repository.OpenAsync(_root, CancellationToken.None);
        var reviewDirectory = Path.Combine(_root, ".cliq", "runs", "scan-reviews");
        Directory.CreateDirectory(reviewDirectory);
        var path = Path.Combine(reviewDirectory, $"{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(path, "{invalid");
        using var service = new ScanReviewService(repository, new SequenceDiscovery());

        Assert.ThrowsAsync<WorkspaceValidationException>(async () =>
            await service.ListAsync(null, null, CancellationToken.None));
        await File.WriteAllTextAsync(path, "{\"schemaVersion\":99}");
        Assert.ThrowsAsync<WorkspaceValidationException>(async () =>
            await service.ListAsync(null, null, CancellationToken.None));

        await repository.CloseAsync(CancellationToken.None);
    }

    private static PageScanResult ChangedScan(PageDefinition baseline, string newId = "new")
    {
        var matched = baseline.Elements[0] with { Name = "matched_from_scan", NotFound = false };
        var added = Element(newId, newId.Replace('-', '_'), $"locator-{newId}");
        var missing = baseline.Elements[1] with { NotFound = true };
        var proposed = baseline with
        {
            Elements = [matched, added, missing],
            UpdatedAt = DateTimeOffset.UtcNow,
        };
        return ScanResult(proposed, [added], [baseline.Elements[1]]);
    }

    private static PageScanResult ScanResult(
        PageDefinition proposed,
        IReadOnlyList<PageElement> added,
        IReadOnlyList<PageElement> missing) =>
        new(
            proposed,
            new ScanReconciliationReport(
                proposed.Elements.Count - added.Count - missing.Count,
                added.Count,
                missing.Count),
            new LocatorDiffReport(
                proposed.Id,
                DateTimeOffset.UtcNow,
                added.Select(DiffEntry).ToList(),
                missing.Select(DiffEntry).ToList()));

    private static LocatorDiffEntry DiffEntry(PageElement element) =>
        new(element.Id, element.Name, element.Tag, element.Locator);

    private static PageDefinition Page(string id, IReadOnlyList<PageElement> elements) =>
        new(
            PageDefinition.CurrentSchemaVersion,
            id,
            "Page",
            "https://example.test/page",
            elements,
            DateTimeOffset.UtcNow);

    private static PageElement Element(
        string id,
        string name,
        string locator,
        bool notFound = false) =>
        new(
            id,
            name,
            "button",
            StepAction.Click,
            new Locator(LocatorStrategy.TestId, locator),
            NotFound: notFound);

    private sealed class SequenceDiscovery(params object[] outcomes) : IPageDiscovery
    {
        private readonly Queue<object> _outcomes = new(outcomes);

        public Task<PageScanResult> ScanAsync(
            string url,
            string name,
            BrowserKind browser,
            bool headless,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_outcomes.Count == 0)
            {
                throw new InvalidOperationException("No discovery result was configured.");
            }

            return _outcomes.Dequeue() switch
            {
                PageScanResult result => Task.FromResult(result),
                Exception exception => Task.FromException<PageScanResult>(exception),
                _ => throw new InvalidOperationException("Unsupported discovery outcome."),
            };
        }
    }
}

using System.Text.Json;
using System.Text.RegularExpressions;
using Cliq.Application;
using Cliq.Domain;

namespace Cliq.Infrastructure;

public sealed partial class ScanReviewService(
    IWorkspaceRepository repository,
    IPageDiscovery discovery) : IScanReviewService, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = CliqJsonOptions.Indented;

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly AtomicFileWriter _atomicWriter = new();

    public async Task<StagedPageScanResult> ScanAsync(
        string url,
        string name,
        BrowserKind browser,
        bool headless,
        CancellationToken cancellationToken)
    {
        return await _gate.WithLockAsync(async () =>
        {
            var snapshot = await repository.GetSnapshotAsync(cancellationToken);
            var result = await discovery.ScanAsync(url, name, browser, headless, cancellationToken);
            var baseline = snapshot.Pages.SingleOrDefault(page => page.Id == result.Page.Id);

            if (baseline is null || result.LocatorDiff is null)
            {
                await SupersedePendingAsync(result.Page.Id, cancellationToken);
                var saved = await repository.SavePageAsync(result.Page, cancellationToken);
                return new StagedPageScanResult(
                    saved,
                    result.Reconciliation,
                    DynamicElementSuggestions: result.DynamicElementSuggestions,
                    ScreenshotPath: result.ScreenshotPath);
            }

            var proposedById = result.Page.Elements.ToDictionary(element => element.Id, StringComparer.Ordinal);
            var baselineById = baseline.Elements.ToDictionary(element => element.Id, StringComparer.Ordinal);
            var items = new List<ScanReviewItem>();
            foreach (var entry in result.LocatorDiff.New)
            {
                if (!proposedById.TryGetValue(entry.ElementId, out var element))
                {
                    throw new WorkspaceValidationException(
                        $"Scan diff references new element '{entry.ElementId}', which is absent from the proposed page.");
                }

                items.Add(new ScanReviewItem(
                    Guid.NewGuid().ToString("N"),
                    ScanReviewChangeKind.New,
                    element));
            }

            foreach (var entry in result.LocatorDiff.Missing)
            {
                if (!baselineById.TryGetValue(entry.ElementId, out var element))
                {
                    throw new WorkspaceValidationException(
                        $"Scan diff references missing element '{entry.ElementId}', which is absent from the baseline page.");
                }

                items.Add(new ScanReviewItem(
                    Guid.NewGuid().ToString("N"),
                    ScanReviewChangeKind.Missing,
                    element));
            }

            if (items.Count == 0)
            {
                await SupersedePendingAsync(result.Page.Id, cancellationToken);
                var saved = await repository.SavePageAsync(result.Page, cancellationToken);
                return new StagedPageScanResult(
                    saved,
                    result.Reconciliation,
                    DynamicElementSuggestions: result.DynamicElementSuggestions,
                    ScreenshotPath: result.ScreenshotPath);
            }

            var now = DateTimeOffset.UtcNow;
            var review = new ScanReview(
                ScanReview.CurrentSchemaVersion,
                Guid.NewGuid().ToString("N"),
                baseline.Id,
                ScanReviewStatus.Pending,
                now,
                now,
                null,
                baseline,
                result.Page,
                items);
            await SupersedePendingAsync(result.Page.Id, cancellationToken);
            await WriteAsync(review, cancellationToken);
            return new StagedPageScanResult(
                baseline,
                result.Reconciliation,
                review,
                result.DynamicElementSuggestions,
                result.ScreenshotPath);
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<ScanReview>> ListAsync(
        string? pageId,
        ScanReviewStatus? status,
        CancellationToken cancellationToken)
    {
        return await _gate.WithLockAsync(async () =>
        {
            if (status is not null && !Enum.IsDefined(status.Value))
            {
                throw new WorkspaceValidationException($"Invalid scan review status '{status}'.");
            }

            var reviews = await ReadAllAsync(cancellationToken);
            return reviews
                .Where(review => pageId is null || review.PageId == pageId)
                .Where(review => status is null || review.Status == status)
                .OrderByDescending(review => review.CreatedAt)
                .ToList() as IReadOnlyList<ScanReview>;
        }, cancellationToken);
    }

    public async Task<ScanReview> GetAsync(string reviewId, CancellationToken cancellationToken)
    {
        return await _gate.WithLockAsync(
            () => ReadRequiredAsync(reviewId, cancellationToken),
            cancellationToken);
    }

    public async Task<ScanReview> UpdateDecisionsAsync(
        string reviewId,
        IReadOnlyList<ScanReviewDecisionUpdate> updates,
        CancellationToken cancellationToken)
    {
        return await _gate.WithLockAsync(async () =>
        {
            if (updates.Count == 0)
            {
                throw new WorkspaceValidationException("At least one scan review decision is required.");
            }

            var invalidItemId = updates.FirstOrDefault(update =>
                string.IsNullOrEmpty(update.ItemId) || !ReviewIdPattern().IsMatch(update.ItemId));
            if (invalidItemId is not null)
            {
                throw new WorkspaceValidationException(
                    $"Invalid scan review item ID '{invalidItemId.ItemId}'.");
            }

            var invalidDecision = updates.FirstOrDefault(update => !Enum.IsDefined(update.Decision));
            if (invalidDecision is not null)
            {
                throw new WorkspaceValidationException(
                    $"Invalid decision '{invalidDecision.Decision}' for scan review item '{invalidDecision.ItemId}'.");
            }

            var duplicate = updates.GroupBy(update => update.ItemId, StringComparer.Ordinal)
                .FirstOrDefault(group => group.Count() > 1);
            if (duplicate is not null)
            {
                throw new WorkspaceValidationException(
                    $"Scan review item '{duplicate.Key}' has more than one decision in the request.");
            }

            var review = await ReadRequiredAsync(reviewId, cancellationToken);
            EnsurePending(review);
            var decisions = updates.ToDictionary(update => update.ItemId, update => update.Decision, StringComparer.Ordinal);
            var knownIds = review.Items.Select(item => item.Id).ToHashSet(StringComparer.Ordinal);
            var unknownId = decisions.Keys.FirstOrDefault(id => !knownIds.Contains(id));
            if (unknownId is not null)
            {
                throw new WorkspaceValidationException(
                    $"Scan review '{review.Id}' has no item '{unknownId}'.");
            }

            var updated = review with
            {
                UpdatedAt = DateTimeOffset.UtcNow,
                Items = review.Items
                    .Select(item => decisions.TryGetValue(item.Id, out var decision)
                        ? item with { Decision = decision }
                        : item)
                    .ToList(),
            };
            await WriteAsync(updated, cancellationToken);
            return updated;
        }, cancellationToken);
    }

    public async Task<ScanReviewApplyResult> ApplyAsync(
        string reviewId,
        CancellationToken cancellationToken)
    {
        return await _gate.WithLockAsync(async () =>
        {
            var review = await ReadRequiredAsync(reviewId, cancellationToken);
            EnsurePending(review);
            if (review.Items.Any(item => item.Decision == ScanReviewDecision.Wait))
            {
                throw new ScanReviewConflictException(
                    "scan_review_unresolved",
                    "Every scan review item must be accepted or denied before the review can be applied.");
            }

            var active = await repository.GetPageAsync(review.PageId, cancellationToken)
                ?? throw new KeyNotFoundException($"Page '{review.PageId}' was not found.");
            if (active.UpdatedAt != review.BaselinePage.UpdatedAt)
            {
                throw new ScanReviewConflictException(
                    "scan_review_stale",
                    $"Page '{review.PageId}' changed after scan review '{review.Id}' was created.");
            }

            var acceptedMissingIds = review.Items
                .Where(item => item.Kind == ScanReviewChangeKind.Missing
                    && item.Decision == ScanReviewDecision.Accept)
                .Select(item => item.Element.Id)
                .ToHashSet(StringComparer.Ordinal);
            var references = (await repository.ListTestsAsync(cancellationToken))
                .SelectMany(test => test.Steps
                    .Where(step => step.PageId == review.PageId
                        && step.ElementId is not null
                        && acceptedMissingIds.Contains(step.ElementId))
                    .Select(step => new ScanReviewReference(test.Id, step.Id)))
                .ToList();
            if (references.Count > 0)
            {
                throw new ScanReviewConflictException(
                    "scan_review_removal_referenced",
                    "One or more accepted element removals are referenced by saved tests.",
                    references);
            }

            var newDecisions = review.Items
                .Where(item => item.Kind == ScanReviewChangeKind.New)
                .ToDictionary(item => item.Element.Id, item => item.Decision, StringComparer.Ordinal);
            var missingDecisions = review.Items
                .Where(item => item.Kind == ScanReviewChangeKind.Missing)
                .ToDictionary(item => item.Element.Id, item => item.Decision, StringComparer.Ordinal);
            var baselineById = review.BaselinePage.Elements.ToDictionary(element => element.Id, StringComparer.Ordinal);
            var mergedElements = new List<PageElement>();

            foreach (var proposed in review.ProposedPage.Elements)
            {
                if (newDecisions.TryGetValue(proposed.Id, out var newDecision))
                {
                    if (newDecision == ScanReviewDecision.Accept)
                    {
                        mergedElements.Add(proposed with { NotFound = false });
                    }

                    continue;
                }

                if (missingDecisions.TryGetValue(proposed.Id, out var missingDecision))
                {
                    if (missingDecision == ScanReviewDecision.Deny
                        && baselineById.TryGetValue(proposed.Id, out var baselineElement))
                    {
                        mergedElements.Add(baselineElement);
                    }

                    continue;
                }

                mergedElements.Add(proposed with { NotFound = false });
            }

            var page = await repository.SavePageAsync(
                review.ProposedPage with { Elements = mergedElements },
                cancellationToken);
            var now = DateTimeOffset.UtcNow;
            var applied = review with
            {
                Status = ScanReviewStatus.Applied,
                UpdatedAt = now,
                CompletedAt = now,
            };
            await WriteAsync(applied, cancellationToken);
            return new ScanReviewApplyResult(applied, page);
        }, cancellationToken);
    }

    public async Task<ScanReview> DiscardAsync(string reviewId, CancellationToken cancellationToken)
    {
        return await _gate.WithLockAsync(async () =>
        {
            var review = await ReadRequiredAsync(reviewId, cancellationToken);
            EnsurePending(review);
            var now = DateTimeOffset.UtcNow;
            var discarded = review with
            {
                Status = ScanReviewStatus.Discarded,
                UpdatedAt = now,
                CompletedAt = now,
            };
            await WriteAsync(discarded, cancellationToken);
            return discarded;
        }, cancellationToken);
    }

    public void Dispose()
    {
        _gate.Dispose();
        _atomicWriter.Dispose();
    }

    private async Task SupersedePendingAsync(string pageId, CancellationToken cancellationToken)
    {
        foreach (var review in (await ReadAllAsync(cancellationToken))
                     .Where(review => review.PageId == pageId && review.Status == ScanReviewStatus.Pending))
        {
            var now = DateTimeOffset.UtcNow;
            await WriteAsync(
                review with
                {
                    Status = ScanReviewStatus.Superseded,
                    UpdatedAt = now,
                    CompletedAt = now,
                },
                cancellationToken);
        }
    }

    private static void EnsurePending(ScanReview review)
    {
        if (review.Status != ScanReviewStatus.Pending)
        {
            throw new ScanReviewConflictException(
                "scan_review_not_pending",
                $"Scan review '{review.Id}' is {review.Status.ToString().ToLowerInvariant()} and cannot be changed.");
        }
    }

    private async Task<IReadOnlyList<ScanReview>> ReadAllAsync(CancellationToken cancellationToken)
    {
        var directory = ReviewDirectory();
        if (!Directory.Exists(directory))
        {
            return [];
        }

        var reviews = new List<ScanReview>();
        foreach (var path in Directory.EnumerateFiles(directory, "*.json").OrderBy(path => path, StringComparer.Ordinal))
        {
            reviews.Add(await ReadFileAsync(path, cancellationToken));
        }

        return reviews;
    }

    private async Task<ScanReview> ReadRequiredAsync(string reviewId, CancellationToken cancellationToken)
    {
        ValidateReviewId(reviewId);
        var path = Path.Combine(ReviewDirectory(), $"{reviewId}.json");
        if (!File.Exists(path))
        {
            throw new KeyNotFoundException($"Scan review '{reviewId}' was not found.");
        }

        return await ReadFileAsync(path, cancellationToken);
    }

    private static async Task<ScanReview> ReadFileAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = File.OpenRead(path);
            var review = await JsonSerializer.DeserializeAsync<ScanReview>(stream, JsonOptions, cancellationToken)
                ?? throw new WorkspaceValidationException($"Scan review '{path}' is empty.");
            ValidatePersistedReview(review, path);
            return review;
        }
        catch (JsonException exception)
        {
            throw new WorkspaceValidationException($"Invalid JSON in '{path}': {exception.Message}");
        }
    }

    private async Task WriteAsync(ScanReview review, CancellationToken cancellationToken)
    {
        ValidateReviewId(review.Id);
        var directory = ReviewDirectory();
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, $"{review.Id}.json");
        ValidatePersistedReview(review, path);
        await _atomicWriter.WriteJsonAsync(path, review, JsonOptions, cancellationToken);
    }

    private string ReviewDirectory()
    {
        var root = repository.RootPath ?? throw new WorkspaceNotOpenException();
        return Path.Combine(root, ".cliq", "runs", "scan-reviews");
    }

    private static void ValidateReviewId(string reviewId)
    {
        if (!ReviewIdPattern().IsMatch(reviewId))
        {
            throw new WorkspaceValidationException($"Invalid scan review ID '{reviewId}'.");
        }
    }

    private static void ValidatePersistedReview(ScanReview review, string path)
    {
        if (review.SchemaVersion != ScanReview.CurrentSchemaVersion)
        {
            throw new WorkspaceValidationException(
                $"Unsupported schema version {review.SchemaVersion} in '{path}'. Expected {ScanReview.CurrentSchemaVersion}.");
        }

        if (!ReviewIdPattern().IsMatch(review.Id)
            || !string.Equals(Path.GetFileNameWithoutExtension(path), review.Id, StringComparison.Ordinal))
        {
            throw new WorkspaceValidationException($"Scan review '{path}' has an invalid or mismatched ID.");
        }

        if (review.BaselinePage.Id != review.PageId || review.ProposedPage.Id != review.PageId)
        {
            throw new WorkspaceValidationException(
                $"Scan review '{review.Id}' contains page snapshots for a different page.");
        }

        if (review.Items.Count == 0
            || review.Items.Any(item => !ReviewIdPattern().IsMatch(item.Id))
            || review.Items.Select(item => item.Id).Distinct(StringComparer.Ordinal).Count() != review.Items.Count)
        {
            throw new WorkspaceValidationException(
                $"Scan review '{review.Id}' contains invalid or duplicate item IDs.");
        }


        if (!Enum.IsDefined(review.Status)
            || review.Items.Any(item => !Enum.IsDefined(item.Kind) || !Enum.IsDefined(item.Decision)))
        {
            throw new WorkspaceValidationException(
                $"Scan review '{review.Id}' contains an invalid status, change kind, or decision.");
        }

        var baselineIds = review.BaselinePage.Elements.Select(element => element.Id).ToHashSet(StringComparer.Ordinal);
        var proposedIds = review.ProposedPage.Elements.Select(element => element.Id).ToHashSet(StringComparer.Ordinal);
        var baselineById = review.BaselinePage.Elements.ToDictionary(element => element.Id, StringComparer.Ordinal);
        var proposedById = review.ProposedPage.Elements.ToDictionary(element => element.Id, StringComparer.Ordinal);
        if (review.Items
                .Select(item => $"{item.Kind}:{item.Element.Id}")
                .Distinct(StringComparer.Ordinal)
                .Count() != review.Items.Count
            || review.Items.Any(item => item.Kind == ScanReviewChangeKind.New
                && (!proposedIds.Contains(item.Element.Id)
                    || !Equals(proposedById[item.Element.Id], item.Element)))
            || review.Items.Any(item => item.Kind == ScanReviewChangeKind.Missing
                && (!baselineIds.Contains(item.Element.Id)
                    || !Equals(baselineById[item.Element.Id], item.Element))))
        {
            throw new WorkspaceValidationException(
                $"Scan review '{review.Id}' contains an item that is absent from its corresponding page snapshot.");
        }

        var shouldBeComplete = review.Status != ScanReviewStatus.Pending;
        if (shouldBeComplete != review.CompletedAt.HasValue)
        {
            throw new WorkspaceValidationException(
                $"Scan review '{review.Id}' has inconsistent status and completion metadata.");
        }
    }

    [GeneratedRegex("^[a-f0-9]{32}$", RegexOptions.CultureInvariant)]
    private static partial Regex ReviewIdPattern();
}

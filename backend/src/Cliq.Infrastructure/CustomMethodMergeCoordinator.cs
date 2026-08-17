using Cliq.Domain;
using Cliq.Infrastructure.Generation;

namespace Cliq.Infrastructure;

/// <summary>
/// The custom-method "always ask" merge/waiting-list subsystem <see cref="WorkspaceCodeGeneration.GenerateAsync"/>
/// calls into for each page: merging fresh candidates extracted from the existing generated file
/// with anything already deferred for that page, building what the VS extension's merge UI shows
/// for an unresolved conflict, reconciling the waiting list once a resolution arrives, and cleaning
/// up a renamed page's stale old file. Extracted out of <see cref="WorkspaceCodeGeneration"/> so
/// this fairly self-contained subsystem isn't entangled with that class's own directory setup and
/// page/test codegen loop - GenerateAsync still owns deciding, page by page, whether a decision is
/// needed and which of these to call; this class only owns how each of those operations is done.
/// </summary>
internal sealed class CustomMethodMergeCoordinator(WorkspaceRepository repository)
{
    /// <summary>
    /// A page renamed since it was last generated now maps to a different class name, leaving its
    /// old <c>&lt;OldName&gt;.cs</c> file behind under a name no page points to anymore -
    /// regeneration never reads or rewrites that file since it addresses pages by their current
    /// class name only. If the old file has no hand-written <c>[CliqCustomMethod]</c> content it's
    /// deleted (nothing to lose); otherwise it's left exactly where it is and reported as a
    /// warning rather than guessed at.
    /// </summary>
    /// <returns>
    /// Whether the rename conflict is now fully dealt with (the old file no longer exists) - false
    /// means it was left in place, unresolved, so the caller must keep pointing its generation
    /// state at the old name until a future call resolves it.
    /// </returns>
    public async Task<bool> HandleRenamedPageObjectAsync(
        string pageObjectDirectory,
        string pageId,
        string previousClassName,
        IReadOnlyDictionary<string, IReadOnlyList<string>>? customMethodResolutions,
        IReadOnlyDictionary<string, IReadOnlyList<string>>? customMethodDiscards,
        List<CustomMethodRecoveryWarning> warnings,
        List<PendingCustomMethodMerge> pendingMerges,
        CancellationToken cancellationToken)
    {
        var oldPath = Path.Combine(pageObjectDirectory, $"{previousClassName}.cs");
        if (!File.Exists(oldPath))
        {
            return true;
        }

        var oldSource = await File.ReadAllTextAsync(oldPath, cancellationToken);
        var customMethods = CustomMethodExtractor.Extract(oldSource);
        if (customMethods.Count == 0)
        {
            File.Delete(oldPath);
            return true;
        }

        if (customMethodResolutions is not null &&
            customMethodResolutions.TryGetValue(pageId, out var resolvedMethods))
        {
            // Resolved via the merge UI: whatever wasn't chosen to carry over and wasn't
            // explicitly discarded goes to the waiting list, and the stale renamed-away file can
            // finally go - nothing of value is left in it.
            IReadOnlyList<string> discardedMethods =
                customMethodDiscards is not null && customMethodDiscards.TryGetValue(pageId, out var discards)
                    ? discards
                    : [];
            var candidates = await BuildMergedCandidatesAsync(pageId, oldSource, cancellationToken);
            await DeferUnresolvedAsync(
                pageId, previousClassName, candidates, resolvedMethods, discardedMethods, cancellationToken);
            File.Delete(oldPath);
            return true;
        }

        warnings.Add(new CustomMethodRecoveryWarning(previousClassName, customMethods.Count, 0, oldPath));
        var pending = await BuildPendingMergeAsync(pageId, previousClassName, oldSource, cancellationToken);
        if (pending is not null)
        {
            pendingMerges.Add(pending);
        }

        return false;
    }

    /// <summary>
    /// The full candidate set for one page's conflict - everything <see cref="CustomMethodExtractor.ExtractAll"/>
    /// finds in the given source, merged with anything already deferred for the same page in the
    /// global waiting list (a fresh candidate wins over a stale deferred duplicate with the same
    /// signature). Used both to build what the merge UI shows (<see cref="BuildPendingMergeAsync"/>)
    /// AND, critically, by <see cref="DeferUnresolvedAsync"/>'s cleanup pass when a resolution
    /// arrives - a candidate that only exists in the waiting list (carried over from an earlier,
    /// separate conflict on the same page) still needs to be visited there too, or a user choosing
    /// Carry Over/Discard for it would never clear its now-stale waiting-list entry.
    /// </summary>
    public async Task<IReadOnlyList<CustomMethodCandidate>> BuildMergedCandidatesAsync(
        string pageId,
        string sourceText,
        CancellationToken cancellationToken)
    {
        var freshCandidates = BuildCandidates(
            CustomMethodExtractor.ExtractAll(sourceText),
            CustomMethodExtractor.IsBalanced(sourceText));
        var waitingListCandidates = (await repository.ListPendingCustomMethodsAsync(cancellationToken))
            .Where(entry => string.Equals(entry.PageId, pageId, StringComparison.Ordinal))
            .Select(entry => new CustomMethodCandidate(
                entry.Signature,
                entry.SourceText,
                entry.HasSyntaxError,
                entry.HasSyntaxError
                    ? ["Previously deferred with a syntax error."]
                    : Array.Empty<string>()));

        return freshCandidates
            .Concat(waitingListCandidates)
            .GroupBy(candidate => candidate.Signature, StringComparer.Ordinal)
            .Select(group => group.First())
            .ToList();
    }

    /// <summary>
    /// Wraps <see cref="BuildMergedCandidatesAsync"/> as the <see cref="PendingCustomMethodMerge"/>
    /// the VS extension's merge UI shows for one page's conflict. Returns null when there's
    /// nothing to show.
    /// </summary>
    public async Task<PendingCustomMethodMerge?> BuildPendingMergeAsync(
        string pageId,
        string pageObjectName,
        string sourceText,
        CancellationToken cancellationToken)
    {
        var merged = await BuildMergedCandidatesAsync(pageId, sourceText, cancellationToken);
        return merged.Count == 0 ? null : new PendingCustomMethodMerge(pageId, pageObjectName, merged);
    }

    // fileIsBalanced covers the whole page object file, not just this one span - a missing brace
    // anywhere else in the file can make FindMethodEnd's depth-walk latch onto the wrong closing
    // brace (most commonly the class's own) for a span it otherwise reports as ClosedCleanly, so
    // a syntactically-valid-looking recovery still can't be trusted when the file as a whole is
    // unbalanced. See CustomMethodExtractor.IsBalanced.
    private static IReadOnlyList<CustomMethodCandidate> BuildCandidates(
        IReadOnlyList<ExtractedSpan> spans,
        bool fileIsBalanced)
    {
        var candidates = new List<CustomMethodCandidate>();
        foreach (var span in spans)
        {
            IReadOnlyList<string> syntaxErrors;
            if (!span.ClosedCleanly)
            {
                syntaxErrors = ["The custom method's body could not be parsed (unbalanced braces or unsupported syntax)."];
            }
            else if (!fileIsBalanced)
            {
                syntaxErrors = ["The page object file has unbalanced braces elsewhere, so this method's extracted boundaries can't be trusted."];
            }
            else
            {
                syntaxErrors = CustomMethodSyntaxChecker.FindSyntaxErrors(span.SourceText);
            }

            candidates.Add(new CustomMethodCandidate(
                ExtractSignature(span.SourceText), span.SourceText, syntaxErrors.Count > 0, syntaxErrors));
        }

        return candidates;
    }

    private static string ExtractSignature(string sourceText)
    {
        foreach (var line in sourceText.Split('\n'))
        {
            var trimmed = line.Trim('\r', ' ', '\t');
            if (trimmed.Length == 0 ||
                trimmed.StartsWith("///", StringComparison.Ordinal) ||
                (trimmed.StartsWith('[') && trimmed.EndsWith(']')))
            {
                continue;
            }

            return trimmed.TrimEnd('{').TrimEnd();
        }

        return sourceText.Split('\n')[0].Trim();
    }

    /// <summary>
    /// Splits every candidate the user didn't choose to carry over into two buckets: explicitly
    /// discarded (permanently dropped - any existing waiting-list entry for it is deleted, and no
    /// new one is created) or still undecided (deferred/upserted into the global waiting list,
    /// preserving its existing entry Id if it was already there, so a VS extension caller's cached
    /// reference doesn't go stale). Also clears out any waiting-list entry for a candidate that WAS
    /// carried over, since it's resolved now and shouldn't reappear.
    /// </summary>
    public async Task DeferUnresolvedAsync(
        string pageId,
        string pageObjectName,
        IReadOnlyList<CustomMethodCandidate> candidates,
        IReadOnlyList<string> resolvedMethods,
        IReadOnlyList<string> discardedMethods,
        CancellationToken cancellationToken)
    {
        var resolvedSet = new HashSet<string>(resolvedMethods, StringComparer.Ordinal);
        var discardedSet = new HashSet<string>(discardedMethods, StringComparer.Ordinal);
        var existingWaitingList = await repository.ListPendingCustomMethodsAsync(cancellationToken);

        foreach (var candidate in candidates)
        {
            var existing = existingWaitingList.FirstOrDefault(entry =>
                string.Equals(entry.PageId, pageId, StringComparison.Ordinal) &&
                string.Equals(entry.Signature, candidate.Signature, StringComparison.Ordinal));

            if (resolvedSet.Contains(candidate.SourceText) || discardedSet.Contains(candidate.SourceText))
            {
                if (existing is not null)
                {
                    await repository.DeletePendingCustomMethodAsync(existing.Id, cancellationToken);
                }

                continue;
            }

            await repository.SavePendingCustomMethodAsync(
                new PendingCustomMethodEntry(
                    existing?.Id ?? Guid.NewGuid().ToString("N"),
                    pageId,
                    pageObjectName,
                    candidate.Signature,
                    candidate.SourceText,
                    candidate.HasSyntaxError,
                    DateTimeOffset.UtcNow),
                cancellationToken);
        }
    }
}

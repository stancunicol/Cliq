using Cliq.Domain;

namespace Cliq.Infrastructure;

/// <summary>
/// Matches a freshly scanned element list against the previously saved one so elements that
/// still exist keep their stable <see cref="PageElement.Id"/> (and stay valid for any
/// <c>TestStep</c> referencing them) across re-scans, instead of every scan silently reassigning
/// new IDs. Tiers are tried from most to least reliable; a pair is only accepted where exactly one
/// old and one new element share a key, so ambiguous ties fall through to the next tier rather
/// than risk a wrong match.
/// </summary>
internal static class ElementReconciler
{
    private static readonly Func<PageElement, string?>[] UniqueTierKeys =
    [
        pe => pe.Locator.Strategy == LocatorStrategy.TestId ? $"{pe.Tag}|testId|{pe.Locator.Value}" : null,
        // Same full XPath = same element, per the current discovery priority (testId > xpath > css).
        pe => pe.Locator.Strategy == LocatorStrategy.XPath ? $"{pe.Tag}|xpath|{pe.Locator.Value}" : null,
        pe => pe.Locator.Strategy == LocatorStrategy.Id ? $"{pe.Tag}|id|{pe.Locator.Value}" : null,
        pe => pe.Locator.Strategy == LocatorStrategy.Name ? $"{pe.Tag}|name|{pe.Locator.Value}" : null,
        // Name was derived from testId ?? id ?? name ?? placeholder ?? text (see
        // PuppeteerPageDiscovery.BuildElements), so comparing it recovers placeholder/text-based
        // matching for elements with no stable id/name/testId, without persisting new raw fields.
        pe => $"{pe.Tag}|sanitizedName|{pe.Name}",
    ];

    public static (IReadOnlyList<PageElement> Elements, ScanReconciliationReport Report) Reconcile(
        IReadOnlyList<PageElement> freshElements,
        IReadOnlyList<PageElement> previousElements)
    {
        var unmatchedOld = new HashSet<int>(Enumerable.Range(0, previousElements.Count));
        var unmatchedNew = new HashSet<int>(Enumerable.Range(0, freshElements.Count));
        var matches = new Dictionary<int, int>(); // newIndex -> oldIndex

        foreach (var keyOf in UniqueTierKeys)
        {
            MatchByUniqueKey(freshElements, previousElements, unmatchedNew, unmatchedOld, matches, keyOf);
        }

        MatchByPosition(freshElements, previousElements, unmatchedNew, unmatchedOld, matches);

        var result = new List<PageElement>(freshElements.Count + unmatchedOld.Count);
        for (var i = 0; i < freshElements.Count; i++)
        {
            result.Add(matches.TryGetValue(i, out var oldIndex)
                ? freshElements[i] with { Id = previousElements[oldIndex].Id, NotFound = false }
                : freshElements[i]);
        }

        foreach (var oldIndex in unmatchedOld.OrderBy(i => i))
        {
            result.Add(previousElements[oldIndex] with { NotFound = true });
        }

        var report = new ScanReconciliationReport(
            MatchedCount: matches.Count,
            NewCount: freshElements.Count - matches.Count,
            MissingCount: unmatchedOld.Count);

        return (result, report);
    }

    private static void MatchByUniqueKey(
        IReadOnlyList<PageElement> freshElements,
        IReadOnlyList<PageElement> previousElements,
        HashSet<int> unmatchedNew,
        HashSet<int> unmatchedOld,
        Dictionary<int, int> matches,
        Func<PageElement, string?> keyOf)
    {
        var oldByKey = GroupRemaining(previousElements, unmatchedOld, keyOf);
        var newByKey = GroupRemaining(freshElements, unmatchedNew, keyOf);

        foreach (var (key, oldIndices) in oldByKey)
        {
            if (oldIndices.Count != 1 || !newByKey.TryGetValue(key, out var newIndices) || newIndices.Count != 1)
            {
                continue;
            }

            var oldIndex = oldIndices[0];
            var newIndex = newIndices[0];
            matches[newIndex] = oldIndex;
            unmatchedOld.Remove(oldIndex);
            unmatchedNew.Remove(newIndex);
        }
    }

    private static Dictionary<string, List<int>> GroupRemaining(
        IReadOnlyList<PageElement> elements,
        IEnumerable<int> remainingIndices,
        Func<PageElement, string?> keyOf)
    {
        var groups = new Dictionary<string, List<int>>();
        foreach (var index in remainingIndices)
        {
            var key = keyOf(elements[index]);
            if (key is null)
            {
                continue;
            }

            if (!groups.TryGetValue(key, out var list))
            {
                list = [];
                groups[key] = list;
            }

            list.Add(index);
        }

        return groups;
    }

    // Last-resort tie-breaker for elements with no stable id/name/testId at all: pair whatever's
    // left in original-scan order, within the same (tag, action) group - the same fallback role
    // the structural CSS selector already plays for locators.
    private static void MatchByPosition(
        IReadOnlyList<PageElement> freshElements,
        IReadOnlyList<PageElement> previousElements,
        HashSet<int> unmatchedNew,
        HashSet<int> unmatchedOld,
        Dictionary<int, int> matches)
    {
        var oldGroups = unmatchedOld
            .OrderBy(i => i)
            .GroupBy(i => (previousElements[i].Tag, previousElements[i].Action))
            .ToDictionary(g => g.Key, g => g.ToList());

        var newGroups = unmatchedNew
            .OrderBy(i => i)
            .GroupBy(i => (freshElements[i].Tag, freshElements[i].Action))
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var (key, oldIndices) in oldGroups)
        {
            if (!newGroups.TryGetValue(key, out var newIndices))
            {
                continue;
            }

            var pairCount = Math.Min(oldIndices.Count, newIndices.Count);
            for (var i = 0; i < pairCount; i++)
            {
                matches[newIndices[i]] = oldIndices[i];
                unmatchedOld.Remove(oldIndices[i]);
                unmatchedNew.Remove(newIndices[i]);
            }
        }
    }
}

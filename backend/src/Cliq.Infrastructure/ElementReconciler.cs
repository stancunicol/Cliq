using Cliq.Domain;

namespace Cliq.Infrastructure;

/// <summary>
/// Matches a freshly scanned element list against the previously saved one so elements that
/// still exist keep their stable <see cref="PageElement.Id"/> (and stay valid for any
/// <c>TestStep</c> referencing them) across re-scans, instead of every scan silently reassigning
/// new IDs. Tiers are tried from most to least reliable; a pair is only accepted where exactly one
/// old and one new element share a key, so ambiguous ties fall through to the next tier rather
/// than risk a wrong match. There is no positional or other fuzzy fallback beyond these tiers: an
/// element that matches none of them is reported as new, and its previous counterpart as missing.
/// </summary>
internal static class ElementReconciler
{
    // Mirrors the discovery locator priority exactly (testId > xpath > css) - id/name/sanitized-name
    // tiers were dropped rather than kept as extra fallbacks, so an element with no stable testId or
    // xpath is reconciled only by an exact css-value match; if that value drifted too, the element is
    // simply reported as new/missing rather than guessed at.
    private static readonly Func<PageElement, string?>[] UniqueTierKeys =
    [
        pe => pe.Locator.Strategy == LocatorStrategy.TestId ? $"{pe.Tag}|testId|{pe.Locator.Value}" : null,
        pe => pe.Locator.Strategy == LocatorStrategy.XPath ? $"{pe.Tag}|xpath|{pe.Locator.Value}" : null,
        pe => pe.Locator.Strategy == LocatorStrategy.Css ? $"{pe.Tag}|css|{pe.Locator.Value}" : null,
    ];

    public static (IReadOnlyList<PageElement> Elements, ScanReconciliationReport Report, IReadOnlyList<PageElement> NewElements) Reconcile(
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

        var newElements = unmatchedNew.OrderBy(i => i).Select(i => freshElements[i]).ToList();

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

        return (result, report, newElements);
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
}

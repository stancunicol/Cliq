using System.Text.Json;
using System.Text.Json.Serialization;
using Cliq.Domain;
using Cliq.Infrastructure.Generation;
using OpenQA.Selenium;
using DomainLocatorStrategy = Cliq.Domain.LocatorStrategy;

namespace Cliq.Infrastructure;

/// <summary>
/// When a saved locator no longer resolves at test-run time (e.g. a framework-regenerated id),
/// tries to re-derive it live from the current page using the same "sanitized base-name" signal
/// <c>ElementReconciler</c> uses across re-scans - but only acts on it when exactly one live
/// candidate matches, never guessing among several.
/// </summary>
internal static class SelfHealingLocator
{
    // Adapted from backend/tools/puppeteer-scanner/extract-elements.js's locatorFor/isUniqueMatch/
    // cssEscape/structuralSelector, restricted to a single tag - the acknowledged duplication cost
    // of keeping test execution on Selenium instead of Puppeteer. Keep these two in sync if the
    // identification priority rules ever change.
    private const string CandidateScript = """
        return (() => {
            const tag = arguments[0];
            const testIdAttribute = arguments[1];
            const strategyOrder = arguments[2];
            const ignoreIdRegexes = arguments[3].map((pattern) => new RegExp(pattern, 'i'));

            function isIgnoredId(id) {
                return ignoreIdRegexes.some((regex) => regex.test(id));
            }

            function cssEscape(value) {
                return value.replace(/["\\]/g, '\\$&');
            }

            function isVisible(el) {
                const rect = el.getBoundingClientRect();
                if (rect.width <= 0 || rect.height <= 0) return false;
                const style = window.getComputedStyle(el);
                return style.visibility !== 'hidden' && style.display !== 'none';
            }

            function isUniqueMatch(selector, el) {
                try {
                    const matches = document.querySelectorAll(selector);
                    return matches.length === 1 && matches[0] === el;
                } catch {
                    return false;
                }
            }

            function structuralSelector(el) {
                const parts = [];
                let node = el;
                while (node && node.parentElement && node.nodeName.toLowerCase() !== 'body') {
                    const siblings = Array.from(node.parentElement.children);
                    const index = siblings.indexOf(node) + 1;
                    parts.unshift(`${node.nodeName.toLowerCase()}:nth-child(${index})`);
                    node = node.parentElement;
                }
                return parts.join(' > ');
            }

            const strategyChecks = {
                testId: (el) => {
                    const testId = el.getAttribute(testIdAttribute);
                    if (!testId) return null;
                    const candidate = `[${testIdAttribute}="${cssEscape(testId)}"]`;
                    return isUniqueMatch(candidate, el) ? { strategy: 'css', value: candidate } : null;
                },
                id: (el) => (el.id && !isIgnoredId(el.id) && isUniqueMatch(`#${CSS.escape(el.id)}`, el))
                    ? { strategy: 'id', value: el.id }
                    : null,
                name: (el) => {
                    if (!el.name) return null;
                    const candidate = `${el.tagName.toLowerCase()}[name="${cssEscape(el.name)}"]`;
                    return isUniqueMatch(candidate, el) ? { strategy: 'name', value: el.name } : null;
                },
            };

            function locatorFor(el) {
                for (const key of strategyOrder) {
                    const result = strategyChecks[key] && strategyChecks[key](el);
                    if (result) return result;
                }
                return { strategy: 'css', value: structuralSelector(el) };
            }

            const candidates = Array.from(document.querySelectorAll(tag)).filter(isVisible);
            return JSON.stringify(candidates.map((el) => {
                const locator = locatorFor(el);
                const text = (el.textContent || '').replace(/\s+/g, ' ').trim().slice(0, 40);
                return {
                    testId: el.getAttribute(testIdAttribute),
                    id: (el.id && !isIgnoredId(el.id)) ? el.id : null,
                    name: el.getAttribute('name'),
                    placeholder: el.getAttribute('placeholder'),
                    text,
                    locatorStrategy: locator.strategy,
                    locatorValue: locator.value,
                };
            }));
        })()
        """;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static Locator? TryHeal(IWebDriver driver, PageElement element, ElementIdentificationSettings settings)
    {
        var js = (IJavaScriptExecutor)driver;
        var json = (string)js.ExecuteScript(CandidateScript, element.Tag, settings.TestIdAttribute, settings.StrategyOrder, settings.IgnoreIdPatterns)!;
        var candidates = JsonSerializer.Deserialize<List<Candidate>>(json, JsonOptions) ?? [];
        return SelectMatch(candidates, element.Name, settings.TestIdAttribute);
    }

    internal static Locator? SelectMatch(IReadOnlyList<Candidate> candidates, string expectedName, string testIdAttribute)
    {
        Locator? match = null;
        var matchCount = 0;

        foreach (var candidate in candidates)
        {
            var baseName = candidate.TestId ?? candidate.Id ?? candidate.Name ?? candidate.Placeholder ?? candidate.Text;
            var sanitized = IdentifierNaming.Sanitize(baseName, Guid.NewGuid().ToString("N"));
            if (sanitized != expectedName)
            {
                continue;
            }

            matchCount++;
            match = ToLocator(candidate.LocatorStrategy, candidate.LocatorValue, testIdAttribute);
        }

        return matchCount == 1 ? match : null;
    }

    private static Locator ToLocator(string strategy, string value, string testIdAttribute)
    {
        var testIdPrefix = $"[{testIdAttribute}=\"";
        if (strategy == "css" && value.StartsWith(testIdPrefix, StringComparison.Ordinal) && value.EndsWith("\"]", StringComparison.Ordinal))
        {
            return new Locator(DomainLocatorStrategy.TestId, value[testIdPrefix.Length..^2]);
        }

        return new Locator(
            strategy switch
            {
                "id" => DomainLocatorStrategy.Id,
                "name" => DomainLocatorStrategy.Name,
                _ => DomainLocatorStrategy.Css,
            },
            value);
    }

    internal sealed class Candidate
    {
        public string? TestId { get; set; }
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Placeholder { get; set; }
        public string? Text { get; set; }
        public string LocatorStrategy { get; set; } = "";
        public string LocatorValue { get; set; } = "";
    }
}

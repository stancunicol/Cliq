using System.Text.Json;
using System.Text.Json.Serialization;
using OpenQA.Selenium;

namespace PageObjectGenerator.Core;

public static class ElementScanner
{
    // Walks the live DOM and describes every fillable/clickable/assertable
    // element that a human tester could actually see and interact with -
    // elements with zero rendered size or hidden via CSS are skipped, since
    // generating a test for something nobody could click just produces a test
    // that fails every run for reasons unrelated to the page's actual behavior.
    // For each surviving element: the locator chosen for it (data-testid > id > name > structural
    // nth-child fallback, each verified to uniquely match only that element
    // before being accepted - a duplicate id/name in broken markup falls
    // through to the next candidate instead of silently resolving to the
    // wrong element later) and the naming hints used to derive an identifier.
    // Headings are captured as "assert" targets - their current text becomes
    // the expected value baked into the generated test, i.e. a snapshot
    // assertion, not an interaction. Radio inputs sharing a "name" are
    // collapsed into a single "radio" descriptor per group (one method
    // selects among all options), not one descriptor per option.
    // Selenium's ExecuteScript runs this as a function body, so an implicit
    // top-level expression result (unlike CDP's Runtime.evaluate) is not
    // returned automatically - the explicit "return" below is required.
    private const string ExtractScript = """
        return (() => {
            const candidates = Array.from(document.querySelectorAll(
                'input, textarea, select, button, a[href], h1, h2, h3, h4, h5, h6'
            ));

            function cssEscape(value) {
                return value.replace(/["\\]/g, '\\$&');
            }

            // Skips anything a human tester couldn't actually see or click: zero
            // rendered size, or hidden via display:none/visibility:hidden. A
            // disabled-but-visible element is left in - a human can still see it
            // and verify its state, even if they can't act on it.
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

            function locatorFor(el) {
                const testId = el.getAttribute('data-testid');
                if (testId) {
                    const candidate = `[data-testid="${cssEscape(testId)}"]`;
                    if (isUniqueMatch(candidate, el)) return { strategy: 'css', value: candidate };
                }
                if (el.id && isUniqueMatch(`#${CSS.escape(el.id)}`, el)) {
                    return { strategy: 'id', value: el.id };
                }
                if (el.name) {
                    const candidate = `${el.tagName.toLowerCase()}[name="${cssEscape(el.name)}"]`;
                    if (isUniqueMatch(candidate, el)) return { strategy: 'name', value: el.name };
                }
                return { strategy: 'css', value: structuralSelector(el) };
            }

            function kindFor(el) {
                const tag = el.tagName.toLowerCase();
                if (['h1', 'h2', 'h3', 'h4', 'h5', 'h6'].includes(tag)) return 'assert';
                if (tag === 'textarea') return 'fill';
                if (tag === 'input') {
                    const type = (el.getAttribute('type') || 'text').toLowerCase();
                    if (type === 'checkbox') return 'check';
                    if (type === 'radio') return 'radio';
                    if (['submit', 'button', 'reset'].includes(type)) return 'click';
                    return 'fill';
                }
                if (tag === 'button' || tag === 'a') return 'click';
                if (tag === 'select') return el.multiple ? 'multiselect' : 'select';
                return null;
            }

            const seenRadioGroups = new Set();

            const result = candidates.map((el) => {
                if (!isVisible(el)) return null;

                const kind = kindFor(el);
                if (!kind) return null;

                let groupName = null;
                if (kind === 'radio') {
                    groupName = el.getAttribute('name');
                    if (!groupName || seenRadioGroups.has(groupName)) return null;
                    seenRadioGroups.add(groupName);
                }

                const text = (el.textContent || '').trim();
                if (kind === 'assert' && !text) return null;

                const locator = kind === 'radio'
                    ? { strategy: 'name', value: groupName }
                    : locatorFor(el);

                return {
                    tag: el.tagName.toLowerCase(),
                    kind,
                    strategy: locator.strategy,
                    locatorValue: locator.value,
                    groupName,
                    testId: el.getAttribute('data-testid'),
                    id: el.id || null,
                    name: el.getAttribute('name'),
                    placeholder: el.getAttribute('placeholder'),
                    text: text.slice(0, 40),
                    assertText: kind === 'assert' ? text : null,
                };
            }).filter(Boolean);

            return JSON.stringify(result);
        })()
        """;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static IReadOnlyList<ElementDescriptor> Scan(IWebDriver driver)
    {
        var js = (IJavaScriptExecutor)driver;
        var json = (string)js.ExecuteScript(ExtractScript)!;
        var rawElements = JsonSerializer.Deserialize<List<RawElement>>(json, JsonOptions) ?? [];

        var descriptors = new List<ElementDescriptor>();
        var usedNames = new Dictionary<string, int>();

        for (var i = 0; i < rawElements.Count; i++)
        {
            var el = rawElements[i];
            var baseName = el.Kind == "radio"
                ? el.GroupName
                : el.TestId ?? el.Id ?? el.Name ?? el.Placeholder ?? el.Text;
            if (string.IsNullOrWhiteSpace(baseName))
            {
                baseName = $"{el.Tag}_{i}";
            }

            var name = IdentifierNaming.Sanitize(baseName, $"{el.Tag}_{i}");

            usedNames.TryGetValue(name, out var seen);
            usedNames[name] = seen + 1;
            if (seen > 0)
            {
                name = $"{name}_{seen + 1}";
            }

            descriptors.Add(new ElementDescriptor(
                el.Tag, ParseKind(el.Kind), ParseStrategy(el.Strategy), el.LocatorValue, name, el.AssertText, el.GroupName));
        }

        return descriptors;
    }

    private static ElementActionKind ParseKind(string kind) => kind switch
    {
        "fill" => ElementActionKind.Fill,
        "click" => ElementActionKind.Click,
        "select" => ElementActionKind.Select,
        "check" => ElementActionKind.Check,
        "assert" => ElementActionKind.Assert,
        "radio" => ElementActionKind.Radio,
        "multiselect" => ElementActionKind.MultiSelect,
        _ => throw new InvalidOperationException($"Unknown element kind '{kind}'"),
    };

    private static LocatorStrategy ParseStrategy(string strategy) => strategy switch
    {
        "id" => LocatorStrategy.Id,
        "name" => LocatorStrategy.Name,
        "css" => LocatorStrategy.CssSelector,
        _ => throw new InvalidOperationException($"Unknown locator strategy '{strategy}'"),
    };

    private sealed class RawElement
    {
        public string Tag { get; set; } = "";
        public string Kind { get; set; } = "";
        public string Strategy { get; set; } = "";
        public string LocatorValue { get; set; } = "";
        public string? GroupName { get; set; }
        public string? TestId { get; set; }
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Placeholder { get; set; }
        public string? Text { get; set; }
        public string? AssertText { get; set; }
    }
}

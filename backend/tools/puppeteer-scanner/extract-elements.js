'use strict';

// Runs inside the page via page.evaluate() - must be self-contained (no
// closures over outer scope, including module-level consts) since Puppeteer
// serializes only this function's own source into the page. `config`
// controls which attribute counts as "testId" and the priority order of
// identification strategies tried before falling back to a structural CSS
// selector; see backend/tools/puppeteer-scanner's --locator-config flag,
// driven by the workspace's settings.json.
//
// `id` is deliberately not a locator strategy: framework-generated ids are
// too often volatile, so an element's `id` is only ever used as a naming
// hint (see the `id` field on the returned objects), never as the thing
// Selenium looks the element up by.
function extractElements(config) {
  const defaults = { testIdAttribute: 'data-testid', strategyOrder: ['testId', 'xpath'], ignoreIdPatterns: [] };
  const { testIdAttribute, strategyOrder, ignoreIdPatterns } = { ...defaults, ...config };

  const ignoreIdRegexes = ignoreIdPatterns.map((pattern) => new RegExp(pattern, 'i'));

  // A framework-generated id (React Select, Ember, Angular Material, GUIDs, ...) is volatile
  // across page loads even without any DOM/data change, so it's untrustworthy both as a locator
  // and as a naming source - treat it as if the element had no id at all.
  function isIgnoredId(id) {
    return ignoreIdRegexes.some((regex) => regex.test(id));
  }

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

  // Bare `div`s are the least stable thing to select on - they carry no semantic meaning and
  // shift constantly as layout wrappers get added or removed - so the structural CSS fallback
  // steps over them with a wildcard instead of pinning to the tag name.
  function structuralSelector(el) {
    const parts = [];
    let node = el;
    while (node && node.parentElement && node.nodeName.toLowerCase() !== 'body') {
      const siblings = Array.from(node.parentElement.children);
      const index = siblings.indexOf(node) + 1;
      const tag = node.nodeName.toLowerCase() === 'div' ? '*' : node.nodeName.toLowerCase();
      parts.unshift(`${tag}:nth-child(${index})`);
      node = node.parentElement;
    }
    return parts.join(' > ');
  }

  // XPath 1.0 has no string-escape mechanism, so a literal containing both quote characters has
  // to be assembled with concat() instead of quoted directly.
  function xpathLiteral(value) {
    if (!value.includes('"')) return `"${value}"`;
    if (!value.includes("'")) return `'${value}'`;
    const segments = value.split('"');
    return `concat(${segments.map((part, i) => i < segments.length - 1 ? `"${part}", '"'` : `"${part}"`).join(', ')})`;
  }

  function normalizeText(value) {
    return (value || '').replace(/\s+/g, ' ').trim();
  }

  function isUniqueXPathMatch(xpath, el) {
    try {
      const result = document.evaluate(xpath, document, null, XPathResult.ORDERED_NODE_SNAPSHOT_TYPE, null);
      return result.snapshotLength === 1 && result.snapshotItem(0) === el;
    } catch {
      return false;
    }
  }

  function labelTextFor(el) {
    if (el.id) {
      const byFor = document.querySelector(`label[for="${cssEscape(el.id)}"]`);
      if (byFor) return normalizeText(byFor.textContent);
    }
    const wrapping = el.closest('label');
    if (wrapping) return normalizeText(wrapping.textContent);
    const labelledBy = el.getAttribute('aria-labelledby');
    if (labelledBy) {
      const ref = document.getElementById(labelledBy);
      if (ref) return normalizeText(ref.textContent);
    }
    return null;
  }

  // A field's own <label> is the most human-legible, most refactor-resistant anchor there is -
  // it survives id/class churn and reads the same way a manual tester would describe the field.
  function xpathByLabel(el) {
    const text = labelTextFor(el);
    if (!text) return null;
    const tag = el.tagName.toLowerCase();
    const xp = `//label[contains(normalize-space(string(.)), ${xpathLiteral(text)})]/following::${tag}[1]`;
    return isUniqueXPathMatch(xp, el) ? xp : null;
  }

  // No label association - look for a nearby container that already carries a test id and scope
  // the search to it, so the resulting path reads as "the Nth <tag> inside that known region"
  // rather than an absolute, layout-dependent chain.
  function xpathByAncestorTestId(el, attribute) {
    const tag = el.tagName.toLowerCase();
    let node = el.parentElement;
    let depth = 0;
    while (node && depth < 6) {
      const testId = node.getAttribute(attribute);
      if (testId) {
        const scoped = `//*[@${attribute}=${xpathLiteral(testId)}]//${tag}`;
        const matches = document.evaluate(scoped, document, null, XPathResult.ORDERED_NODE_SNAPSHOT_TYPE, null);
        for (let i = 0; i < matches.snapshotLength; i++) {
          if (matches.snapshotItem(i) === el) {
            return matches.snapshotLength === 1 ? scoped : `(${scoped})[${i + 1}]`;
          }
        }
      }
      node = node.parentElement;
      depth += 1;
    }
    return null;
  }

  // Buttons, links and headings usually carry their own identifying text - contains() over
  // normalize-space tolerates incidental whitespace changes that an exact match would break on.
  function xpathByOwnAttribute(el) {
    const tag = el.tagName.toLowerCase();
    const text = normalizeText(el.textContent);
    if (text) {
      const byText = `//${tag}[contains(normalize-space(string(.)), ${xpathLiteral(text)})]`;
      if (isUniqueXPathMatch(byText, el)) return byText;
    }
    const placeholder = el.getAttribute('placeholder');
    if (placeholder) {
      const byPlaceholder = `//${tag}[contains(@placeholder, ${xpathLiteral(placeholder)})]`;
      if (isUniqueXPathMatch(byPlaceholder, el)) return byPlaceholder;
    }
    const ariaLabel = el.getAttribute('aria-label');
    if (ariaLabel) {
      const byAria = `//${tag}[contains(@aria-label, ${xpathLiteral(ariaLabel)})]`;
      if (isUniqueXPathMatch(byAria, el)) return byAria;
    }
    return null;
  }

  function relativePositionPath(el, ancestor) {
    const parts = [];
    let node = el;
    while (node && node !== ancestor) {
      const siblings = Array.from(node.parentElement.children).filter((sibling) => sibling.tagName === node.tagName);
      const index = siblings.indexOf(node) + 1;
      parts.unshift(`${node.tagName.toLowerCase()}[${index}]`);
      node = node.parentElement;
    }
    return parts.join('/');
  }

  // Last resort within the xpath tier: anchor on the nearest ancestor that has a non-volatile id,
  // rather than an absolute /html/body/... chain that breaks on any unrelated layout change. Reuses
  // the same isIgnoredId check as everything else, so a framework-regenerated ancestor id (a React
  // Select/Ember/Angular Material wrapper, say) can't end up baked into a "stable" xpath either.
  // If nothing qualifies up the tree, this - and the whole xpath tier - gives up, and locatorFor()
  // falls through to the structural CSS selector.
  function xpathByNearestIdAncestor(el) {
    let node = el.parentElement;
    let depth = 0;
    while (node && depth < 8) {
      if (node.id && !isIgnoredId(node.id)) {
        const relative = relativePositionPath(el, node);
        const xp = `//*[@id=${xpathLiteral(node.id)}]/${relative}`;
        if (isUniqueXPathMatch(xp, el)) return xp;
      }
      node = node.parentElement;
      depth += 1;
    }
    return null;
  }

  function xpathFor(el) {
    return xpathByLabel(el)
      || xpathByAncestorTestId(el, testIdAttribute)
      || xpathByOwnAttribute(el)
      || xpathByNearestIdAncestor(el);
  }

  const strategyChecks = {
    testId: (el) => {
      const testId = el.getAttribute(testIdAttribute);
      if (!testId) return null;
      const candidate = `[${testIdAttribute}="${cssEscape(testId)}"]`;
      return isUniqueMatch(candidate, el) ? { strategy: 'css', value: candidate } : null;
    },
    xpath: (el) => {
      const xp = xpathFor(el);
      return xp ? { strategy: 'xpath', value: xp } : null;
    },
  };

  function locatorFor(el) {
    for (const key of strategyOrder) {
      const result = strategyChecks[key]?.(el);
      if (result) return result;
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

  return candidates
    .map((el) => {
      if (!isVisible(el)) return null;

      const kind = kindFor(el);
      if (!kind) return null;

      let groupName = null;
      if (kind === 'radio') {
        groupName = el.getAttribute('name');
        if (!groupName || seenRadioGroups.has(groupName)) return null;
        seenRadioGroups.add(groupName);
      }

      // Collapse whitespace (including any real newlines/tabs from the
      // source HTML's own formatting) into single spaces, the same way
      // a browser collapses it when rendering - textContent alone keeps
      // it raw, which both looks wrong as a naming hint and would not
      // match what the generated tests actually read back later.
      const text = (el.textContent || '').replace(/\s+/g, ' ').trim();
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
        testId: el.getAttribute(testIdAttribute),
        id: (el.id && !isIgnoredId(el.id)) ? el.id : null,
        name: el.getAttribute('name'),
        placeholder: el.getAttribute('placeholder'),
        text: text.slice(0, 40),
        assertText: kind === 'assert' ? text : null,
      };
    })
    .filter(Boolean);
}

module.exports = { extractElements };

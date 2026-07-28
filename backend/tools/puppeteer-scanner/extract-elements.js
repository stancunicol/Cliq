'use strict';

// Runs inside the page via page.evaluate() - must be self-contained (no
// closures over outer scope, including module-level consts) since Puppeteer
// serializes only this function's own source into the page. `config`
// controls which attribute counts as "testId" and the priority order of
// identification strategies tried before falling back to a structural CSS
// selector; see backend/tools/puppeteer-scanner's --locator-config flag,
// driven by the workspace's settings.json.
function extractElements(config) {
  const defaults = { testIdAttribute: 'data-testid', strategyOrder: ['testId', 'id', 'name'] };
  const { testIdAttribute, strategyOrder } = { ...defaults, ...config };

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

  const strategyChecks = {
    testId: (el) => {
      const testId = el.getAttribute(testIdAttribute);
      if (!testId) return null;
      const candidate = `[${testIdAttribute}="${cssEscape(testId)}"]`;
      return isUniqueMatch(candidate, el) ? { strategy: 'css', value: candidate } : null;
    },
    id: (el) => (el.id && isUniqueMatch(`#${CSS.escape(el.id)}`, el))
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
        id: el.id || null,
        name: el.getAttribute('name'),
        placeholder: el.getAttribute('placeholder'),
        text: text.slice(0, 40),
        assertText: kind === 'assert' ? text : null,
      };
    })
    .filter(Boolean);
}

module.exports = { extractElements };

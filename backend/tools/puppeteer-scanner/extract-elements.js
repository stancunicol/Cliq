'use strict';

// Runs inside the page via page.evaluate() - must be self-contained (no
// closures over outer scope) since Puppeteer serializes it into the page.
function extractElements() {
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
        testId: el.getAttribute('data-testid'),
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

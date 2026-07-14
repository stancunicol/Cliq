from __future__ import annotations

import keyword
import re
from dataclasses import dataclass

from engine.page import Page

# Walks the live DOM and describes every fillable/clickable element: the
# selector chosen (data-testid > id > name > structural nth-child fallback)
# and the naming hints used later to derive a Python identifier.
_EXTRACT_SCRIPT = r"""
(() => {
    const candidates = Array.from(document.querySelectorAll(
        'input, textarea, select, button, a[href]'
    ));

    function cssEscape(value) {
        return value.replace(/["\\]/g, '\\$&');
    }

    function selectorFor(el) {
        const testId = el.getAttribute('data-testid');
        if (testId) return `[data-testid="${cssEscape(testId)}"]`;
        if (el.id) return `#${CSS.escape(el.id)}`;
        if (el.name) return `${el.tagName.toLowerCase()}[name="${cssEscape(el.name)}"]`;

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

    function kindFor(el) {
        const tag = el.tagName.toLowerCase();
        if (tag === 'textarea') return 'fill';
        if (tag === 'input') {
            const type = (el.getAttribute('type') || 'text').toLowerCase();
            if (type === 'checkbox') return 'check';
            if (['radio', 'submit', 'button', 'reset'].includes(type)) return 'click';
            return 'fill';
        }
        if (tag === 'button' || tag === 'a') return 'click';
        if (tag === 'select') return 'select';
        return null;
    }

    return candidates.map((el) => {
        const kind = kindFor(el);
        if (!kind) return null;
        return {
            tag: el.tagName.toLowerCase(),
            kind,
            selector: selectorFor(el),
            testId: el.getAttribute('data-testid'),
            id: el.id || null,
            name: el.getAttribute('name'),
            placeholder: el.getAttribute('placeholder'),
            text: (el.textContent || '').trim().slice(0, 40),
        };
    }).filter(Boolean);
})()
"""


@dataclass
class ElementDescriptor:
    tag: str
    kind: str  # "fill" | "click" | "select" | "check"
    selector: str
    suggested_name: str


_PYTHON_KEYWORDS = set(keyword.kwlist)


def _sanitize_identifier(raw: str | None, fallback: str) -> str:
    raw = re.sub(r"[^0-9a-zA-Z]+", "_", raw or "").strip("_").lower()
    raw = re.sub(r"^[0-9_]+", "", raw)
    if not raw or raw in _PYTHON_KEYWORDS:
        raw = fallback
    return raw


async def extract_elements(page: Page) -> list[ElementDescriptor]:
    raw_elements = await page.evaluate(_EXTRACT_SCRIPT)

    descriptors: list[ElementDescriptor] = []
    used_names: dict[str, int] = {}

    for i, el in enumerate(raw_elements):
        base_name = (
            el.get("testId") or el.get("id") or el.get("name")
            or el.get("placeholder") or el.get("text") or f"{el['tag']}_{i}"
        )
        name = _sanitize_identifier(base_name, f"{el['tag']}_{i}")

        seen = used_names.get(name, 0)
        used_names[name] = seen + 1
        if seen:
            name = f"{name}_{seen + 1}"

        descriptors.append(
            ElementDescriptor(tag=el["tag"], kind=el["kind"], selector=el["selector"], suggested_name=name)
        )

    return descriptors


_CLASS_TEMPLATE = """from engine.page import Page


class {class_name}:
    def __init__(self, page: Page) -> None:
        self._page = page

{methods}"""

_FILL_METHOD = """    async def fill_{name}(self, value: str) -> None:
        await self._page.fill({selector!r}, value)
"""

_CLICK_METHOD = """    async def click_{name}(self) -> None:
        await self._page.click({selector!r})
"""

_SELECT_METHOD = """    async def select_{name}(self, value: str) -> None:
        await self._page.select_option({selector!r}, value)
"""

_CHECK_METHOD = """    async def set_{name}(self, checked: bool) -> None:
        await self._page.set_checked({selector!r}, checked)
"""

_METHOD_TEMPLATES = {
    "fill": _FILL_METHOD,
    "click": _CLICK_METHOD,
    "select": _SELECT_METHOD,
    "check": _CHECK_METHOD,
}


def generate_page_object(class_name: str, elements: list[ElementDescriptor]) -> str:
    methods = [
        _METHOD_TEMPLATES[el.kind].format(name=el.suggested_name, selector=el.selector) for el in elements
    ]
    body = "\n".join(methods) if methods else "    pass\n"
    return _CLASS_TEMPLATE.format(class_name=class_name, methods=body)

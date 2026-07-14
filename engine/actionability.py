from __future__ import annotations

import asyncio
import time
from dataclasses import dataclass

from engine.cdp_connection import CDPConnection

# Auto-executing JS expression: reads current state of one element in a single
# round-trip, so each poll iteration costs exactly one Runtime.evaluate call.
_CHECK_SCRIPT = """
(() => {{
    const el = document.querySelector({selector!r});
    if (!el) return {{ found: false }};

    const inViewport = (r) => r.top >= 0 && r.left >= 0
        && r.bottom <= window.innerHeight && r.right <= window.innerWidth;

    let rect = el.getBoundingClientRect();
    if (!inViewport(rect)) {{
        el.scrollIntoView({{ block: 'center', inline: 'center' }});
        rect = el.getBoundingClientRect();
    }}

    const style = getComputedStyle(el);
    const visible = rect.width > 0 && rect.height > 0
        && style.visibility !== 'hidden' && style.display !== 'none'
        && parseFloat(style.opacity) > 0;
    const enabled = !('disabled' in el) || !el.disabled;

    const cx = rect.left + rect.width / 2;
    const cy = rect.top + rect.height / 2;
    const topEl = document.elementFromPoint(cx, cy);
    const hitTarget = topEl === el || (el.contains && el.contains(topEl));

    return {{
        found: true, visible, enabled, hitTarget,
        rect: {{ x: rect.x, y: rect.y, width: rect.width, height: rect.height }}
    }};
}})()
"""


class ActionabilityTimeout(TimeoutError):
    pass


@dataclass
class ElementState:
    found: bool
    visible: bool = False
    enabled: bool = False
    hit_target: bool = False
    rect: dict | None = None

    @property #found + visible + enabled + hit_target
    def actionable(self) -> bool:
        return self.found and self.visible and self.enabled and self.hit_target

"""creates a json from the current state of an element, including whether it is found, 
visible, enabled, hit-testable, and its bounding rect."""
async def _read_state(conn: CDPConnection, session_id: str, selector: str) -> ElementState:
    result = await conn.send(
        "Runtime.evaluate",
        {"expression": _CHECK_SCRIPT.format(selector=selector), "returnByValue": True},
        session_id=session_id,
    )
    value = result["result"]["value"]
    if not value.get("found"):
        return ElementState(found=False)
    return ElementState(
        found=True,
        visible=value["visible"],
        enabled=value["enabled"],
        hit_target=value["hitTarget"],
        rect=value["rect"],
    )


async def wait_for_actionable(
    conn: CDPConnection,
    session_id: str,
    selector: str,
    timeout: float = 5.0,
    poll_interval: float = 0.1,
) -> ElementState:
    """Polls the DOM until the element is visible, stable (same rect across
    two consecutive polls), enabled and hit-testable at its own center point."""
    deadline = time.monotonic() + timeout
    previous_rect: dict | None = None

    while time.monotonic() < deadline:
        state = await _read_state(conn, session_id, selector)

        if state.actionable and state.rect == previous_rect:
            return state

        previous_rect = state.rect
        await asyncio.sleep(poll_interval)

    raise ActionabilityTimeout(f"Element '{selector}' not actionable after {timeout}s")

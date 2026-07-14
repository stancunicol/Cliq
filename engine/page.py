from __future__ import annotations

import asyncio

from engine.actionability import wait_for_actionable
from engine.cdp_connection import CDPConnection


class Page:
    """A single tab, attached to one CDP session within a BrowserContext."""

    def __init__(self, conn: CDPConnection, session_id: str):
        self._conn = conn
        self._session_id = session_id

    @classmethod
    async def create(cls, conn: CDPConnection, browser_context_id: str, url: str = "about:blank") -> "Page":
        target = await conn.send(
            "Target.createTarget", {"url": url, "browserContextId": browser_context_id}
        )
        attach = await conn.send(
            "Target.attachToTarget", {"targetId": target["targetId"], "flatten": True}
        )
        session_id = attach["sessionId"]
        await conn.send("Page.enable", session_id=session_id)
        return cls(conn, session_id)

    async def navigate(self, url: str, timeout: float = 10.0) -> None:
        load_event = asyncio.Event()

        def on_event(data: dict) -> None:
            if data.get("method") == "Page.loadEventFired" and data.get("sessionId") == self._session_id:
                load_event.set()

        self._conn.on_event(on_event)
        await self._conn.send("Page.navigate", {"url": url}, session_id=self._session_id)
        await asyncio.wait_for(load_event.wait(), timeout=timeout)

    async def click(self, selector: str, timeout: float = 5.0) -> None:
        state = await wait_for_actionable(self._conn, self._session_id, selector, timeout=timeout)
        cx = state.rect["x"] + state.rect["width"] / 2
        cy = state.rect["y"] + state.rect["height"] / 2

        common = {"x": cx, "y": cy, "button": "left", "clickCount": 1}
        await self._conn.send(
            "Input.dispatchMouseEvent", {"type": "mousePressed", **common}, session_id=self._session_id
        )
        # a real click has a non-zero press duration; some sites don't
        # register a press+release sent back-to-back as a legitimate click
        await asyncio.sleep(0.05)
        await self._conn.send(
            "Input.dispatchMouseEvent", {"type": "mouseReleased", **common}, session_id=self._session_id
        )

    async def fill(self, selector: str, text: str, timeout: float = 5.0) -> None:
        await wait_for_actionable(self._conn, self._session_id, selector, timeout=timeout)

        script = f"""
        (() => {{
            const el = document.querySelector({selector!r});
            el.focus();

            // React (and similar frameworks) override the instance-level
            // "value" setter to track changes. Setting el.value directly
            // updates the DOM but not the framework's internal state, so we
            // call the native prototype setter instead, then fire the event
            // the framework actually listens for.
            const proto = Object.getPrototypeOf(el);
            const nativeSetter = Object.getOwnPropertyDescriptor(proto, 'value').set;
            nativeSetter.call(el, {text!r});

            el.dispatchEvent(new Event('input', {{ bubbles: true }}));
            el.dispatchEvent(new Event('change', {{ bubbles: true }}));
        }})()
        """
        await self._conn.send("Runtime.evaluate", {"expression": script}, session_id=self._session_id)

    async def select_option(self, selector: str, value: str, timeout: float = 5.0) -> None:
        await wait_for_actionable(self._conn, self._session_id, selector, timeout=timeout)

        script = f"""
        (() => {{
            const el = document.querySelector({selector!r});
            el.focus();

            const proto = Object.getPrototypeOf(el);
            const nativeSetter = Object.getOwnPropertyDescriptor(proto, 'value').set;
            nativeSetter.call(el, {value!r});

            el.dispatchEvent(new Event('input', {{ bubbles: true }}));
            el.dispatchEvent(new Event('change', {{ bubbles: true }}));
        }})()
        """
        await self._conn.send("Runtime.evaluate", {"expression": script}, session_id=self._session_id)

    async def set_checked(self, selector: str, checked: bool, timeout: float = 5.0) -> None:
        await wait_for_actionable(self._conn, self._session_id, selector, timeout=timeout)

        script = f"""
        (() => {{
            const el = document.querySelector({selector!r});
            el.focus();

            const proto = Object.getPrototypeOf(el);
            const nativeSetter = Object.getOwnPropertyDescriptor(proto, 'checked').set;
            nativeSetter.call(el, {"true" if checked else "false"});

            el.dispatchEvent(new Event('input', {{ bubbles: true }}));
            el.dispatchEvent(new Event('change', {{ bubbles: true }}));
        }})()
        """
        await self._conn.send("Runtime.evaluate", {"expression": script}, session_id=self._session_id)

    async def evaluate(self, expression: str):
        result = await self._conn.send(
            "Runtime.evaluate",
            {"expression": expression, "returnByValue": True},
            session_id=self._session_id,
        )
        return result["result"].get("value")

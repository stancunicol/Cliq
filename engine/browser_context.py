from __future__ import annotations

from engine.cdp_connection import CDPConnection
from engine.page import Page


class BrowserContext:
    """An isolated context (own cookies/storage/cache) within one browser process."""

    def __init__(self, conn: CDPConnection, browser_context_id: str):
        self._conn = conn
        self.browser_context_id = browser_context_id

    @classmethod
    async def create(cls, conn: CDPConnection) -> "BrowserContext":
        result = await conn.send("Target.createBrowserContext")
        return cls(conn, result["browserContextId"])

    async def new_page(self, url: str = "about:blank") -> Page:
        return await Page.create(self._conn, self.browser_context_id, url)

    async def close(self) -> None:
        await self._conn.send(
            "Target.disposeBrowserContext", {"browserContextId": self.browser_context_id}
        )

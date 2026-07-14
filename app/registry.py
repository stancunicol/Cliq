from __future__ import annotations

import uuid
from dataclasses import dataclass, field

from fastapi import WebSocket

from engine.browser_context import BrowserContext
from engine.cdp_connection import CDPConnection
from engine.page import Page
from engine.process_manager import BrowserProcess

"""groupes togheter the info about session, cdp conn, and the list of websockets connected to the session, 
so that we can broadcast events to all websockets in the session."""
@dataclass
class SessionEntry:
    id: str
    browser: BrowserProcess
    conn: CDPConnection
    websockets: list[WebSocket] = field(default_factory=list)

    async def broadcast(self, event: dict) -> None:
        for ws in list(self.websockets):
            try:
                await ws.send_json(event)
            except Exception:
                self.websockets.remove(ws) #if the client disconnects, remove the websocket


@dataclass
class ContextEntry:
    id: str
    session_id: str
    context: BrowserContext


@dataclass
class PageEntry:
    id: str
    context_id: str
    session_id: str
    page: Page


class Registry:
    def __init__(self) -> None:
        self.sessions: dict[str, SessionEntry] = {}
        self.contexts: dict[str, ContextEntry] = {}
        self.pages: dict[str, PageEntry] = {}

    def new_id(self) -> str:
        return uuid.uuid4().hex

    def pop_session_cascade(self, session_id: str) -> SessionEntry | None:
        """Removes a session along with every context/page that belongs to
        it, so closing a session never leaves orphaned registry entries
        pointing at a browser process that's about to be terminated."""
        session = self.sessions.pop(session_id, None)
        if session is None:
            return None

        for context_id in [cid for cid, entry in self.contexts.items() if entry.session_id == session_id]:
            self.contexts.pop(context_id, None)

        for page_id in [pid for pid, entry in self.pages.items() if entry.session_id == session_id]:
            self.pages.pop(page_id, None)

        return session


registry = Registry() #singleton instance of the registry, used to store all sessions, contexts, and pages

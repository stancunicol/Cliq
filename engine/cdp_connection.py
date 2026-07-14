from __future__ import annotations

import asyncio
import itertools
import json
import urllib.request
from typing import Any, Callable

import websockets


def get_browser_ws_endpoint(http_endpoint: str) -> str:
    with urllib.request.urlopen(f"{http_endpoint}/json/version", timeout=5) as resp:
        data = json.load(resp)
    return data["webSocketDebuggerUrl"]


class CDPConnection:
    """One WebSocket per browser process; sessions multiplex over it (flatten mode)."""

    def __init__(self, ws_url: str):
        self._ws_url = ws_url
        self._ws: websockets.WebSocketClientProtocol | None = None
        self._id_counter = itertools.count(1) #id for each message sent to the browser
        self._pending: dict[int, asyncio.Future] = {} #maps message id to future for the response
        self._event_handlers: list[Callable[[dict], None]] = [] #event handlers for incoming messages from the browser
        self._reader_task: asyncio.Task | None = None #task for reading incoming messages

    """opens a WebSocket connection to the browser's DevTools endpoint and starts 
    a read loop for incoming messages."""
    @classmethod
    async def connect(cls, http_endpoint: str) -> "CDPConnection":
        ws_url = get_browser_ws_endpoint(http_endpoint)
        conn = cls(ws_url)
        conn._ws = await websockets.connect(ws_url, max_size=None) #no limit on message size, allows large messages from the browser (big DOMs, screenshots, etc.)
        conn._reader_task = asyncio.create_task(conn._read_loop())
        return conn

    """sends a command to the browser and waits for the response.
    If session_id is provided, the command is sent to that session (flatten mode).
    one connection can have multiple isolated sessions, each with its own state."""
    async def send(
        self, method: str, params: dict | None = None, session_id: str | None = None
    ) -> dict:
        if self._ws is None:
            raise RuntimeError("Not connected")

        msg_id = next(self._id_counter)
        message: dict[str, Any] = {"id": msg_id, "method": method, "params": params or {}}
        if session_id:
            message["sessionId"] = session_id

        future: asyncio.Future = asyncio.get_event_loop().create_future()
        self._pending[msg_id] = future

        await self._ws.send(json.dumps(message))
        return await future

    def on_event(self, handler: Callable[[dict], None]) -> None:
        self._event_handlers.append(handler)

    """if id exists -> message response, set the result of the future. 
    If no id -> event, call all event handlers."""
    async def _read_loop(self) -> None:
        assert self._ws is not None
        async for raw in self._ws:
            data = json.loads(raw)
            if "id" in data:
                future = self._pending.pop(data["id"], None)
                if future and not future.done():
                    if "error" in data:
                        future.set_exception(RuntimeError(data["error"]))
                    else:
                        future.set_result(data.get("result", {}))
            else:
                for handler in self._event_handlers:
                    handler(data)

    """closes the WebSocket connection and cancels the read loop task."""
    async def close(self) -> None:
        if self._reader_task:
            self._reader_task.cancel()
        if self._ws:
            await self._ws.close()

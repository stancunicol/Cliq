from __future__ import annotations

import asyncio

from fastapi import APIRouter, HTTPException, WebSocket, WebSocketDisconnect

from app.registry import SessionEntry, registry
from app.schemas import CreateSessionRequest, SessionResponse, SessionSummary
from engine.cdp_connection import CDPConnection
from engine.process_manager import launch_browser

router = APIRouter(prefix="/sessions", tags=["sessions"])


def _to_summary(entry: SessionEntry) -> SessionSummary:
    return SessionSummary(session_id=entry.id, devtools_port=entry.browser.devtools_port)


@router.post("", response_model=SessionResponse)
async def create_session(body: CreateSessionRequest) -> SessionResponse:
    # launch_browser() blocks (up to ~10s polling for the DevTools port file);
    # run it off the event loop thread so it doesn't stall other requests.
    browser = await asyncio.to_thread(launch_browser, headless=body.headless)
    conn = await CDPConnection.connect(browser.http_endpoint)

    session_id = registry.new_id()
    registry.sessions[session_id] = SessionEntry(id=session_id, browser=browser, conn=conn)
    return SessionResponse(session_id=session_id)


@router.get("", response_model=list[SessionSummary])
async def list_sessions() -> list[SessionSummary]:
    return [_to_summary(entry) for entry in registry.sessions.values()]


@router.get("/{session_id}", response_model=SessionSummary)
async def get_session(session_id: str) -> SessionSummary:
    entry = registry.sessions.get(session_id)
    if entry is None:
        raise HTTPException(status_code=404, detail="Session not found")
    return _to_summary(entry)


@router.delete("/{session_id}", status_code=204, response_model=None)
async def close_session(session_id: str) -> None:
    session = registry.pop_session_cascade(session_id)
    if session is None:
        raise HTTPException(status_code=404, detail="Session not found")

    await session.conn.close()
    session.browser.terminate()


@router.websocket("/{session_id}/ws")
async def session_events(websocket: WebSocket, session_id: str) -> None:
    session = registry.sessions.get(session_id)
    if session is None:
        await websocket.close(code=4404)
        return

    await websocket.accept()
    session.websockets.append(websocket)
    try:
        while True:
            await websocket.receive_text()
    except WebSocketDisconnect:
        session.websockets.remove(websocket)

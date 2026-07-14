from __future__ import annotations

from fastapi import APIRouter, HTTPException

from app.registry import ContextEntry, registry
from app.schemas import ContextResponse, ContextSummary
from engine.browser_context import BrowserContext

router = APIRouter(prefix="/sessions/{session_id}/contexts", tags=["contexts"])
detail_router = APIRouter(prefix="/contexts/{context_id}", tags=["contexts"])


def _to_summary(entry: ContextEntry) -> ContextSummary:
    return ContextSummary(context_id=entry.id, session_id=entry.session_id)


@router.post("", response_model=ContextResponse)
async def create_context(session_id: str) -> ContextResponse:
    session = registry.sessions.get(session_id)
    if session is None:
        raise HTTPException(status_code=404, detail="Session not found")

    context = await BrowserContext.create(session.conn)
    context_id = registry.new_id()
    registry.contexts[context_id] = ContextEntry(id=context_id, session_id=session_id, context=context)
    return ContextResponse(context_id=context_id)


@router.get("", response_model=list[ContextSummary])
async def list_contexts(session_id: str) -> list[ContextSummary]:
    return [_to_summary(entry) for entry in registry.contexts.values() if entry.session_id == session_id]


@router.delete("/{context_id}", status_code=204, response_model=None)
async def close_context(session_id: str, context_id: str) -> None:
    entry = registry.contexts.pop(context_id, None)
    if entry is None:
        raise HTTPException(status_code=404, detail="Context not found")
    await entry.context.close()


@detail_router.get("", response_model=ContextSummary)
async def get_context(context_id: str) -> ContextSummary:
    entry = registry.contexts.get(context_id)
    if entry is None:
        raise HTTPException(status_code=404, detail="Context not found")
    return _to_summary(entry)

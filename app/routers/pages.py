from __future__ import annotations

from fastapi import APIRouter, HTTPException

from app.registry import PageEntry, SessionEntry, registry
from app.schemas import (
    ActionRequest,
    EvaluateRequest,
    EvaluateResponse,
    GeneratePageObjectRequest,
    GeneratePageObjectResponse,
    NavigateRequest,
    PageResponse,
    PageSummary,
)
from app.page_object_storage import save_page_object
from engine.page_object_generator import extract_elements, generate_page_object

router = APIRouter(prefix="/contexts/{context_id}/pages", tags=["pages"]) #create
action_router = APIRouter(prefix="/pages/{page_id}", tags=["pages"]) #action


def _to_summary(entry: PageEntry) -> PageSummary:
    return PageSummary(page_id=entry.id, context_id=entry.context_id, session_id=entry.session_id)


@router.post("", response_model=PageResponse)
async def create_page(context_id: str) -> PageResponse:
    context_entry = registry.contexts.get(context_id)
    if context_entry is None:
        raise HTTPException(status_code=404, detail="Context not found")

    page = await context_entry.context.new_page()
    page_id = registry.new_id()
    registry.pages[page_id] = PageEntry(
        id=page_id, context_id=context_id, session_id=context_entry.session_id, page=page
    )
    return PageResponse(page_id=page_id)


@router.get("", response_model=list[PageSummary])
async def list_pages(context_id: str) -> list[PageSummary]:
    return [_to_summary(entry) for entry in registry.pages.values() if entry.context_id == context_id]


def _get_page(page_id: str) -> PageEntry:
    entry = registry.pages.get(page_id)
    if entry is None:
        raise HTTPException(status_code=404, detail="Page not found")
    return entry


def _get_session(entry: PageEntry) -> SessionEntry:
    return registry.sessions[entry.session_id]


@action_router.get("", response_model=PageSummary)
async def get_page(page_id: str) -> PageSummary:
    return _to_summary(_get_page(page_id))


@action_router.post("/navigate", status_code=204, response_model=None)
async def navigate(page_id: str, body: NavigateRequest) -> None:
    entry = _get_page(page_id)
    session = _get_session(entry)

    await session.broadcast({"type": "action_start", "action": "navigate", "url": body.url})
    await entry.page.navigate(body.url)
    await session.broadcast({"type": "action_result", "action": "navigate", "status": "passed"})


@action_router.post("/actions", status_code=204, response_model=None)
async def act(page_id: str, body: ActionRequest) -> None:
    entry = _get_page(page_id)
    session = _get_session(entry)

    await session.broadcast({"type": "action_start", "action": body.action, "selector": body.selector})
    try:
        if body.action == "click":
            await entry.page.click(body.selector)
        elif body.action == "select":
            await entry.page.select_option(body.selector, body.value or "")
        elif body.action == "check":
            await entry.page.set_checked(body.selector, body.checked if body.checked is not None else True)
        else:
            await entry.page.fill(body.selector, body.value or "")
    except Exception as exc:
        await session.broadcast(
            {"type": "action_result", "action": body.action, "status": "failed", "error": str(exc)}
        )
        raise #for the client to see the error in the response
    await session.broadcast({"type": "action_result", "action": body.action, "status": "passed"})


@action_router.post("/evaluate", response_model=EvaluateResponse)
async def evaluate(page_id: str, body: EvaluateRequest) -> EvaluateResponse:
    entry = _get_page(page_id)
    value = await entry.page.evaluate(body.expression)
    return EvaluateResponse(value=value)


@action_router.post("/generate-page-object", response_model=GeneratePageObjectResponse)
async def generate_page_object_endpoint(
    page_id: str, body: GeneratePageObjectRequest
) -> GeneratePageObjectResponse:
    entry = _get_page(page_id)
    elements = await extract_elements(entry.page)
    code = generate_page_object(body.class_name, elements)
    await save_page_object(body.class_name, code)
    return GeneratePageObjectResponse(code=code)

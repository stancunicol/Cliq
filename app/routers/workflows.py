from __future__ import annotations

from fastapi import APIRouter, HTTPException

from app.registry import ContextEntry, PageEntry, SessionEntry, registry
from app.schemas import (
    AssertStep,
    DiscoveredAction,
    InteractStep,
    RunFlowRequest,
    RunFlowResponse,
    SimulatedActionResult,
    SimulateRequest,
    SimulateResponse,
)
from app.page_object_storage import save_page_object
from engine.browser_context import BrowserContext
from engine.page import Page
from engine.page_object_generator import ElementDescriptor, extract_elements, generate_page_object

router = APIRouter(prefix="/workflows", tags=["workflows"])
simulate_router = APIRouter(prefix="/pages/{page_id}", tags=["workflows"])


@router.post("/run", response_model=RunFlowResponse)
async def run_flow(body: RunFlowRequest) -> RunFlowResponse:
    """Discovery only: navigates and reports what can be done. Executes nothing."""
    session = registry.sessions.get(body.session_id)
    if session is None:
        raise HTTPException(status_code=404, detail="Session not found")

    context = await BrowserContext.create(session.conn)
    context_id = registry.new_id()
    registry.contexts[context_id] = ContextEntry(id=context_id, session_id=body.session_id, context=context)

    page = await context.new_page()
    page_id = registry.new_id()
    registry.pages[page_id] = PageEntry(
        id=page_id, context_id=context_id, session_id=body.session_id, page=page
    )

    await session.broadcast({"type": "action_start", "action": "navigate", "url": body.url})
    await page.navigate(body.url)
    await session.broadcast({"type": "action_result", "action": "navigate", "status": "passed"})

    elements = await extract_elements(page)
    available_actions = [
        DiscoveredAction(name=el.suggested_name, kind=el.kind, selector=el.selector) for el in elements
    ]

    generated_code = None
    if body.generate_page_object:
        generated_code = generate_page_object(body.class_name, elements)
        await save_page_object(body.class_name, generated_code)

    return RunFlowResponse(
        session_id=body.session_id,
        context_id=context_id,
        page_id=page_id,
        generated_code=generated_code,
        available_actions=available_actions,
    )


async def _run_assert_step(session: SessionEntry, page: Page, step: AssertStep) -> SimulatedActionResult:
    actual = await page.evaluate(step.expression)
    passed = actual == step.equals

    await session.broadcast({"type": "action_start", "action": "assert", "expression": step.expression})
    await session.broadcast(
        {"type": "action_result", "action": "assert", "status": "passed" if passed else "failed"}
    )
    return SimulatedActionResult(
        type="assert",
        label=step.expression,
        status="passed" if passed else "failed",
        error=None if passed else f"expected {step.equals!r}, got {actual!r}",
        actual=actual,
    )


async def _run_interact_step(
    session: SessionEntry, page: Page, step: InteractStep, descriptors: dict[str, ElementDescriptor]
) -> SimulatedActionResult:
    descriptor = descriptors.get(step.name)
    if descriptor is None:
        return SimulatedActionResult(
            type="interact",
            label=step.name,
            status="failed",
            error=f"No element named '{step.name}' found on the current page",
        )

    await session.broadcast({"type": "action_start", "action": descriptor.kind, "selector": descriptor.selector})
    try:
        if descriptor.kind == "click":
            await page.click(descriptor.selector)
        elif descriptor.kind == "select":
            await page.select_option(descriptor.selector, step.value or "")
        elif descriptor.kind == "check":
            await page.set_checked(descriptor.selector, step.checked if step.checked is not None else True)
        else:
            await page.fill(descriptor.selector, step.value or "")
    except Exception as exc:
        await session.broadcast(
            {"type": "action_result", "action": descriptor.kind, "status": "failed", "error": str(exc)}
        )
        return SimulatedActionResult(type="interact", label=step.name, status="failed", error=str(exc))

    await session.broadcast({"type": "action_result", "action": descriptor.kind, "status": "passed"})
    return SimulatedActionResult(type="interact", label=step.name, status="passed")


@simulate_router.post("/simulate", response_model=SimulateResponse)
async def simulate(page_id: str, body: SimulateRequest) -> SimulateResponse:
    """Executes actions referenced by the names discovered via /workflows/run,
    re-resolving each selector from the current page so it can never drift
    from what actually exists on the DOM right now."""
    entry = registry.pages.get(page_id)
    if entry is None:
        raise HTTPException(status_code=404, detail="Page not found")

    session = registry.sessions[entry.session_id]
    page = entry.page
    descriptors = {el.suggested_name: el for el in await extract_elements(page)}

    results: list[SimulatedActionResult] = []
    for step in body.actions:
        if step.type == "assert":
            result = await _run_assert_step(session, page, step)
        else:
            result = await _run_interact_step(session, page, step, descriptors)

        results.append(result)
        if result.status == "failed":
            break

    if body.close_session:
        registry.pop_session_cascade(entry.session_id)
        await session.conn.close()
        session.browser.terminate()

    return SimulateResponse(action_results=results)

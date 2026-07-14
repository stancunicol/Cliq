from __future__ import annotations

from fastapi import FastAPI

from app.request_logging import RequestLoggingMiddleware
from app.routers import contexts, pages, sessions, workflows

app = FastAPI(title="UI Testing Engine")
app.add_middleware(RequestLoggingMiddleware)

app.include_router(sessions.router)
app.include_router(contexts.router)
app.include_router(contexts.detail_router)
app.include_router(pages.router)
app.include_router(pages.action_router)
app.include_router(workflows.router)
app.include_router(workflows.simulate_router)


@app.get("/health")
async def health() -> dict:
    return {"status": "ok"}

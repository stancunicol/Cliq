from __future__ import annotations

import asyncio
import json
import time
from pathlib import Path
from typing import Any

from starlette.middleware.base import BaseHTTPMiddleware
from starlette.requests import Request
from starlette.responses import Response

_LOG_PATH = Path("logs/requests.jsonl")


def _to_json(raw: bytes) -> Any:
    if not raw:
        return None
    try:
        return json.loads(raw)
    except (UnicodeDecodeError, json.JSONDecodeError):
        return raw.decode("utf-8", errors="replace")


def _append(entry: dict) -> None:
    _LOG_PATH.parent.mkdir(exist_ok=True)
    with _LOG_PATH.open("a", encoding="utf-8") as f:
        f.write(json.dumps(entry) + "\n")


class RequestLoggingMiddleware(BaseHTTPMiddleware):
    """Logs every HTTP request/response (method, path, bodies, status, timing)
    as one JSON line per call in logs/requests.jsonl. WebSocket connections
    bypass this entirely (Starlette only invokes dispatch() for HTTP)."""

    async def dispatch(self, request: Request, call_next):
        request_body = await request.body()

        started_at = time.time()
        start = time.monotonic()
        response = await call_next(request)
        duration_ms = (time.monotonic() - start) * 1000

        response_body = b""
        async for chunk in response.body_iterator:
            response_body += chunk

        await asyncio.to_thread(
            _append,
            {
                "timestamp": started_at,
                "method": request.method,
                "path": request.url.path,
                "query": str(request.url.query) or None,
                "request_body": _to_json(request_body),
                "status_code": response.status_code,
                "response_body": _to_json(response_body),
                "duration_ms": round(duration_ms, 2),
            },
        )

        return Response(
            content=response_body,
            status_code=response.status_code,
            headers=dict(response.headers),
            media_type=response.media_type,
        )

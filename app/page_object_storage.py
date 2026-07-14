from __future__ import annotations

import asyncio
from datetime import datetime
from pathlib import Path

_OUTPUT_DIR = Path("generated_pages")


def _write(class_name: str, code: str) -> Path:
    _OUTPUT_DIR.mkdir(exist_ok=True)

    # timestamps sort lexicographically the same as chronologically, so the
    # last match is the most recently saved version for this class name
    existing = sorted(_OUTPUT_DIR.glob(f"{class_name}_*.py"))
    if existing and existing[-1].read_text(encoding="utf-8") == code:
        return existing[-1]

    # content actually changed (or this is the first time) -> new snapshot
    timestamp = datetime.now().strftime("%Y%m%d_%H%M%S_%f")
    path = _OUTPUT_DIR / f"{class_name}_{timestamp}.py"
    path.write_text(code, encoding="utf-8")
    return path


async def save_page_object(class_name: str, code: str) -> Path:
    return await asyncio.to_thread(_write, class_name, code)

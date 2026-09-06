from __future__ import annotations

import json
import logging
from typing import Any

from fastapi import FastAPI, Request
from fastapi.exceptions import RequestValidationError
from fastapi.responses import JSONResponse

logger = logging.getLogger(__name__)


def _sanitize_errors(errors: list[dict[str, Any]]) -> list[dict[str, Any]]:
    sanitized: list[dict[str, Any]] = []
    for error in errors:
        item: dict[str, Any] = {}
        for key, value in error.items():
            item[key] = str(value) if isinstance(value, Exception) else value
        sanitized.append(item)
    return sanitized


def register_exception_handlers(app: FastAPI) -> None:
    @app.exception_handler(RequestValidationError)
    async def validation_exception_handler(request: Request, exc: RequestValidationError) -> JSONResponse:
        logger.error(json.dumps({"event": "validation_error", "path": str(request.url.path)}))
        return JSONResponse(status_code=422, content={"detail": _sanitize_errors(exc.errors())})

    @app.exception_handler(Exception)
    async def generic_exception_handler(request: Request, exc: Exception) -> JSONResponse:
        logger.error("Unexpected error: %s", str(exc), exc_info=True)
        return JSONResponse(status_code=500, content={"detail": "Internal Error"})

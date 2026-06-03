from __future__ import annotations

import json
import logging
import os
from typing import Any

from fastapi import FastAPI, Request
from fastapi.exceptions import RequestValidationError
from fastapi.responses import JSONResponse

logger = logging.getLogger(__name__)
logger.setLevel(os.environ.get("LOG_LEVEL", "INFO"))


def _sanitize_errors(errors: list[dict[str, Any]]) -> list[dict[str, Any]]:
    sanitized: list[dict[str, Any]] = []
    for error in errors:
        clean_error: dict[str, Any] = {}
        for key, value in error.items():
            clean_error[key] = str(value) if isinstance(value, Exception) else value
        sanitized.append(clean_error)
    return sanitized


def register_exception_handlers(app: FastAPI) -> None:
    @app.exception_handler(RequestValidationError)
    async def request_validation_exception_handler(request: Request, exc: RequestValidationError) -> JSONResponse:
        logger.error(json.dumps({"event": "request_validation_error", "path": request.url.path}))
        return JSONResponse(status_code=422, content={"detail": _sanitize_errors(exc.errors())})

    @app.exception_handler(Exception)
    async def generic_exception_handler(request: Request, exc: Exception) -> JSONResponse:
        logger.error(json.dumps({"event": "unhandled_exception", "path": request.url.path, "error": str(exc)}))
        return JSONResponse(status_code=500, content={"detail": "Internal Server Error"})

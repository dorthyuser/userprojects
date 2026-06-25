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
            if isinstance(value, Exception):
                item[key] = str(value)
            elif isinstance(value, dict):
                item[key] = json.loads(json.dumps(value, default=str))
            elif isinstance(value, list):
                item[key] = [str(v) if isinstance(v, Exception) else v for v in value]
            else:
                item[key] = value
        sanitized.append(item)
    return sanitized


async def request_validation_exception_handler(request: Request, exc: RequestValidationError) -> JSONResponse:
    logger.error("Validation error: %s", str(exc))
    return JSONResponse(status_code=422, content={"detail": "Validation Error", "errors": _sanitize_errors(exc.errors())})


async def generic_exception_handler(request: Request, exc: Exception) -> JSONResponse:
    logger.error("Unhandled error: %s", str(exc), exc_info=True)
    return JSONResponse(status_code=500, content={"detail": "Internal Error"})


def register_exception_handlers(app: FastAPI) -> None:
    app.add_exception_handler(RequestValidationError, request_validation_exception_handler)
    app.add_exception_handler(Exception, generic_exception_handler)

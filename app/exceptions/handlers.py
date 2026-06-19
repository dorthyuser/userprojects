from __future__ import annotations

import json
import logging
from collections.abc import Iterable
from typing import Any

from fastapi import FastAPI, Request
from fastapi.exceptions import RequestValidationError
from fastapi.responses import JSONResponse

logger = logging.getLogger(__name__)


def _make_json_safe(value: Any) -> Any:
    if isinstance(value, dict):
        return {str(key): _make_json_safe(item) for key, item in value.items()}
    if isinstance(value, list):
        return [_make_json_safe(item) for item in value]
    if isinstance(value, tuple):
        return [_make_json_safe(item) for item in value]
    if isinstance(value, Exception):
        return str(value)
    return value


def _validation_payload(exc: RequestValidationError) -> dict[str, Any]:
    return {"detail": _make_json_safe(exc.errors())}


async def validation_exception_handler(request: Request, exc: RequestValidationError) -> JSONResponse:
    logger.error(json.dumps({"message": "Validation error", "path": str(request.url.path)}))
    return JSONResponse(status_code=422, content={"error": {"code": "VALIDATION_ERROR", "message": "Validation Error"}})


async def generic_exception_handler(request: Request, exc: Exception) -> JSONResponse:
    logger.error(json.dumps({"message": "Unhandled exception", "path": str(request.url.path), "error": str(exc)}), exc_info=True)
    return JSONResponse(status_code=500, content={"error": {"code": "INTERNAL_ERROR", "message": "Internal Error"}})


def register_exception_handlers(app: FastAPI) -> None:
    app.add_exception_handler(RequestValidationError, validation_exception_handler)
    app.add_exception_handler(Exception, generic_exception_handler)

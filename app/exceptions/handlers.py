from __future__ import annotations

import json
import logging
from typing import Any

from fastapi import FastAPI, HTTPException, Request
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
                item[key] = {k: str(v) if isinstance(v, Exception) else v for k, v in value.items()}
            else:
                item[key] = value
        sanitized.append(item)
    return sanitized


def request_validation_exception_handler(request: Request, exc: RequestValidationError) -> JSONResponse:
    logger.info(json.dumps({"event": "validation_error", "path": request.url.path}))
    return JSONResponse(status_code=422, content={"error": {"code": "VALIDATION_ERROR", "message": "Validation Error", "details": _sanitize_errors(exc.errors())}})


def http_exception_handler(request: Request, exc: HTTPException) -> JSONResponse:
    return JSONResponse(status_code=exc.status_code, content={"error": {"code": str(exc.detail), "message": str(exc.detail)}})


def generic_exception_handler(request: Request, exc: Exception) -> JSONResponse:
    logger.error("Unexpected error: %s", str(exc), exc_info=True)
    return JSONResponse(status_code=500, content={"error": {"code": "INTERNAL_ERROR", "message": "Internal Error"}})


def register_exception_handlers(app: FastAPI) -> None:
    app.add_exception_handler(RequestValidationError, request_validation_exception_handler)
    app.add_exception_handler(HTTPException, http_exception_handler)
    app.add_exception_handler(Exception, generic_exception_handler)

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
            elif isinstance(value, list):
                item[key] = [str(v) if isinstance(v, Exception) else v for v in value]
            elif isinstance(value, dict):
                item[key] = {
                    sub_key: (str(sub_value) if isinstance(sub_value, Exception) else sub_value)
                    for sub_key, sub_value in value.items()
                }
            else:
                item[key] = value
        sanitized.append(item)
    return sanitized


def register_exception_handlers(app: FastAPI) -> None:
    @app.exception_handler(RequestValidationError)
    async def request_validation_exception_handler(request: Request, exc: RequestValidationError) -> JSONResponse:
        logger.info(json.dumps({"message": "validation_error", "path": str(request.url.path)}))
        return JSONResponse(status_code=422, content={"detail": "Validation Error", "errors": _sanitize_errors(exc.errors())})

    @app.exception_handler(HTTPException)
    async def http_exception_handler(request: Request, exc: HTTPException) -> JSONResponse:
        logger.info(json.dumps({"message": "http_exception", "path": str(request.url.path), "status_code": exc.status_code}))
        detail = exc.detail if isinstance(exc.detail, str) else "Internal Error"
        return JSONResponse(status_code=exc.status_code, content={"detail": detail})

    @app.exception_handler(Exception)
    async def generic_exception_handler(request: Request, exc: Exception) -> JSONResponse:
        logger.error(json.dumps({"message": "unhandled_exception", "path": str(request.url.path), "error": str(exc)}), exc_info=True)
        return JSONResponse(status_code=500, content={"detail": "Internal Error"})

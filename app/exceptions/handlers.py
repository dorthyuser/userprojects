import json
import logging
from collections.abc import Callable
from typing import Any

from fastapi import FastAPI, Request
from fastapi.exceptions import RequestValidationError
from fastapi.responses import JSONResponse

logger = logging.getLogger(__name__)


def _sanitize_errors(errors: list[dict[str, Any]]) -> list[dict[str, Any]]:
    sanitized: list[dict[str, Any]] = []
    for error in errors:
        clean_error: dict[str, Any] = {}
        for key, value in error.items():
            if isinstance(value, Exception):
                clean_error[key] = str(value)
            elif isinstance(value, dict):
                clean_error[key] = {
                    inner_key: str(inner_value) if isinstance(inner_value, Exception) else inner_value
                    for inner_key, inner_value in value.items()
                }
            else:
                clean_error[key] = value
        sanitized.append(clean_error)
    return sanitized


def register_exception_handlers(app: FastAPI) -> None:
    @app.exception_handler(RequestValidationError)
    async def request_validation_exception_handler(request: Request, exc: RequestValidationError) -> JSONResponse:
        logger.error(json.dumps({"event": "validation_error", "path": str(request.url.path)}))
        return JSONResponse(status_code=422, content={"detail": "Validation Error", "errors": _sanitize_errors(exc.errors())})

    @app.exception_handler(Exception)
    async def generic_exception_handler(request: Request, exc: Exception) -> JSONResponse:
        logger.error("Unhandled exception: %s", str(exc), exc_info=True)
        return JSONResponse(status_code=500, content={"detail": "Internal Error"})

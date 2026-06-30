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
        clean_error: dict[str, Any] = {}
        for key, value in error.items():
            if isinstance(value, Exception):
                clean_error[key] = str(value)
            elif isinstance(value, dict):
                clean_error[key] = {k: str(v) if isinstance(v, Exception) else v for k, v in value.items()}
            elif isinstance(value, list):
                clean_error[key] = [str(item) if isinstance(item, Exception) else item for item in value]
            else:
                clean_error[key] = value
        sanitized.append(clean_error)
    return sanitized


async def request_validation_exception_handler(request: Request, exc: RequestValidationError) -> JSONResponse:
    logger.error(json.dumps({"event": "request_validation_error", "path": str(request.url.path), "message": str(exc)}))
    return JSONResponse(status_code=422, content={"detail": "Validation Error", "errors": _sanitize_errors(exc.errors())})


async def http_exception_handler(request: Request, exc: HTTPException) -> JSONResponse:
    logger.error(json.dumps({"event": "http_exception", "path": str(request.url.path), "message": str(exc.detail)}))
    return JSONResponse(status_code=exc.status_code, content={"detail": exc.detail})


async def generic_exception_handler(request: Request, exc: Exception) -> JSONResponse:
    logger.error(json.dumps({"event": "unhandled_exception", "path": str(request.url.path), "message": str(exc)}), exc_info=True)
    return JSONResponse(status_code=500, content={"detail": "Internal Error"})


def register_exception_handlers(app: FastAPI) -> None:
    app.add_exception_handler(RequestValidationError, request_validation_exception_handler)
    app.add_exception_handler(HTTPException, http_exception_handler)
    app.add_exception_handler(Exception, generic_exception_handler)

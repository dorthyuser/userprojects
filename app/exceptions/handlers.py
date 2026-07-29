import json
import logging

from fastapi import FastAPI, HTTPException, Request
from fastapi.exceptions import RequestValidationError
from fastapi.responses import JSONResponse

logger = logging.getLogger(__name__)


def _sanitize_errors(errors: list[dict]) -> list[dict]:
    sanitized: list[dict] = []
    for error in errors:
        item: dict = {}
        for key, value in error.items():
            item[key] = str(value) if isinstance(value, Exception) else value
        sanitized.append(item)
    return sanitized


async def request_validation_exception_handler(request: Request, exc: RequestValidationError) -> JSONResponse:
    logger.error(json.dumps({"step": "VALIDATION", "outcome": "FAILURE", "error": str(exc)}))
    return JSONResponse(status_code=422, content={"detail": "Validation Error", "errors": _sanitize_errors(exc.errors())})


async def http_exception_handler(request: Request, exc: HTTPException) -> JSONResponse:
    return JSONResponse(status_code=exc.status_code, content={"detail": exc.detail})


async def generic_exception_handler(request: Request, exc: Exception) -> JSONResponse:
    logger.error(json.dumps({"step": "UNHANDLED", "outcome": "FAILURE", "error": str(exc)}), exc_info=True)
    return JSONResponse(status_code=500, content={"detail": "Internal Error"})


def register_exception_handlers(app: FastAPI) -> None:
    app.add_exception_handler(RequestValidationError, request_validation_exception_handler)
    app.add_exception_handler(HTTPException, http_exception_handler)
    app.add_exception_handler(Exception, generic_exception_handler)

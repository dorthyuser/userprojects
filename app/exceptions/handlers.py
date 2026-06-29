import json
import logging
from typing import Any

from fastapi import FastAPI, Request, status
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
            elif isinstance(value, list):
                clean_error[key] = [str(item) if isinstance(item, Exception) else item for item in value]
            else:
                clean_error[key] = value
        sanitized.append(clean_error)
    return sanitized


async def validation_exception_handler(request: Request, exc: RequestValidationError) -> JSONResponse:
    logger.error(json.dumps({"event": "validation_failure", "rule": "request_validation"}))
    return JSONResponse(
        status_code=status.HTTP_422_UNPROCESSABLE_ENTITY,
        content={"detail": "Validation Error", "errors": _sanitize_errors(exc.errors())},
    )


async def generic_exception_handler(request: Request, exc: Exception) -> JSONResponse:
    logger.error("Unexpected error: %s", str(exc), exc_info=True)
    return JSONResponse(
        status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
        content={"detail": "Internal Error"},
    )


def register_exception_handlers(app: FastAPI) -> None:
    app.add_exception_handler(RequestValidationError, validation_exception_handler)
    app.add_exception_handler(Exception, generic_exception_handler)

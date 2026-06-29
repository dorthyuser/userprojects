import json
import logging
from collections.abc import Mapping, Sequence
from typing import Any

from fastapi import FastAPI, Request, status
from fastapi.exceptions import RequestValidationError
from fastapi.responses import JSONResponse

logger = logging.getLogger(__name__)


def _sanitize_error_value(value: Any) -> Any:
    if isinstance(value, Exception):
        return str(value)
    if isinstance(value, Mapping):
        return {str(key): _sanitize_error_value(item) for key, item in value.items()}
    if isinstance(value, Sequence) and not isinstance(value, (str, bytes, bytearray)):
        return [_sanitize_error_value(item) for item in value]
    return value


def _validation_payload(exc: RequestValidationError) -> dict[str, Any]:
    return {"detail": _sanitize_error_value(exc.errors())}


async def request_validation_exception_handler(request: Request, exc: RequestValidationError) -> JSONResponse:
    logger.error(json.dumps({"event": "validation_failure", "rule": "request_validation"}))
    return JSONResponse(status_code=status.HTTP_422_UNPROCESSABLE_ENTITY, content={"detail": "Validation Error"})


async def generic_exception_handler(request: Request, exc: Exception) -> JSONResponse:
    logger.error("Unhandled exception: %s", str(exc), exc_info=True)
    return JSONResponse(status_code=status.HTTP_500_INTERNAL_SERVER_ERROR, content={"detail": "Internal Error"})


def register_exception_handlers(app: FastAPI) -> None:
    app.add_exception_handler(RequestValidationError, request_validation_exception_handler)
    app.add_exception_handler(Exception, generic_exception_handler)

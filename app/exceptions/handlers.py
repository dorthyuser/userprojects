import json
import logging
from typing import Any

from fastapi import FastAPI, Request
from fastapi.exceptions import RequestValidationError
from fastapi.responses import JSONResponse

logger = logging.getLogger(__name__)


def _json_safe_errors(errors: list[dict[str, Any]]) -> list[dict[str, Any]]:
    safe_errors: list[dict[str, Any]] = []
    for error in errors:
        safe_error: dict[str, Any] = {}
        for key, value in error.items():
            safe_error[key] = str(value) if isinstance(value, Exception) else value
        safe_errors.append(safe_error)
    return safe_errors


def register_exception_handlers(app: FastAPI) -> None:
    @app.exception_handler(RequestValidationError)
    async def validation_exception_handler(request: Request, exc: RequestValidationError) -> JSONResponse:
        logger.warning(json.dumps({"event": "validation_error", "path": request.url.path}))
        return JSONResponse(status_code=422, content={"detail": _json_safe_errors(exc.errors())})

    @app.exception_handler(Exception)
    async def generic_exception_handler(request: Request, exc: Exception) -> JSONResponse:
        logger.error(json.dumps({"event": "unhandled_exception", "path": request.url.path, "error": str(exc)}), exc_info=True)
        return JSONResponse(status_code=500, content={"detail": "Internal Error"})

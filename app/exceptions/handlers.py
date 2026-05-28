import json
import logging
from collections.abc import Callable
from fastapi import FastAPI, Request
from fastapi.exceptions import RequestValidationError
from fastapi.responses import JSONResponse

logger = logging.getLogger(__name__)


def _sanitize_errors(errors: list[dict[str, object]]) -> list[dict[str, object]]:
    sanitized: list[dict[str, object]] = []
    for error in errors:
        item: dict[str, object] = {}
        for key, value in error.items():
            item[key] = str(value) if isinstance(value, Exception) else value
        sanitized.append(item)
    return sanitized


def register_exception_handlers(app: FastAPI) -> None:
    @app.exception_handler(RequestValidationError)
    async def request_validation_handler(request: Request, exc: RequestValidationError) -> JSONResponse:
        logger.info(json.dumps({"event": "validation_error", "path": str(request.url.path)}))
        return JSONResponse(status_code=422, content={"detail": _sanitize_errors(exc.errors())})

    @app.exception_handler(Exception)
    async def generic_exception_handler(request: Request, exc: Exception) -> JSONResponse:
        logger.error(json.dumps({"event": "unhandled_exception", "message": str(exc)}))
        return JSONResponse(status_code=500, content={"detail": "Internal server error"})

import json
import logging
from typing import Any

from fastapi import FastAPI, Request
from fastapi.exceptions import RequestValidationError
from fastapi.responses import JSONResponse

logger = logging.getLogger(__name__)


def _json_safe(value: Any) -> Any:
    if isinstance(value, Exception):
        return str(value)
    if isinstance(value, dict):
        return {str(key): _json_safe(val) for key, val in value.items()}
    if isinstance(value, list):
        return [_json_safe(item) for item in value]
    return value


def register_exception_handlers(app: FastAPI) -> None:
    @app.exception_handler(RequestValidationError)
    async def request_validation_handler(request: Request, exc: RequestValidationError) -> JSONResponse:
        logger.warning(json.dumps({"event": "request_validation_error", "path": request.url.path}))
        return JSONResponse(status_code=422, content={"detail": _json_safe(exc.errors())})

    @app.exception_handler(Exception)
    async def generic_exception_handler(request: Request, exc: Exception) -> JSONResponse:
        logger.error(json.dumps({"event": "unhandled_exception", "path": request.url.path, "error": str(exc)}))
        return JSONResponse(status_code=500, content={"detail": "Internal Server Error"})

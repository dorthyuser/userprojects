import logging
from collections.abc import Callable

from fastapi import FastAPI, Request
from fastapi.exceptions import RequestValidationError
from fastapi.responses import JSONResponse

logger = logging.getLogger(__name__)


def _json_log(level: str, message: str, **fields: object) -> None:
    payload = {"level": level, "message": message, **fields}
    logger.info(payload)


async def validation_exception_handler(request: Request, exc: RequestValidationError) -> JSONResponse:
    _json_log("warning", "request validation failed", method=request.method, path=request.url.path)
    return JSONResponse(status_code=422, content={"detail": exc.errors()})


async def generic_exception_handler(request: Request, exc: Exception) -> JSONResponse:
    _json_log("error", "unhandled exception", method=request.method, path=request.url.path, error=str(exc))
    return JSONResponse(status_code=500, content={"detail": "Internal Server Error"})


def register_exception_handlers(app: FastAPI) -> None:
    app.add_exception_handler(RequestValidationError, validation_exception_handler)
    app.add_exception_handler(Exception, generic_exception_handler)

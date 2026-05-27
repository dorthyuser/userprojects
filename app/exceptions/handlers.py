import logging

from fastapi import FastAPI, Request
from fastapi.exceptions import RequestValidationError
from fastapi.responses import JSONResponse

logger = logging.getLogger(__name__)


def register_exception_handlers(app: FastAPI) -> None:
    @app.exception_handler(RequestValidationError)
    async def validation_exception_handler(request: Request, exc: RequestValidationError) -> JSONResponse:
        logger.error(
            "validation_error",
            extra={"path": str(request.url.path), "method": request.method},
        )
        return JSONResponse(status_code=422, content={"detail": exc.errors()})

    @app.exception_handler(Exception)
    async def generic_exception_handler(request: Request, exc: Exception) -> JSONResponse:
        logger.error(
            f"unexpected_error: {exc}",
            extra={"path": str(request.url.path), "method": request.method},
        )
        return JSONResponse(status_code=500, content={"detail": "Internal server error"})

import json
import logging
from fastapi import FastAPI, Request
from fastapi.exceptions import RequestValidationError
from fastapi.responses import JSONResponse

logger = logging.getLogger(__name__)


def register_exception_handlers(app: FastAPI) -> None:

    @app.exception_handler(RequestValidationError)
    async def validation_exception_handler(request: Request, exc: RequestValidationError) -> JSONResponse:
        logger.info(json.dumps({"event": "validation_error", "path": str(request.url.path)}))

        def _make_serializable(obj):
            if isinstance(obj, dict):
                return {k: _make_serializable(v) for k, v in obj.items()}
            if isinstance(obj, list):
                return [_make_serializable(i) for i in obj]
            if isinstance(obj, Exception):
                return str(obj)
            return obj

        return JSONResponse(status_code=422, content={"detail": _make_serializable(exc.errors())})

    @app.exception_handler(Exception)
    async def generic_exception_handler(request: Request, exc: Exception) -> JSONResponse:
        logger.info(json.dumps({"event": "generic_error", "error": str(exc)}))
        return JSONResponse(status_code=500, content={"detail": "Internal server error"})

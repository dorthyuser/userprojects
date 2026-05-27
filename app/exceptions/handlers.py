from __future__ import annotations

import logging

from fastapi import Request
from fastapi.exceptions import RequestValidationError
from fastapi.responses import JSONResponse

logger = logging.getLogger("crm.new_test_prime_number")


async def validation_exception_handler(request: Request, exc: RequestValidationError) -> JSONResponse:
    logger.info('{"event":"validation_error","path":"%s","method":"%s"}' % (request.url.path, request.method))
    return JSONResponse(status_code=422, content={"detail": exc.errors()})


async def generic_exception_handler(request: Request, exc: Exception) -> JSONResponse:
    logger.error('{"event":"unhandled_exception","path":"%s","method":"%s","message":"%s"}' % (request.url.path, request.method, str(exc).replace('"', "'")))
    return JSONResponse(status_code=500, content={"detail": "Internal Server Error"})

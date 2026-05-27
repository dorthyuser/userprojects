from __future__ import annotations

import logging
from contextlib import asynccontextmanager
from typing import AsyncIterator

from fastapi import FastAPI
from fastapi.requests import Request
from fastapi.responses import JSONResponse
from fastapi.exceptions import RequestValidationError

from app.exceptions.handlers import generic_exception_handler, validation_exception_handler
from app.routers.prime_numbers_router import router as prime_numbers_router

logger = logging.getLogger("crm.new_test_prime_number")
logging.basicConfig(level=logging.INFO, format="%(message)s")


@asynccontextmanager
async def lifespan(app: FastAPI) -> AsyncIterator[None]:
    logger.info('{"event":"startup","service":"new-test-prime-number"}')
    yield
    logger.info('{"event":"shutdown","service":"new-test-prime-number"}')


app = FastAPI(title="new-test-prime-number", version="1.0.0", lifespan=lifespan)
app.include_router(prime_numbers_router)

app.add_exception_handler(RequestValidationError, validation_exception_handler)
app.add_exception_handler(Exception, generic_exception_handler)

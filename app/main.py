import json
import logging
from contextlib import asynccontextmanager
from typing import AsyncIterator

from fastapi import FastAPI, Request
from fastapi.responses import JSONResponse

from app.exceptions.handlers import register_exception_handlers
from app.routers.prime_router import router as prime_router

logger = logging.getLogger(__name__)
logging.basicConfig(level=logging.INFO, format="%(message)s")


@asynccontextmanager
async def lifespan(app: FastAPI) -> AsyncIterator[None]:
    logger.info(json.dumps({"event": "startup", "service": "primenumber2"}))
    yield
    logger.info(json.dumps({"event": "shutdown", "service": "primenumber2"}))


app = FastAPI(title="primenumber2", version="1.0.0", lifespan=lifespan)
register_exception_handlers(app)
app.include_router(prime_router)


@app.get("/health")
async def health_check() -> JSONResponse:
    return JSONResponse(content={"status": "ok"})

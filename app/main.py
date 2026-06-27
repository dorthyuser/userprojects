import json
import logging
import os
from contextlib import asynccontextmanager
from typing import AsyncIterator

from fastapi import FastAPI

from app.exceptions.handlers import register_exception_handlers
from app.routers.sequence_router import router as sequence_router

logger = logging.getLogger(__name__)
logging.basicConfig(level=os.getenv("LOG_LEVEL", "INFO"), format="%(message)s")


@asynccontextmanager
async def lifespan(app: FastAPI) -> AsyncIterator[None]:
    logger.info(json.dumps({"event": "startup", "service": "sequence"}))
    yield
    logger.info(json.dumps({"event": "shutdown", "service": "sequence"}))


app = FastAPI(title="test-new-cl", version="1.0.0", lifespan=lifespan)
register_exception_handlers(app)
app.include_router(sequence_router)

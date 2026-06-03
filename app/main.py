import json
import logging
import os
from contextlib import asynccontextmanager
from typing import Any

from fastapi import FastAPI

from app.exceptions.handlers import register_exception_handlers
from app.routers.adverse_events_router import router as adverse_events_router

logger = logging.getLogger(__name__)
logging.basicConfig(level=os.environ.get("LOG_LEVEL", "INFO"))


@asynccontextmanager
async def lifespan(app: FastAPI):
    logger.info(json.dumps({"event": "startup", "service": "crm-ae"}))
    yield
    logger.info(json.dumps({"event": "shutdown", "service": "crm-ae"}))


app = FastAPI(title="new-ae-test", version="1.0.0", lifespan=lifespan)
app.include_router(adverse_events_router)
register_exception_handlers(app)

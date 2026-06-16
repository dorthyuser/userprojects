import json
import logging
import os
from contextlib import asynccontextmanager

from fastapi import FastAPI

from app.exceptions.handlers import register_exception_handlers
from app.routers.adverse_event_router import router as adverse_event_router

logger = logging.getLogger(__name__)
logging.basicConfig(level=os.environ.get("LOG_LEVEL", "INFO"))


@asynccontextmanager
async def lifespan(app: FastAPI):
    logger.info(json.dumps({"event": "startup", "resource": "adverse-events"}))
    yield
    logger.info(json.dumps({"event": "shutdown", "resource": "adverse-events"}))


app = FastAPI(lifespan=lifespan)
app.include_router(adverse_event_router)
register_exception_handlers(app)

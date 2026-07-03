import json
import logging
from contextlib import asynccontextmanager

from fastapi import FastAPI

from app.exceptions.handlers import register_exception_handlers
from app.routers.adverse_events_router import router as adverse_events_router

logger = logging.getLogger(__name__)


@asynccontextmanager
async def lifespan(app: FastAPI):
    logger.info(json.dumps({"event": "startup", "resource": "api"}))
    yield
    logger.info(json.dumps({"event": "shutdown", "resource": "api"}))


app = FastAPI(title="adverseproject", version="1.0.0", lifespan=lifespan)
register_exception_handlers(app)
app.include_router(adverse_events_router)

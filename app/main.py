import json
import logging
from contextlib import asynccontextmanager

from fastapi import FastAPI, Request
from fastapi.responses import JSONResponse

from app.exceptions.handlers import register_exception_handlers
from app.routers.adverse_events_router import router as adverse_events_router

logger = logging.getLogger(__name__)


@asynccontextmanager
async def lifespan(app: FastAPI):
    logger.info(json.dumps({"event": "startup", "service": "testing1"}))
    yield
    logger.info(json.dumps({"event": "shutdown", "service": "testing1"}))


app = FastAPI(title="testing1", version="1.0.0", lifespan=lifespan)
register_exception_handlers(app)
app.include_router(adverse_events_router)


@app.get("/health")
async def health() -> JSONResponse:
    return JSONResponse(content={"status": "ok"})

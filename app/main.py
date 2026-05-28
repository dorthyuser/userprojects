import json
import logging

from fastapi import FastAPI

from app.exceptions.handlers import register_exception_handlers
from app.routers.travelcard_router import router as travelcard_router

logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

app = FastAPI(title="pythontravelcard949")
app.include_router(travelcard_router)
register_exception_handlers(app)


@app.on_event("startup")
async def startup_event() -> None:
    logger.info(json.dumps({"event": "startup", "resource": "travelcard"}))


@app.on_event("shutdown")
async def shutdown_event() -> None:
    logger.info(json.dumps({"event": "shutdown", "resource": "travelcard"}))
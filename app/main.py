import json
import logging

from fastapi import FastAPI

from app.exceptions.handlers import register_exception_handlers
from app.routers.travelcard_router import router as travelcard_router

logging.basicConfig(level=logging.INFO, format="%(message)s")
logger = logging.getLogger(__name__)

app = FastAPI(title="travelcard-tc")


@app.on_event("startup")
def startup_event() -> None:
    logger.info(json.dumps({"event": "startup", "service": "travelcard-tc"}))


@app.on_event("shutdown")
def shutdown_event() -> None:
    logger.info(json.dumps({"event": "shutdown", "service": "travelcard-tc"}))


register_exception_handlers(app)
app.include_router(travelcard_router)

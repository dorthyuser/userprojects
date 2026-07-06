import logging
import json

from fastapi import FastAPI

from app.exceptions.handlers import register_exception_handlers
from app.routers.adverse_event_router import router as adverse_event_router

logger = logging.getLogger(__name__)

app = FastAPI(title="Adverse Event Reporter API", version="1.0.0")

register_exception_handlers(app)
app.include_router(adverse_event_router)


@app.on_event("startup")
def startup_event() -> None:
    logger.info(json.dumps({"event": "startup", "service": "adverse-event-reporter"}))


@app.on_event("shutdown")
def shutdown_event() -> None:
    logger.info(json.dumps({"event": "shutdown", "service": "adverse-event-reporter"}))

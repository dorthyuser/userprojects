import json
import logging
import os

from fastapi import FastAPI

from app.exceptions.handlers import register_exception_handlers
from app.routers.adverse_events_router import router as adverse_events_router

logger = logging.getLogger(__name__)
logging.basicConfig(level=os.environ.get("LOG_LEVEL", "INFO"), format="%(message)s")

app = FastAPI(title="pythonlambdaae957", version="1.0.0")
register_exception_handlers(app)
app.include_router(adverse_events_router)


@app.on_event("startup")
def startup_event() -> None:
    logger.info(json.dumps({"event": "startup", "service": "pythonlambdaae957"}))


@app.on_event("shutdown")
def shutdown_event() -> None:
    logger.info(json.dumps({"event": "shutdown", "service": "pythonlambdaae957"}))

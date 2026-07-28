import logging
import os

from fastapi import FastAPI

from app.exceptions.handlers import register_exception_handlers
from app.routers.adverse_events_router import router as adverse_events_router
from app.services.adverse_events_service import initialize_service

LOG_LEVEL = os.getenv("LOG_LEVEL", "INFO").upper()
logging.basicConfig(level=getattr(logging, LOG_LEVEL, logging.INFO), format="%(message)s")

app = FastAPI(title="aepython714pm", version="1.0.0")
register_exception_handlers(app)
app.include_router(adverse_events_router)


@app.on_event("startup")
def startup_event() -> None:
    initialize_service()


@app.on_event("shutdown")
def shutdown_event() -> None:
    return None

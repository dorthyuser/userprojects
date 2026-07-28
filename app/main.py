import logging
import os

from fastapi import FastAPI

from app.exceptions.handlers import register_exception_handlers
from app.routers.adverse_events_router import router as adverse_events_router

LOG_LEVEL = os.getenv("LOG_LEVEL", "INFO").upper()
logging.basicConfig(level=getattr(logging, LOG_LEVEL, logging.INFO), format="%(message)s")

app = FastAPI(title="aepython714pm", version="1.0.0")
register_exception_handlers(app)
app.include_router(adverse_events_router)

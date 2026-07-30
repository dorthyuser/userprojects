import json
import logging
import os
from contextlib import asynccontextmanager
from typing import Any

from fastapi import FastAPI, Request

from app.exceptions.handlers import register_exception_handlers
from app.routers.adverse_event_router import router as adverse_event_router
from app.routers.notification_router import router as notification_router


class JsonFormatter(logging.Formatter):
    def format(self, record: logging.LogRecord) -> str:
        payload = {
            "timestamp": self.formatTime(record, datefmt="%Y-%m-%dT%H:%M:%SZ"),
            "level": record.levelname,
            "logger": record.name,
            "message": record.getMessage(),
        }
        if record.exc_info:
            payload["error"] = self.formatException(record.exc_info)
        return json.dumps(payload)


def _configure_logging() -> None:
    root_logger = logging.getLogger()
    if root_logger.handlers:
        return
    handler = logging.StreamHandler()
    handler.setFormatter(JsonFormatter())
    root_logger.addHandler(handler)
    root_logger.setLevel(os.getenv("LOG_LEVEL", "INFO").upper())


@asynccontextmanager
async def lifespan(app: FastAPI):
    _configure_logging()
    yield


app = FastAPI(title="ae-demo413pm", lifespan=lifespan)
register_exception_handlers(app)
app.include_router(adverse_event_router)
app.include_router(notification_router)


@app.get("/health")
async def health() -> dict[str, str]:
    return {"status": "ok"}

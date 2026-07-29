import logging
import os
from contextlib import asynccontextmanager

from fastapi import FastAPI

from app.exceptions.handlers import register_exception_handlers
from app.routers.adverse_events_router import router as adverse_events_router


class JsonFormatter(logging.Formatter):
    def format(self, record: logging.LogRecord) -> str:
        message = {
            "timestamp": self.formatTime(record, datefmt="%Y-%m-%dT%H:%M:%SZ"),
            "level": record.levelname,
            "logger": record.name,
            "message": record.getMessage(),
        }
        return __import__("json").dumps(message)


def configure_logging() -> None:
    root_logger = logging.getLogger()
    if not root_logger.handlers:
        handler = logging.StreamHandler()
        handler.setFormatter(JsonFormatter())
        root_logger.addHandler(handler)
    root_logger.setLevel(getattr(logging, os.getenv("LOG_LEVEL", "INFO").upper(), logging.INFO))


@asynccontextmanager
async def lifespan(app: FastAPI):
    configure_logging()
    yield


app = FastAPI(title="aeproject1139", version="1.0.0", lifespan=lifespan)
register_exception_handlers(app)
app.include_router(adverse_events_router)

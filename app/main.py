import logging
import sys
from typing import Any

from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware

from app.exceptions.handlers import register_exception_handlers
from app.routers.prime_numbers_router import router as prime_numbers_router


class JsonFormatter(logging.Formatter):
    def format(self, record: logging.LogRecord) -> str:
        payload: dict[str, Any] = {
            "level": record.levelname,
            "logger": record.name,
            "message": record.getMessage()
        }
        return __import__("json").dumps(payload)


def configure_logging() -> None:
    handler = logging.StreamHandler(sys.stdout)
    handler.setFormatter(JsonFormatter())
    root_logger = logging.getLogger()
    root_logger.handlers.clear()
    root_logger.addHandler(handler)
    root_logger.setLevel(logging.INFO)


configure_logging()

app = FastAPI(title="new-test-prime-number", version="1.0.0")

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"]
)

register_exception_handlers(app)
app.include_router(prime_numbers_router)


@app.on_event("startup")
async def startup_event() -> None:
    logging.getLogger(__name__).info("application_startup")


@app.on_event("shutdown")
async def shutdown_event() -> None:
    logging.getLogger(__name__).info("application_shutdown")

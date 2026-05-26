import logging
import os

from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware

from app.exceptions.handlers import register_exception_handlers
from app.routers.travelcards_router import router as travelcards_router


class JsonFormatter(logging.Formatter):
    def format(self, record: logging.LogRecord) -> str:
        payload = {
            "level": record.levelname,
            "name": record.name,
            "message": record.getMessage()
        }
        if record.exc_info:
            payload["exception"] = self.formatException(record.exc_info)
        return str(payload).replace("'", '"')


def configure_logging() -> None:
    root_logger = logging.getLogger()
    if root_logger.handlers:
        return
    handler = logging.StreamHandler()
    handler.setFormatter(JsonFormatter())
    root_logger.setLevel(logging.INFO)
    root_logger.addHandler(handler)


configure_logging()

app = FastAPI(title="travelcard-py", version="1.0.0")

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

register_exception_handlers(app)
app.include_router(travelcards_router)


@app.on_event("startup")
async def startup_event() -> None:
    logging.getLogger(__name__).info(
        "startup",
        extra={"service": "travelcard-py", "port": os.environ.get("PORT", "8080")}
    )


@app.on_event("shutdown")
async def shutdown_event() -> None:
    logging.getLogger(__name__).info("shutdown", extra={"service": "travelcard-py"})

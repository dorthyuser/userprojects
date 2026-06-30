import json
import logging

from fastapi import FastAPI

from app.exceptions.handlers import register_exception_handlers
from app.routers.prime_router import router as prime_router

logging.basicConfig(level=logging.INFO, format="%(message)s")
logger = logging.getLogger(__name__)

app = FastAPI(title="primenumber2", version="1.0.0")
register_exception_handlers(app)
app.include_router(prime_router)


@app.on_event("startup")
def startup_event() -> None:
    logger.info(json.dumps({"event": "startup", "service": "primenumber2"}))


@app.on_event("shutdown")
def shutdown_event() -> None:
    logger.info(json.dumps({"event": "shutdown", "service": "primenumber2"}))

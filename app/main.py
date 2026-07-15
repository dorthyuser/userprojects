import logging
import json

from fastapi import FastAPI

from app.exceptions.handlers import register_exception_handlers
from app.routers.users_router import router as users_router
from app.routers.users_local_router import router as users_local_router

logger = logging.getLogger(__name__)
logging.basicConfig(level=logging.INFO, format="%(message)s")

app = FastAPI(title="crmusecase1144am", version="1.0.0")
register_exception_handlers(app)
app.include_router(users_router)
app.include_router(users_local_router)


@app.on_event("startup")
def startup_event() -> None:
    logger.info(json.dumps({"event": "startup", "service": "crmusecase1144am"}))


@app.on_event("shutdown")
def shutdown_event() -> None:
    logger.info(json.dumps({"event": "shutdown", "service": "crmusecase1144am"}))

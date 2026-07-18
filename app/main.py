import logging

from fastapi import FastAPI

from app.exceptions.handlers import register_exception_handlers
from app.routers.users_router import router as users_router

app = FastAPI(title="zohocrm1145am")
register_exception_handlers(app)
app.include_router(users_router)

logger = logging.getLogger(__name__)


@app.on_event("startup")
def startup_event() -> None:
    logger.info("startup")


@app.on_event("shutdown")
def shutdown_event() -> None:
    logger.info("shutdown")

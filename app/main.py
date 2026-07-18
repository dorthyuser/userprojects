import logging
import os

from fastapi import FastAPI

from app.exceptions.handlers import register_exception_handlers
from app.routers.users_router import router as users_router
from app.routers.users_local_router import router as users_local_router

logging.basicConfig(level=logging.INFO)
app = FastAPI(title="zohocrm1145am")
register_exception_handlers(app)
app.include_router(users_router)
app.include_router(users_local_router)


@app.on_event("startup")
def startup_event() -> None:
    os.environ.get("AWS_SECRET_NAME", "")


@app.on_event("shutdown")
def shutdown_event() -> None:
    return None

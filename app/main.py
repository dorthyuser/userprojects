import logging
import os

from fastapi import FastAPI

from app.exceptions.handlers import register_exception_handlers
from app.routers.users_router import router as users_router
from app.routers.users_local_router import router as users_local_router

logging.basicConfig(level=os.environ.get("LOG_LEVEL", "INFO"))
app = FastAPI(title="zohocrm1145am")
register_exception_handlers(app)
app.include_router(users_router)
app.include_router(users_local_router)

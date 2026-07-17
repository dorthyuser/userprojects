import logging
import os

from fastapi import FastAPI

from app.exceptions.handlers import register_exception_handlers
from app.routers.users_router import router as users_router

logging.basicConfig(level=os.environ.get("LOG_LEVEL", "INFO"))
app = FastAPI(title="crmusecase1144am", version="1.0.0")
register_exception_handlers(app)
app.include_router(users_router)

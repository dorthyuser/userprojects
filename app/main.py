import logging
import os

from fastapi import FastAPI

from app.exceptions.handlers import register_exception_handlers
from app.routers.payments_router import router as payments_router

logging.basicConfig(level=os.getenv("LOG_LEVEL", "INFO"))

app = FastAPI(title="payment325", version="1.0.0")
register_exception_handlers(app)
app.include_router(payments_router)

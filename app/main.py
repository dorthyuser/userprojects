import logging

from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware

from app.exceptions.handlers import register_exception_handlers
from app.routers.ae_router import router as ae_router

app = FastAPI(title="yoshiapipython", version="1.0.0")
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)
app.include_router(ae_router)
register_exception_handlers(app)

logging.getLogger(__name__).info("application_started")

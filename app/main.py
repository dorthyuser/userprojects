from fastapi import FastAPI

from app.routers.consent_router import router as consent_router
from app.exceptions.handlers import register_exception_handlers

app = FastAPI(title="patient-consent-management1005", version="1.1.0")

app.include_router(consent_router)
register_exception_handlers(app)

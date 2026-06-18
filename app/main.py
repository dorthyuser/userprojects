from fastapi import FastAPI

from app.exceptions.handlers import register_exception_handlers
from app.routers.consent_router import router as consent_router

app = FastAPI(title="patient-consent-management1005", version="1.1")

register_exception_handlers(app)
app.include_router(consent_router)

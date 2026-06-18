from fastapi import FastAPI

from app.exceptions.handlers import register_exception_handlers
from app.routers.consent_router import router as consent_router

app = FastAPI(title="patient-consent-management1005", version="1.1")

app.include_router(consent_router)
register_exception_handlers(app)


@app.on_event("startup")
async def startup_event() -> None:
    return None


@app.on_event("shutdown")
async def shutdown_event() -> None:
    return None

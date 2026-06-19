from fastapi import FastAPI

from app.routers.consent_router import router as consent_router

app = FastAPI(
    title="Patient Consent Management API",
    version="1.1.0",
    docs_url=None,
    redoc_url=None,
    openapi_url="/openapi.json"
)

app.include_router(consent_router)


@app.on_event("startup")
def startup_event() -> None:
    return None


@app.on_event("shutdown")
def shutdown_event() -> None:
    return None

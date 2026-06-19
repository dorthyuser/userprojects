import json
import logging
from contextlib import asynccontextmanager

from fastapi import FastAPI, Request
from fastapi.responses import JSONResponse

from app.routers.consent_router import router as consent_router

logger = logging.getLogger(__name__)


@asynccontextmanager
async def lifespan(app: FastAPI):
    logger.info(json.dumps({"event": "startup", "service": "patient-consent-management1005"}))
    yield
    logger.info(json.dumps({"event": "shutdown", "service": "patient-consent-management1005"}))


app = FastAPI(title="Patient Consent Management API", version="1.0.0", lifespan=lifespan)
app.include_router(consent_router)


@app.get("/health")
async def health() -> JSONResponse:
    return JSONResponse(content={"status": "ok"})

import logging
import json
from fastapi import FastAPI
from app.routers.travelcard_router import router as travelcard_router
from app.exceptions.handlers import register_exception_handlers

logger = logging.getLogger(__name__)

app = FastAPI(title="pythontravelcard234")


@app.on_event("startup")
async def startup_event() -> None:
    logger.info(json.dumps({"event": "startup", "service": "pythontravelcard234"}))


@app.on_event("shutdown")
async def shutdown_event() -> None:
    logger.info(json.dumps({"event": "shutdown", "service": "pythontravelcard234"}))


app.include_router(travelcard_router)
register_exception_handlers(app)

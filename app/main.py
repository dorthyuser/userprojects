import json
import logging
from fastapi import FastAPI
from app.exceptions.handlers import register_exception_handlers
from app.routers.travelcard_router import router as travelcard_router

logger = logging.getLogger(__name__)
logging.basicConfig(level=logging.INFO, format="%(message)s")

app = FastAPI(title="pythontravelcard203")
register_exception_handlers(app)
app.include_router(travelcard_router)


@app.on_event("startup")
async def startup_event() -> None:
    logger.info(json.dumps({"event": "startup", "service": "pythontravelcard203"}))


@app.on_event("shutdown")
async def shutdown_event() -> None:
    logger.info(json.dumps({"event": "shutdown", "service": "pythontravelcard203"}))
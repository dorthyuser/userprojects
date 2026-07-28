import logging
import os

from fastapi import FastAPI

from app.exceptions.handlers import register_exception_handlers
from app.routers.adverse_events_router import router as adverse_events_router
from app.services.logging_service import configure_logging
from app.services.startup_service import initialize_application

app = FastAPI(title="aeproject414pm", version="1.0.0")

configure_logging()
register_exception_handlers(app)
app.include_router(adverse_events_router)


@app.on_event("startup")
def startup_event() -> None:
    initialize_application()


@app.on_event("shutdown")
def shutdown_event() -> None:
    logging.getLogger(__name__).info(
        '{"event":"shutdown","status":"ok"}'
    )

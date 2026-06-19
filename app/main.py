import os

from fastapi import FastAPI

from app.exceptions.handlers import register_exception_handlers
from app.routers.adverse_events_router import router as adverse_events_router

app = FastAPI(title="pythonlambdaae957", version="1.0.0")
register_exception_handlers(app)
app.include_router(adverse_events_router)


@app.on_event("startup")
def startup_event() -> None:
    # Startup placeholder: connection pools are lazy-created on first use
    pass


@app.on_event("shutdown")
def shutdown_event() -> None:
    # Shutdown placeholder
    pass

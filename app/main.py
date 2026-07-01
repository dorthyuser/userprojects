from fastapi import FastAPI

from app.exceptions.handlers import register_exception_handlers
from app.routers.adverse_events_router import router as adverse_events_router

app = FastAPI(title="Adverse Event Reporter API", version="1.0.0")

register_exception_handlers(app)
app.include_router(adverse_events_router)


@app.on_event("startup")
def startup_event() -> None:
    return None


@app.on_event("shutdown")
def shutdown_event() -> None:
    return None

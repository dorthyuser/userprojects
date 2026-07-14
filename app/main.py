from fastapi import FastAPI

from app.exceptions.handlers import register_exception_handlers
from app.routers.adverse_event_router import router as adverse_event_router
from app.routers.notification_router import router as notification_router

app = FastAPI(title="ae-demo413pm", version="1.0.0")

register_exception_handlers(app)
app.include_router(adverse_event_router)
app.include_router(notification_router)


@app.on_event("startup")
def startup_event() -> None:
    return None


@app.on_event("shutdown")
def shutdown_event() -> None:
    return None

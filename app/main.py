from fastapi import FastAPI

from app.exceptions.handlers import register_exception_handlers
from app.routers.adverse_event_router import router as adverse_event_router

app = FastAPI(title="adverse-csharp", version="1.0.0")

register_exception_handlers(app)
app.include_router(adverse_event_router)

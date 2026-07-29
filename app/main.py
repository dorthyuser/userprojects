from fastapi import FastAPI

from app.exceptions.handlers import register_exception_handlers
from app.routers.adverse_events_router import router as adverse_events_router

app = FastAPI(title="aepython423", version="1.0.0")

register_exception_handlers(app)
app.include_router(adverse_events_router)

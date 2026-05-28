from fastapi import FastAPI

from app.exceptions.handlers import register_exception_handlers
from app.routers.extract_router import router as extract_router

app = FastAPI(title="ai2dev.datascraper", version="1.0.0")

register_exception_handlers(app)
app.include_router(extract_router)


@app.on_event("startup")
async def startup_event() -> None:
    return None


@app.on_event("shutdown")
async def shutdown_event() -> None:
    return None

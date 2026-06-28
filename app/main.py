from fastapi import FastAPI

from app.exceptions.handlers import register_exception_handlers
from app.routers.sequence_router import router as sequence_router

app = FastAPI(title="test-new-cl", version="1.0.0")


@app.on_event("startup")
async def startup_event() -> None:
    return None


@app.on_event("shutdown")
async def shutdown_event() -> None:
    return None


app.include_router(sequence_router)
register_exception_handlers(app)

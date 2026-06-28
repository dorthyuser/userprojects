from fastapi import FastAPI

from app.exceptions.handlers import register_exception_handlers
from app.routers.pharma_advancements_router import router as pharma_advancements_router

app = FastAPI(title="test-my-new-clinical", version="1.0.0")


@app.on_event("startup")
async def startup_event() -> None:
    return None


@app.on_event("shutdown")
async def shutdown_event() -> None:
    return None


app.include_router(pharma_advancements_router)
register_exception_handlers(app)

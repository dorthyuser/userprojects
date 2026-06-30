from fastapi import FastAPI

from app.exceptions.handlers import register_exception_handlers
from app.routers.prime_router import router as prime_router

app = FastAPI(title="primenumber2", version="1.0.0")

register_exception_handlers(app)
app.include_router(prime_router)


@app.on_event("startup")
async def startup_event() -> None:
    return None


@app.on_event("shutdown")
async def shutdown_event() -> None:
    return None

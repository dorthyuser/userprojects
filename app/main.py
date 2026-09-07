from fastapi import FastAPI

from app.exceptions.handlers import register_exception_handlers
from app.routers.payments_router import router as payments_router

app = FastAPI(title="python930", version="1.0.0")

register_exception_handlers(app)
app.include_router(payments_router)


@app.on_event("startup")
def startup_event() -> None:
    return None


@app.on_event("shutdown")
def shutdown_event() -> None:
    return None

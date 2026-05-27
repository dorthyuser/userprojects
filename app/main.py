from fastapi import FastAPI

from app.exceptions.handlers import register_exception_handlers
from app.routers.prime_numbers_router import router as prime_numbers_router

app = FastAPI(title="new-test-prime-number", version="1.0.0")


@app.on_event("startup")
def on_startup() -> None:
    return None


@app.on_event("shutdown")
def on_shutdown() -> None:
    return None


app.include_router(prime_numbers_router)
register_exception_handlers(app)

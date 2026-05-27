from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware

from app.exceptions.handlers import register_exception_handlers
from app.routers.prime_numbers_router import router as prime_numbers_router
from app.services.prime_numbers_service import PrimeNumbersService

app = FastAPI(title="new-test-prime-number", version="1.0.0")

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

service = PrimeNumbersService()
app.include_router(prime_numbers_router(service), prefix="/script", tags=["prime-numbers"])
register_exception_handlers(app)


@app.on_event("startup")
async def startup_event() -> None:
    return None


@app.on_event("shutdown")
async def shutdown_event() -> None:
    return None

from fastapi import FastAPI
from app.exceptions.handlers import register_exception_handlers
from app.routers.ae_router import router as ae_router

app = FastAPI(title="yoshipythondemo", version="1.0.0")

register_exception_handlers(app)
app.include_router(ae_router)


@app.on_event("startup")
async def startup_event() -> None:
    return None


@app.on_event("shutdown")
async def shutdown_event() -> None:
    return None

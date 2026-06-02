from fastapi import FastAPI
from app.exceptions.handlers import register_exception_handlers
from app.routers.ae_router import router as ae_router

app = FastAPI(title="Toshi Clinical Adverse Event Reporter", version="1.0.0")

register_exception_handlers(app)
app.include_router(ae_router)


@app.on_event("startup")
def startup_event() -> None:
    return None


@app.on_event("shutdown")
def shutdown_event() -> None:
    return None

from fastapi import FastAPI
from app.exceptions.handlers import register_exception_handlers
from app.routers.ae_router import router as ae_router

app = FastAPI(title="Toshi AE Reporter", version="1.0.0")
register_exception_handlers(app)
app.include_router(ae_router)


@app.get("/health")
def health() -> dict[str, str]:
    return {"status": "ok"}

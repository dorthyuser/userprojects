from fastapi import FastAPI
from mangum import Mangum

from app.exceptions.handlers import register_exception_handlers
from app.routers.ae_router import router as ae_router

app = FastAPI(title="Toshi AE Reporter", version="1.0.0")
app.include_router(ae_router)
register_exception_handlers(app)


@app.get("/health")
def health() -> dict[str, str]:
    return {"status": "ok"}


lambda_handler = Mangum(app, lifespan="off")
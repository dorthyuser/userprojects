import json
import logging

from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware

from app.exceptions.handlers import register_exception_handlers
from app.routers.extract_router import router as extract_router


def _configure_logging() -> None:
    logging.basicConfig(level=logging.INFO, format="%(message)s")


def create_app() -> FastAPI:
    _configure_logging()
    app = FastAPI(title="data-scraper", version="1.0.0")
    app.add_middleware(
        CORSMiddleware,
        allow_origins=["*"],
        allow_credentials=True,
        allow_methods=["*"],
        allow_headers=["*"]
    )
    app.include_router(extract_router)
    register_exception_handlers(app)

    @app.on_event("startup")
    async def startup_event() -> None:
        logging.getLogger(__name__).info(json.dumps({"event": "startup"}))

    @app.on_event("shutdown")
    async def shutdown_event() -> None:
        logging.getLogger(__name__).info(json.dumps({"event": "shutdown"}))

    return app


app = create_app()
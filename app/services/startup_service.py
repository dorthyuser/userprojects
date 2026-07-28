import json
import logging
import os

from app.db.connection import get_pool

logger = logging.getLogger(__name__)


def _require_env(name: str) -> str:
    value = os.environ.get(name)
    if not value:
        logger.error(json.dumps({"event": "missing_env", "variable": name}))
        raise RuntimeError(f"Missing required environment variable: {name}")
    return value


def initialize_application() -> None:
    _require_env("NOTIFICATION_TARGET")
    _require_env("IDEMPOTENCY_WINDOW_S")
    get_pool()

import json
import logging
import os


def configure_logging() -> None:
    level_name = os.environ.get("LOG_LEVEL", "INFO").upper()
    level = getattr(logging, level_name, logging.INFO)
    logging.basicConfig(level=level, format="%(message)s")


class JsonLogAdapter:
    @staticmethod
    def emit(logger: logging.Logger, payload: dict[str, object], level: str = "info") -> None:
        message = json.dumps(payload, default=str)
        getattr(logger, level.lower())(message)

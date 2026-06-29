import json
import logging

logger = logging.getLogger(__name__)


def get_prime_number() -> int:
    logger.info(json.dumps({"event": "service_start", "operation": "get_prime_number", "resource": "prime"}))
    return 101

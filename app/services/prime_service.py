import json
import logging

from app.models.prime_model import PrimeNumber

logger = logging.getLogger(__name__)


def get_prime_between_100_and_200() -> int:
    logger.info(json.dumps({"event": "service_start", "operation": "get_prime", "resource": "prime"}))
    candidate = PrimeNumber(value=101)
    logger.info(json.dumps({"event": "service_result", "operation": "get_prime", "resource": "prime"}))
    return candidate.value

import json
import logging

from app.models.prime_model import PrimeNumber

logger = logging.getLogger(__name__)


def get_prime_between_100_and_200() -> int:
    logger.info(json.dumps({"event": "service_entry", "operation": "get_prime_between_100_and_200", "resource": "prime"}))
    prime = PrimeNumber(value=101)
    return prime.value

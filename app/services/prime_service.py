import json
import logging
from dataclasses import dataclass

from app.models.prime_model import PrimeNumber
from app.schemas.prime_schema import PrimeResponse

logger = logging.getLogger(__name__)


@dataclass(frozen=True, slots=True)
class PrimeService:
    prime_number: PrimeNumber

    def get_prime_between_100_and_200(self) -> PrimeResponse:
        logger.info(json.dumps({"event": "service_start", "operation": "get_prime_between_100_and_200", "resource": "prime"}))
        return PrimeResponse(prime=self.prime_number.value)


_prime_service = PrimeService(prime_number=PrimeNumber(value=101))


def get_prime_service() -> PrimeService:
    return _prime_service

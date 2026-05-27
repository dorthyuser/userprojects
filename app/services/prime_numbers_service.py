import logging
from dataclasses import asdict

from app.models.prime_numbers_model import PrimeNumbersResult
from app.schemas.prime_numbers_schema import PrimeNumbersResponse

logger = logging.getLogger(__name__)


class PrimeNumbersService:
    def generate_primes(self, start: int, end: int) -> PrimeNumbersResponse:
        logger.info(
            "db_operation",
            extra={"table": "none", "operation": "compute"},
        )
        primes = []
        for number in range(start, end + 1):
            if number < 2:
                continue
            is_prime = True
            for divisor in range(2, int(number ** 0.5) + 1):
                if number % divisor == 0:
                    is_prime = False
                    break
            if is_prime:
                primes.append(number)

        result = PrimeNumbersResult(start=start, end=end, primes=primes)
        return PrimeNumbersResponse(**asdict(result))

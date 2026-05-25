import logging
from dataclasses import dataclass

from app.schemas.prime_numbers_schema import PrimeNumbersResponse

logger = logging.getLogger(__name__)


@dataclass(frozen=True)
class PrimeNumbersService:
    def generate_primes(self, start: int, end: int) -> PrimeNumbersResponse:
        logger.info("db_operation table=none operation=compute")
        lower = min(start, end)
        upper = max(start, end)
        primes: list[int] = []

        for number in range(lower, upper + 1):
            if number < 2:
                continue
            is_prime = True
            for divisor in range(2, int(number ** 0.5) + 1):
                if number % divisor == 0:
                    is_prime = False
                    break
            if is_prime:
                primes.append(number)

        return PrimeNumbersResponse(start=lower, end=upper, primes=primes)


def get_prime_numbers_service() -> PrimeNumbersService:
    return PrimeNumbersService()

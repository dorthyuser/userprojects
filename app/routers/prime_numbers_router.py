from fastapi import APIRouter, Query

from app.schemas.prime_numbers_schema import PrimeNumbersResponse
from app.services.prime_numbers_service import generate_prime_numbers

router = APIRouter(prefix="/script", tags=["prime-numbers"])


@router.get("/prime-numbers", response_model=PrimeNumbersResponse)
def get_prime_numbers(start: int = Query(default=50, ge=2), end: int = Query(default=100, ge=2)) -> PrimeNumbersResponse:
    primes = generate_prime_numbers(start=start, end=end)
    return PrimeNumbersResponse(primes=primes)

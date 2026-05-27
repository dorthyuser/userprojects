from __future__ import annotations

import logging

from fastapi import APIRouter, Query

from app.schemas.prime_number_schema import PrimeNumbersResponse
from app.services.prime_number_service import generate_prime_numbers

router = APIRouter(prefix="/script", tags=["prime-numbers"])
logger = logging.getLogger("crm.new_test_prime_number")


@router.get("/prime-numbers", response_model=PrimeNumbersResponse)
async def get_prime_numbers(start: int = Query(default=50, ge=2), end: int = Query(default=100, ge=2)) -> PrimeNumbersResponse:
    logger.info('{"event":"route_entry","method":"GET","path":"/script/prime-numbers"}')
    primes = generate_prime_numbers(start=start, end=end)
    return PrimeNumbersResponse(start=start, end=end, primes=primes)

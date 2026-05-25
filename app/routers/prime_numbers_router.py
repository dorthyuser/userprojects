import logging
from typing import Annotated

from fastapi import APIRouter, Depends, Query, Request

from app.schemas.prime_numbers_schema import PrimeNumbersResponse
from app.services.prime_numbers_service import PrimeNumbersService, get_prime_numbers_service

router = APIRouter(prefix="/script", tags=["prime-numbers"])
logger = logging.getLogger(__name__)


@router.get("/prime-numbers", response_model=PrimeNumbersResponse)
def get_prime_numbers(
    request: Request,
    start: Annotated[int, Query(ge=2)] = 50,
    end: Annotated[int, Query(ge=2)] = 100,
    service: PrimeNumbersService = Depends(get_prime_numbers_service)
) -> PrimeNumbersResponse:
    logger.info(
        "route_entry method=%s path=%s",
        request.method,
        request.url.path
    )
    return service.generate_primes(start=start, end=end)

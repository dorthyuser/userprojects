import logging
from typing import Annotated

from fastapi import APIRouter, Depends, Query, Request

from app.schemas.prime_numbers_schema import PrimeNumbersResponse
from app.services.prime_numbers_service import PrimeNumbersService

logger = logging.getLogger(__name__)


def get_service() -> PrimeNumbersService:
    return PrimeNumbersService()


def router(service: PrimeNumbersService | None = None) -> APIRouter:
    api_router = APIRouter()
    injected_service = service or PrimeNumbersService()

    @api_router.get("/prime-numbers", response_model=PrimeNumbersResponse)
    async def generate_prime_numbers(
        request: Request,
        start: Annotated[int, Query(ge=0)],
        end: Annotated[int, Query(ge=0)]
    ) -> PrimeNumbersResponse:
        logger.info(
            "route_entry",
            extra={
                "http_method": request.method,
                "path": str(request.url.path),
                "identifier_fields": []
            },
        )
        return injected_service.generate_primes(start=start, end=end)

    return api_router

import json
import logging

from fastapi import APIRouter, HTTPException, Request, status

from app.schemas.prime_schema import PrimeResponse
from app.services.prime_service import get_prime_between_100_and_200

logger = logging.getLogger(__name__)
router = APIRouter(prefix="/prime", tags=["prime"])


@router.get("", response_model=PrimeResponse, status_code=status.HTTP_200_OK)
async def get_prime(request: Request) -> PrimeResponse:
    logger.info(
        json.dumps(
            {
                "event": "route_entry",
                "method": request.method,
                "path": str(request.url.path),
            }
        )
    )
    try:
        prime_value = get_prime_between_100_and_200()
        return PrimeResponse(prime=prime_value)
    except HTTPException:
        raise
    except Exception as exc:
        logger.error("Unexpected error: %s", str(exc), exc_info=True)
        raise HTTPException(status_code=status.HTTP_500_INTERNAL_SERVER_ERROR, detail="Internal Error") from exc

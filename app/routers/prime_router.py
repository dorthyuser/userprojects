import json
import logging

from fastapi import APIRouter, HTTPException
from psycopg2 import Error as Psycopg2Error

from app.schemas.prime_schema import PrimeNumberResponse
from app.services.prime_service import get_prime_number_between_100_and_200

logger = logging.getLogger(__name__)
router = APIRouter(prefix="/prime", tags=["prime"])


@router.get("", response_model=PrimeNumberResponse, status_code=200)
def read_prime_number() -> PrimeNumberResponse:
    logger.info(json.dumps({"event": "route_entry", "method": "GET", "path": "/prime", "resource": "prime"}))
    try:
        prime_number = get_prime_number_between_100_and_200()
        return PrimeNumberResponse(prime_number=prime_number)
    except Psycopg2Error as exc:
        logger.error("Database error: %s", str(exc))
        raise HTTPException(status_code=503, detail="Database Error") from exc
    except HTTPException:
        raise
    except Exception as exc:
        logger.error("Unexpected error: %s", str(exc), exc_info=True)
        raise HTTPException(status_code=500, detail="Internal Error") from exc

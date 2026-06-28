import json
import logging

from fastapi import APIRouter, HTTPException, status

from app.schemas.pharma_advancements_schema import PharmaAdvancementsResponse
from app.services.pharma_advancements_service import get_pharma_advancements

logger = logging.getLogger(__name__)
router = APIRouter(prefix="/pharma-advancements", tags=["pharma-advancements"])


@router.get("", response_model=PharmaAdvancementsResponse, status_code=status.HTTP_200_OK)
async def read_pharma_advancements() -> PharmaAdvancementsResponse:
    logger.info(json.dumps({"event": "route_entry", "method": "GET", "path": "/pharma-advancements"}))
    try:
        return get_pharma_advancements()
    except HTTPException:
        raise
    except Exception as exc:
        logger.error("Unexpected error: %s", str(exc), exc_info=True)
        raise HTTPException(status_code=status.HTTP_500_INTERNAL_SERVER_ERROR, detail="Internal Error")

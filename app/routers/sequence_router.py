import json
import logging

from fastapi import APIRouter, HTTPException, status

from app.schemas.sequence_schema import SequenceRequest, SequenceResponse
from app.services.sequence_service import generate_positive_sequence

logger = logging.getLogger(__name__)
router = APIRouter(prefix="/sequence", tags=["sequence"])


@router.post("", response_model=SequenceResponse, status_code=status.HTTP_201_CREATED)
async def create_sequence(payload: SequenceRequest) -> SequenceResponse:
    logger.info(json.dumps({"event": "route_entry", "method": "POST", "path": "/sequence"}))
    try:
        return generate_positive_sequence(payload)
    except HTTPException:
        raise
    except Exception as exc:
        logger.error("Unexpected error: %s", str(exc), exc_info=True)
        raise HTTPException(status_code=status.HTTP_500_INTERNAL_SERVER_ERROR, detail="Internal Error")

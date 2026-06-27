import json
import logging
from typing import List

from fastapi import HTTPException, status

from app.models.sequence_model import SequenceResult
from app.schemas.sequence_schema import SequenceRequest, SequenceResponse

logger = logging.getLogger(__name__)


def generate_positive_sequence(payload: SequenceRequest) -> SequenceResponse:
    logger.info(json.dumps({"event": "service_start", "operation": "generate", "resource": "sequence"}))
    try:
        if payload.start < -10 or payload.end > 50:
            logger.error("Validation failed: range bounds")
            raise HTTPException(status_code=status.HTTP_422_UNPROCESSABLE_ENTITY, detail="Validation Error")
        if payload.start > payload.end:
            logger.error("Validation failed: start greater than end")
            raise HTTPException(status_code=status.HTTP_422_UNPROCESSABLE_ENTITY, detail="Validation Error")

        values: List[int] = [number for number in range(payload.start, payload.end + 1) if number > 0]
        result = SequenceResult(start=payload.start, end=payload.end, values=values)
        return SequenceResponse(start=result.start, end=result.end, values=result.values)
    except HTTPException:
        raise
    except Exception as exc:
        logger.error("Unexpected error: %s", str(exc), exc_info=True)
        raise HTTPException(status_code=status.HTTP_500_INTERNAL_SERVER_ERROR, detail="Internal Error")

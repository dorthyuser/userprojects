import json
import logging

from fastapi import APIRouter, HTTPException, status

from app.schemas.extract_schema import ExtractRequest, ExtractResponse
from app.services.extract_service import extract_website_data

logger = logging.getLogger(__name__)
router = APIRouter(prefix="", tags=["extract"])


@router.get("/extract", response_model=ExtractResponse, status_code=status.HTTP_200_OK)
async def extract(url: str) -> ExtractResponse:
    logger.info(json.dumps({"event": "route_entry", "method": "GET", "path": "/extract"}))
    try:
        request = ExtractRequest(url=url)
        return extract_website_data(request)
    except HTTPException:
        raise
    except Exception as exc:
        logger.error(json.dumps({"event": "route_exception", "message": str(exc)}))
        raise HTTPException(status_code=500, detail="Internal server error") from exc

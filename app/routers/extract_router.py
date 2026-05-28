import json
from typing import Any

from fastapi import APIRouter, Depends, HTTPException, Request, status

from app.schemas.extract_schema import ExtractRequest, ExtractResponse
from app.services.extract_service import ExtractService

router = APIRouter(prefix="/extract", tags=["extract"])


def get_extract_service() -> ExtractService:
    return ExtractService()


@router.get("", response_model=ExtractResponse, status_code=status.HTTP_200_OK)
async def extract_website(
    request: Request,
    url: str,
    service: ExtractService = Depends(get_extract_service)
) -> ExtractResponse:
    try:
        import logging

        logger = logging.getLogger(__name__)
        logger.info(json.dumps({"event": "route_entry", "method": "GET", "path": str(request.url.path)}))
        payload = ExtractRequest(url=url)
        return service.extract(payload)
    except HTTPException:
        raise
    except Exception as exc:
        import logging

        logger = logging.getLogger(__name__)
        logger.error(json.dumps({"event": "route_error", "message": str(exc)}))
        raise HTTPException(status_code=status.HTTP_500_INTERNAL_SERVER_ERROR, detail="Internal server error") from exc

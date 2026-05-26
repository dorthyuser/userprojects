from __future__ import annotations

import logging
from fastapi import APIRouter, Depends, Header, HTTPException, Request, status

from app.schemas.travelcard_schema import CreateTravelcardRequest, CreateTravelcardResponse
from app.services.travelcard_service import TravelcardService, get_travelcard_service

router = APIRouter(prefix="", tags=["travelcards"])
logger = logging.getLogger(__name__)


@router.post("/travelcards", response_model=CreateTravelcardResponse, status_code=status.HTTP_201_CREATED)
async def create_travelcard(
    request: Request,
    payload: CreateTravelcardRequest,
    client_id: str = Header(..., alias="client_id", min_length=1, max_length=128),
    x_correlation_cust_id: str | None = Header(None, alias="X-Correlation-Cust-Id", max_length=100),
    service: TravelcardService = Depends(get_travelcard_service),
) -> CreateTravelcardResponse:
    logger.info(
        __import__("json").dumps(
            {
                "event": "route_entry",
                "method": request.method,
                "path": str(request.url.path),
                "client_id_present": bool(client_id),
                "correlation_id_present": bool(x_correlation_cust_id),
            }
        )
    )
    if request.headers.get("content-type") != "application/json":
        raise HTTPException(status_code=status.HTTP_415_UNSUPPORTED_MEDIA_TYPE, detail="Unsupported content type")
    return service.create_travelcard(payload=payload, client_id=client_id, correlation_id=x_correlation_cust_id)
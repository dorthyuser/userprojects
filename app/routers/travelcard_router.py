import json
import logging
from fastapi import APIRouter, Header, Request
from app.schemas.travelcard_schema import TravelcardCreateRequest, TravelcardCreateResponse
from app.services.travelcard_service import create_travelcard

logger = logging.getLogger(__name__)
router = APIRouter(prefix="/travelcards", tags=["travelcards"])


@router.post("", response_model=TravelcardCreateResponse, status_code=201)
async def create_travelcard_route(
    request: Request,
    payload: TravelcardCreateRequest,
    client_id: str = Header(..., alias="client_id"),
    x_correlation_cust_id: str | None = Header(None, alias="X-Correlation-Cust-Id")
) -> TravelcardCreateResponse:
    logger.info(json.dumps({"event": "route_entry", "method": request.method, "path": str(request.url.path), "client_id": client_id}))
    return create_travelcard(payload=payload, client_id=client_id, x_correlation_cust_id=x_correlation_cust_id)

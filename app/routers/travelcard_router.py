import json
import logging
from fastapi import APIRouter, Header
from app.schemas.travelcard_schema import TravelCardCreateRequest, TravelCardCreateResponse
from app.services.travelcard_service import create_travelcard

logger = logging.getLogger(__name__)
router = APIRouter()


@router.post("/travelcards", response_model=TravelCardCreateResponse, status_code=201)
async def create_travelcard_route(
    payload: TravelCardCreateRequest,
    client_id: str = Header(..., alias="client_id"),
    x_correlation_cust_id: str | None = Header(None, alias="X-Correlation-Cust-Id")
) -> TravelCardCreateResponse:
    logger.info(json.dumps({"event": "route_entry", "method": "POST", "path": "/travelcards", "resource": "travelcard", "client_id_present": bool(client_id), "correlation_id_present": bool(x_correlation_cust_id)}))
    return create_travelcard(payload, client_id=client_id, correlation_id=x_correlation_cust_id)
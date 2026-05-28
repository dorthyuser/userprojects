import json
import logging

from fastapi import APIRouter, Header, Request
from fastapi.responses import JSONResponse

from app.schemas.travelcard_schema import TravelcardCreateRequest, TravelcardCreateResponse
from app.services.travelcard_service import create_travelcard

router = APIRouter(prefix="/travelcards", tags=["travelcards"])
logger = logging.getLogger(__name__)


@router.post("", response_model=TravelcardCreateResponse, status_code=201)
async def create_travelcard_route(
    request: Request,
    payload: TravelcardCreateRequest,
    client_id: str = Header(..., alias="client_id"),
    content_type: str | None = Header(default=None, alias="Content-Type"),
    x_correlation_cust_id: str | None = Header(default=None, alias="X-Correlation-Cust-Id")
) -> JSONResponse:
    logger.info(json.dumps({"event": "route_entry", "method": request.method, "path": str(request.url.path), "resource": "travelcard"}))
    response = create_travelcard(
        payload=payload,
        client_id=client_id,
        content_type=content_type,
        x_correlation_cust_id=x_correlation_cust_id
    )
    return JSONResponse(status_code=201, content=response.model_dump())
import json
import logging

from fastapi import APIRouter, Header, HTTPException, Request, status

from app.schemas.travelcard_schema import TravelcardCreateRequest, TravelcardCreateResponse
from app.services.travelcard_service import create_travelcard

logger = logging.getLogger(__name__)
router = APIRouter(prefix="/travelcards", tags=["travelcards"])


@router.post("", response_model=TravelcardCreateResponse, status_code=status.HTTP_201_CREATED)
async def create_travelcard_route(
    request: Request,
    payload: TravelcardCreateRequest,
    client_id: str = Header(..., alias="client_id"),
    x_correlation_cust_id: str | None = Header(None, alias="X-Correlation-Cust-Id"),
) -> TravelcardCreateResponse:
    logger.info(json.dumps({"event": "route_entry", "method": request.method, "path": str(request.url.path), "resource": "travelcards"}))
    if not (1 <= len(client_id) <= 128):
        logger.error("ERROR client_id length validation failed")
        raise HTTPException(status_code=422, detail="Validation Error")
    if not __import__("re").fullmatch(r"^[\w+]+$", client_id):
        logger.error("ERROR client_id pattern validation failed")
        raise HTTPException(status_code=422, detail="Validation Error")
    if x_correlation_cust_id is not None:
        if len(x_correlation_cust_id) > 100:
            logger.error("ERROR X-Correlation-Cust-Id length validation failed")
            raise HTTPException(status_code=422, detail="Validation Error")
        if not __import__("re").fullmatch(r"^[A-Za-z0-9_-]+$", x_correlation_cust_id):
            logger.error("ERROR X-Correlation-Cust-Id pattern validation failed")
            raise HTTPException(status_code=422, detail="Validation Error")
    return create_travelcard(payload)

import logging

from fastapi import APIRouter, Header, HTTPException, Request, status

from app.schemas.travelcard_schema import TravelcardCreateRequest, TravelcardsListResponse, TravelcardCreateResponse
from app.services.travelcard_service import TravelcardService

router = APIRouter(prefix="/travelcards", tags=["travelcards"])
logger = logging.getLogger(__name__)
service = TravelcardService()


@router.get("", response_model=TravelcardsListResponse)
async def get_travelcards(
    request: Request,
    client_id: str = Header(..., min_length=1, max_length=128, alias="client_id"),
    x_correlation_cust_id: str | None = Header(None, max_length=100, alias="X-Correlation-Cust-Id")
) -> TravelcardsListResponse:
    logger.info(
        "route_entry",
        extra={"method": request.method, "path": str(request.url.path), "client_id_present": bool(client_id)}
    )
    return service.get_all_travelcards()


@router.post("", response_model=TravelcardCreateResponse, status_code=status.HTTP_201_CREATED)
async def create_travelcard(
    request: Request,
    payload: TravelcardCreateRequest,
    client_id: str = Header(..., min_length=1, max_length=128, alias="client_id"),
    x_correlation_cust_id: str | None = Header(None, max_length=100, alias="X-Correlation-Cust-Id")
) -> TravelcardCreateResponse:
    logger.info(
        "route_entry",
        extra={"method": request.method, "path": str(request.url.path), "client_id_present": bool(client_id)}
    )
    try:
        return service.create_travelcard(payload)
    except ValueError as exc:
        raise HTTPException(status_code=status.HTTP_422_UNPROCESSABLE_ENTITY, detail=str(exc)) from exc

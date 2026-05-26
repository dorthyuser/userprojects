import logging
from typing import Annotated

from fastapi import APIRouter, Header, Request, status

from app.schemas.travelcard_schema import CreateTravelcardRequest, TravelcardsResponse
from app.services.travelcard_service import TravelcardService

router = APIRouter(prefix="", tags=["travelcards"])
logger = logging.getLogger(__name__)
service = TravelcardService()


@router.get("/travelcards", response_model=TravelcardsResponse)
def get_travelcards(
    request: Request,
    client_id: Annotated[str, Header(alias="client_id")],
    x_correlation_cust_id: Annotated[str | None, Header(alias="X-Correlation-Cust-Id")] = None
) -> TravelcardsResponse:
    logger.info(
        "route_entry",
        extra={
            "method": request.method,
            "path": request.url.path,
            "client_id_present": bool(client_id),
            "correlation_id_present": bool(x_correlation_cust_id)
        }
    )
    return service.get_travelcards(client_id=client_id)


@router.post("/travelcards", response_model=dict, status_code=status.HTTP_201_CREATED)
def create_travelcard(
    request: Request,
    payload: CreateTravelcardRequest,
    client_id: Annotated[str, Header(alias="client_id")],
    x_correlation_cust_id: Annotated[str | None, Header(alias="X-Correlation-Cust-Id")] = None
) -> dict:
    logger.info(
        "route_entry",
        extra={
            "method": request.method,
            "path": request.url.path,
            "client_id_present": bool(client_id),
            "correlation_id_present": bool(x_correlation_cust_id)
        }
    )
    return service.create_travelcard(client_id=client_id, payload=payload)

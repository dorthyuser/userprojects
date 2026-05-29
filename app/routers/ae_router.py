import json
import logging

from fastapi import APIRouter, Depends, Request, status

from app.schemas.ae_schema import (
    AENotificationsResponse,
    AEReportRequest,
    AEReportResponse,
    NotificationsQueryParams,
)
from app.services.ae_service import AEService, get_ae_service

logger = logging.getLogger(__name__)
router = APIRouter(prefix="/v1/adverse-events", tags=["adverse-events"])


@router.post("", response_model=AEReportResponse, status_code=status.HTTP_201_CREATED)
def submit_adverse_event(
    request: Request,
    payload: AEReportRequest,
    service: AEService = Depends(get_ae_service),
) -> AEReportResponse:
    logger.info(
        json.dumps(
            {
                "event": "route_entry",
                "method": request.method,
                "path": request.url.path,
                "resource": "adverse-event-submission",
            }
        )
    )
    return service.submit_adverse_event(payload)


@router.get("/notifications", response_model=AENotificationsResponse)
def get_notifications(
    request: Request,
    params: NotificationsQueryParams = Depends(),
    service: AEService = Depends(get_ae_service),
) -> AENotificationsResponse:
    logger.info(
        json.dumps(
            {
                "event": "route_entry",
                "method": request.method,
                "path": request.url.path,
                "resource": "adverse-event-notifications",
            }
        )
    )
    return service.get_notifications(params)
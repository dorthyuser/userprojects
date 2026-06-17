import json
import logging

from fastapi import APIRouter, Depends, Query, Request, status

from app.schemas.adverse_events_schema import (
    AdverseEventCreateRequest,
    AdverseEventCreateResponse,
    NotificationListResponse,
    NotificationQueryParams
)
from app.services.adverse_events_service import AdverseEventsService, get_adverse_events_service

logger = logging.getLogger(__name__)
router = APIRouter(prefix="/v1/adverse-events", tags=["adverse-events"])


@router.post("", response_model=AdverseEventCreateResponse, status_code=status.HTTP_201_CREATED)
def submit_adverse_event(
    request: Request,
    payload: AdverseEventCreateRequest,
    service: AdverseEventsService = Depends(get_adverse_events_service)
) -> AdverseEventCreateResponse:
    logger.info(json.dumps({"event": "route_entry", "method": "POST", "path": str(request.url.path)}))
    return service.submit_adverse_event(payload)


@router.get("/notifications", response_model=NotificationListResponse)
def get_notifications(
    request: Request,
    trialId: str | None = Query(default=None),
    siteId: str | None = Query(default=None),
    ctcaeGrade: int | None = Query(default=None),
    serious: bool | None = Query(default=None),
    acknowledged: bool | None = Query(default=None),
    priority: str | None = Query(default=None),
    dateFrom: str | None = Query(default=None),
    dateTo: str | None = Query(default=None),
    page: int = Query(default=1),
    pageSize: int = Query(default=20),
    service: AdverseEventsService = Depends(get_adverse_events_service)
) -> NotificationListResponse:
    logger.info(json.dumps({"event": "route_entry", "method": "GET", "path": str(request.url.path)}))
    params = NotificationQueryParams(
        trialId=trialId,
        siteId=siteId,
        ctcaeGrade=ctcaeGrade,
        serious=serious,
        acknowledged=acknowledged,
        priority=priority,
        dateFrom=dateFrom,
        dateTo=dateTo,
        page=page,
        pageSize=pageSize
    )
    return service.get_notifications(params)
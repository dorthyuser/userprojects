import json
import logging

from fastapi import APIRouter, Depends, Query, Request, status

from app.schemas.adverse_events_schema import (
    AdverseEventCreateRequest,
    AdverseEventCreateResponse,
    NotificationListResponse,
)
from app.services.adverse_events_service import AdverseEventsService, get_adverse_events_service

logger = logging.getLogger(__name__)
router = APIRouter(prefix="/v1/adverse-events", tags=["adverse-events"])


@router.post("", response_model=AdverseEventCreateResponse, status_code=status.HTTP_201_CREATED)
def submit_adverse_event(
    request: Request,
    payload: AdverseEventCreateRequest,
    service: AdverseEventsService = Depends(get_adverse_events_service),
) -> AdverseEventCreateResponse:
    logger.info(json.dumps({"event": "route_entry", "method": request.method, "path": str(request.url.path), "resource": "adverse-events"}))
    return service.submit_adverse_event(payload)


@router.get("/notifications", response_model=NotificationListResponse)
def get_notifications(
    request: Request,
    trialId: str | None = Query(default=None),
    siteId: str | None = Query(default=None),
    ctcaeGrade: int | None = Query(default=None, ge=1, le=5),
    serious: bool | None = Query(default=None),
    acknowledged: bool | None = Query(default=None),
    priority: str | None = Query(default=None),
    dateFrom: str | None = Query(default=None),
    dateTo: str | None = Query(default=None),
    page: int = Query(default=1, ge=1),
    pageSize: int = Query(default=20, ge=1, le=100),
    service: AdverseEventsService = Depends(get_adverse_events_service),
) -> NotificationListResponse:
    logger.info(json.dumps({"event": "route_entry", "method": request.method, "path": str(request.url.path), "resource": "adverse-events-notifications"}))
    return service.get_notifications(
        trial_id=trialId,
        site_id=siteId,
        ctcae_grade=ctcaeGrade,
        serious=serious,
        acknowledged=acknowledged,
        priority=priority,
        date_from=dateFrom,
        date_to=dateTo,
        page=page,
        page_size=pageSize,
    )
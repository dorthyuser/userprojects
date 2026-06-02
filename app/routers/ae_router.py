import json
import logging
from fastapi import APIRouter, Depends, Request, status
from app.schemas.ae_schema import AdverseEventCreateRequest, NotificationListResponse, AdverseEventCreateResponse
from app.services.ae_service import create_adverse_event, get_notifications

logger = logging.getLogger(__name__)
router = APIRouter(prefix="/v1/adverse-events", tags=["adverse-events"])


@router.post("", response_model=AdverseEventCreateResponse, status_code=status.HTTP_201_CREATED)
def submit_adverse_event(request: Request, payload: AdverseEventCreateRequest) -> AdverseEventCreateResponse:
    logger.info(json.dumps({"event": "route_entry", "method": request.method, "path": str(request.url.path), "resource": "adverse-events"}))
    return create_adverse_event(payload)


@router.get("/notifications", response_model=NotificationListResponse)
def retrieve_notifications(request: Request, trialId: str | None = None, siteId: str | None = None, ctcaeGrade: int | None = None, serious: bool | None = None, acknowledged: bool | None = None, priority: str | None = None, dateFrom: str | None = None, dateTo: str | None = None, page: int = 1, pageSize: int = 20) -> NotificationListResponse:
    logger.info(json.dumps({"event": "route_entry", "method": request.method, "path": str(request.url.path), "resource": "notifications"}))
    return get_notifications(trialId=trialId, siteId=siteId, ctcaeGrade=ctcaeGrade, serious=serious, acknowledged=acknowledged, priority=priority, dateFrom=dateFrom, dateTo=dateTo, page=page, pageSize=pageSize)

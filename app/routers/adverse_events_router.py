import json
import logging

from fastapi import APIRouter, Request, status

from app.schemas.adverse_events_schema import AdverseEventCreateRequest, AdverseEventCreateResponse, NotificationListResponse
from app.services.adverse_events_service import AdverseEventService

logger = logging.getLogger(__name__)
router = APIRouter(prefix="/v1/adverse-events", tags=["adverse-events"])
service = AdverseEventService()


@router.post("", response_model=AdverseEventCreateResponse, status_code=status.HTTP_201_CREATED)
async def create_adverse_event(request: Request, payload: AdverseEventCreateRequest) -> AdverseEventCreateResponse:
    logger.info(json.dumps({"method": request.method, "path": str(request.url.path), "step": "ROUTE_ENTRY", "outcome": "SUCCESS"}))
    return service.create_adverse_event(payload)


@router.get("/notifications", response_model=NotificationListResponse)
async def list_notifications(request: Request, trialId: str | None = None, siteId: str | None = None, ctcaeGrade: int | None = None, serious: bool | None = None, acknowledged: bool | None = None, priority: str | None = None, dateFrom: str | None = None, dateTo: str | None = None, page: int = 1, pageSize: int = 20) -> NotificationListResponse:
    logger.info(json.dumps({"method": request.method, "path": str(request.url.path), "step": "ROUTE_ENTRY", "outcome": "SUCCESS"}))
    return service.list_notifications(trialId=trialId, siteId=siteId, ctcaeGrade=ctcaeGrade, serious=serious, acknowledged=acknowledged, priority=priority, dateFrom=dateFrom, dateTo=dateTo, page=page, pageSize=pageSize)

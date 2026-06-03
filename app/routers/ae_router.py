import json
import logging

from fastapi import APIRouter, Depends, Query, Request, status

from app.schemas.ae_schema import AdverseEventCreateRequest, NotificationListResponse, AdverseEventCreateResponse
from app.services.ae_service import AEService, get_ae_service

logger = logging.getLogger(__name__)
router = APIRouter(prefix="/v1/adverse-events", tags=["adverse-events"])


@router.post("", response_model=AdverseEventCreateResponse, status_code=status.HTTP_201_CREATED)
def submit_adverse_event(
    payload: AdverseEventCreateRequest,
    request: Request,
    service: AEService = Depends(get_ae_service),
) -> AdverseEventCreateResponse:
    logger.info(json.dumps({"event": "route_entry", "method": request.method, "path": str(request.url.path), "resource": "adverse-events"}))
    return service.submit_adverse_event(payload)


@router.get("/notifications", response_model=NotificationListResponse, status_code=status.HTTP_200_OK)
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
    service: AEService = Depends(get_ae_service),
) -> NotificationListResponse:
    logger.info(json.dumps({"event": "route_entry", "method": request.method, "path": str(request.url.path), "resource": "adverse-events-notifications"}))
    return service.get_notifications(
        trialId=trialId,
        siteId=siteId,
        ctcaeGrade=ctcaeGrade,
        serious=serious,
        acknowledged=acknowledged,
        priority=priority,
        dateFrom=dateFrom,
        dateTo=dateTo,
        page=page,
        pageSize=pageSize,
    )

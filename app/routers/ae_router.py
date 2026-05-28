import json
import logging

from fastapi import APIRouter, Query, status

from app.schemas.ae_schema import AECreateRequest, AECreateResponse, AENotificationsResponse, NotificationQueryParams
from app.services.ae_service import create_adverse_event, list_notifications

logger = logging.getLogger(__name__)
router = APIRouter(prefix="/v1/adverse-events", tags=["adverse-events"])


@router.post("", response_model=AECreateResponse, status_code=status.HTTP_201_CREATED)
def submit_adverse_event(payload: AECreateRequest) -> AECreateResponse:
    logger.info(json.dumps({"event": "route_entry", "method": "POST", "path": "/v1/adverse-events", "trialId": payload.trialId, "siteId": payload.siteId, "patientId": payload.patientId}))
    return create_adverse_event(payload)


@router.get("/notifications", response_model=AENotificationsResponse, status_code=status.HTTP_200_OK)
def get_notifications(
    trialId: str | None = Query(default=None),
    siteId: str | None = Query(default=None),
    ctcaeGrade: int | None = Query(default=None),
    serious: bool | None = Query(default=None),
    acknowledged: bool | None = Query(default=None),
    priority: str | None = Query(default=None),
    dateFrom: str | None = Query(default=None),
    dateTo: str | None = Query(default=None),
    page: int = Query(default=1),
    pageSize: int = Query(default=20)
) -> AENotificationsResponse:
    logger.info(json.dumps({"event": "route_entry", "method": "GET", "path": "/v1/adverse-events/notifications"}))
    params = NotificationQueryParams(trialId=trialId, siteId=siteId, ctcaeGrade=ctcaeGrade, serious=serious, acknowledged=acknowledged, priority=priority, dateFrom=dateFrom, dateTo=dateTo, page=page, pageSize=pageSize)
    return list_notifications(params)

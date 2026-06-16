import json
import logging

from fastapi import APIRouter, HTTPException, Request, status

from app.schemas.adverse_events_schema import (
    AdverseEventCreateRequest,
    AdverseEventCreateResponse,
    NotificationListResponse,
)
from app.services.adverse_events_service import (
    create_adverse_event,
    get_notifications,
)

router = APIRouter(prefix="/v1/adverse-events", tags=["adverse-events"])
logger = logging.getLogger(__name__)


@router.post("", response_model=AdverseEventCreateResponse, status_code=status.HTTP_201_CREATED)
def submit_adverse_event(request: Request, payload: AdverseEventCreateRequest) -> AdverseEventCreateResponse:
    logger.info(json.dumps({"event": "route_entry", "method": request.method, "path": str(request.url.path), "resource": "adverse-events"}))
    try:
        return create_adverse_event(payload)
    except HTTPException:
        raise
    except Exception as exc:
        logger.error(json.dumps({"event": "route_error", "message": str(exc)}), exc_info=True)
        raise HTTPException(status_code=500, detail="Internal Error") from exc


@router.get("/notifications", response_model=NotificationListResponse)
def retrieve_notifications(request: Request, trialId: str | None = None, siteId: str | None = None, ctcaeGrade: int | None = None, serious: bool | None = None, acknowledged: bool | None = None, priority: str | None = None, dateFrom: str | None = None, dateTo: str | None = None, page: int = 1, pageSize: int = 20) -> NotificationListResponse:
    logger.info(json.dumps({"event": "route_entry", "method": request.method, "path": str(request.url.path), "resource": "adverse-events-notifications"}))
    try:
        return get_notifications(trialId, siteId, ctcaeGrade, serious, acknowledged, priority, dateFrom, dateTo, page, pageSize)
    except HTTPException:
        raise
    except Exception as exc:
        logger.error(json.dumps({"event": "route_error", "message": str(exc)}), exc_info=True)
        raise HTTPException(status_code=500, detail="Internal Error") from exc

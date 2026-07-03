import json
import logging

from fastapi import APIRouter, HTTPException, Request, status

from app.schemas.adverse_events_schema import (
    AdverseEventCreateRequest,
    AdverseEventCreateResponse,
    NotificationListResponse,
)
from app.services.adverse_events_service import (
    get_notifications_service,
    submit_adverse_event_service,
)

logger = logging.getLogger(__name__)
router = APIRouter(prefix="/v1/adverse-events", tags=["adverse-events"])


@router.post("", response_model=AdverseEventCreateResponse, status_code=status.HTTP_201_CREATED)
async def submit_adverse_event(request: Request, payload: AdverseEventCreateRequest) -> AdverseEventCreateResponse:
    logger.info(json.dumps({"event": "route_entry", "method": request.method, "path": str(request.url.path), "resource": "adverse-events"}))
    try:
        return submit_adverse_event_service(payload)
    except HTTPException:
        raise
    except Exception as exc:
        logger.error(json.dumps({"event": "route_error", "message": str(exc), "resource": "adverse-events"}))
        raise HTTPException(status_code=500, detail="Internal Error") from exc


@router.get("/notifications", response_model=NotificationListResponse)
async def get_notifications(request: Request) -> NotificationListResponse:
    logger.info(json.dumps({"event": "route_entry", "method": request.method, "path": str(request.url.path), "resource": "notifications"}))
    try:
        return get_notifications_service(dict(request.query_params))
    except HTTPException:
        raise
    except Exception as exc:
        logger.error(json.dumps({"event": "route_error", "message": str(exc), "resource": "notifications"}))
        raise HTTPException(status_code=500, detail="Internal Error") from exc

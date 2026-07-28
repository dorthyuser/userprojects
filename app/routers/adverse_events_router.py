import json
import logging

from fastapi import APIRouter, HTTPException, Request, status

from app.schemas.adverse_event_schema import (
    AdverseEventCreateRequest,
    AdverseEventCreateResponse,
    NotificationListResponse,
)
from app.services.adverse_event_service import (
    create_adverse_event,
    get_notifications,
)

router = APIRouter(prefix="/v1/adverse-events", tags=["adverse-events"])
logger = logging.getLogger(__name__)


@router.post("", response_model=AdverseEventCreateResponse, status_code=status.HTTP_201_CREATED)
def submit_adverse_event(request: Request, payload: AdverseEventCreateRequest) -> AdverseEventCreateResponse:
    logger.info(
        json.dumps(
            {
                "event": "route_entry",
                "method": request.method,
                "path": str(request.url.path),
            }
        )
    )
    try:
        return create_adverse_event(payload=payload)
    except HTTPException:
        raise
    except Exception as exc:
        logger.error("Unexpected error: %s", str(exc), exc_info=True)
        raise HTTPException(status_code=500, detail="Internal Error") from exc


@router.get("/notifications", response_model=NotificationListResponse)
def list_notifications(request: Request, trialId: str | None = None, siteId: str | None = None, ctcaeGrade: int | None = None, serious: bool | None = None, acknowledged: bool | None = None, priority: str | None = None, dateFrom: str | None = None, dateTo: str | None = None, page: int = 1, pageSize: int = 20) -> NotificationListResponse:
    logger.info(
        json.dumps(
            {
                "event": "route_entry",
                "method": request.method,
                "path": str(request.url.path),
            }
        )
    )
    try:
        return get_notifications(
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
    except HTTPException:
        raise
    except Exception as exc:
        logger.error("Unexpected error: %s", str(exc), exc_info=True)
        raise HTTPException(status_code=500, detail="Internal Error") from exc

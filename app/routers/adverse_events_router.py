import json
import logging
from typing import Annotated

from fastapi import APIRouter, Depends, Request, status

from app.schemas.adverse_event_schema import (
    AdverseEventCreateRequest,
    AdverseEventCreateResponse,
    NotificationListResponse,
)
from app.services.adverse_event_service import (
    create_adverse_event,
    get_notifications,
)

logger = logging.getLogger(__name__)
router = APIRouter(prefix="/v1/adverse-events", tags=["adverse-events"])


@router.post("", response_model=AdverseEventCreateResponse, status_code=status.HTTP_201_CREATED)
def submit_adverse_event(
    request: Request,
    payload: AdverseEventCreateRequest,
) -> AdverseEventCreateResponse:
    logger.info(
        json.dumps(
            {
                "event": "route_entry",
                "method": request.method,
                "path": request.url.path,
                "resource": "adverse-events",
            }
        )
    )
    return create_adverse_event(payload=payload, request_id=getattr(request.state, "request_id", "unknown"))


@router.get("/notifications", response_model=NotificationListResponse)
def list_notifications(
    request: Request,
    trialId: str | None = None,
    siteId: str | None = None,
    ctcaeGrade: int | None = None,
    serious: bool | None = None,
    acknowledged: bool | None = None,
    priority: str | None = None,
    dateFrom: str | None = None,
    dateTo: str | None = None,
    page: int = 1,
    pageSize: int = 20,
) -> NotificationListResponse:
    logger.info(
        json.dumps(
            {
                "event": "route_entry",
                "method": request.method,
                "path": request.url.path,
                "resource": "adverse-events-notifications",
            }
        )
    )
    return get_notifications(
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
        request_id=getattr(request.state, "request_id", "unknown"),
    )

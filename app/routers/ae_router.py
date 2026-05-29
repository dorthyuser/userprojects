from __future__ import annotations

import json
import logging
from typing import Any

from fastapi import APIRouter, Depends, Header, HTTPException, Request, status

from app.schemas.ae_schema import AdverseEventCreateRequest, NotificationListResponse, SubmitAdverseEventResponse
from app.services.ae_service import AdverseEventService, get_ae_service

logger = logging.getLogger(__name__)
router = APIRouter(prefix="/v1/adverse-events", tags=["adverse-events"])


@router.post("", response_model=SubmitAdverseEventResponse, status_code=status.HTTP_201_CREATED)
def submit_adverse_event(
    payload: AdverseEventCreateRequest,
    request: Request,
    service: AdverseEventService = Depends(get_ae_service),
    content_type: str | None = Header(default=None, alias="Content-Type"),
) -> SubmitAdverseEventResponse:
    if content_type is None or "application/json" not in content_type.lower():
        raise HTTPException(status_code=422, detail="Content-Type must include application/json")
    logger.info(json.dumps({"event": "route_entry", "method": request.method, "path": str(request.url.path), "resource": "adverse-events", "identifier": payload.trialId}))
    return service.submit_adverse_event(payload)


@router.get("/notifications", response_model=NotificationListResponse)
def get_notifications(
    request: Request,
    trial_id: str | None = None,
    site_id: str | None = None,
    ctcae_grade: int | None = None,
    serious: bool | None = None,
    acknowledged: bool | None = None,
    priority: str | None = None,
    date_from: str | None = None,
    date_to: str | None = None,
    page: int = 1,
    page_size: int = 20,
    service: AdverseEventService = Depends(get_ae_service),
) -> NotificationListResponse:
    logger.info(json.dumps({"event": "route_entry", "method": request.method, "path": str(request.url.path), "resource": "adverse-events-notifications"}))
    return service.get_notifications(
        trialId=trial_id,
        siteId=site_id,
        ctcaeGrade=ctcae_grade,
        serious=serious,
        acknowledged=acknowledged,
        priority=priority,
        dateFrom=date_from,
        dateTo=date_to,
        page=page,
        pageSize=page_size,
    )

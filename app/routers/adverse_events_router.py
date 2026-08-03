from fastapi import APIRouter, HTTPException, Request, status

from app.schemas.adverse_event_schema import (
    AdverseEventCreateRequest,
    AdverseEventCreateResponse,
    NotificationListResponse,
)
from app.services.adverse_event_service import (
    create_adverse_event,
    list_notifications,
)

router = APIRouter(prefix="/v1/adverse-events", tags=["adverse-events"])


@router.post("", response_model=AdverseEventCreateResponse, status_code=status.HTTP_201_CREATED)
async def submit_adverse_event(request: Request, payload: AdverseEventCreateRequest) -> AdverseEventCreateResponse:
    try:
        return create_adverse_event(request, payload)
    except HTTPException:
        raise


@router.get("/notifications", response_model=NotificationListResponse)
async def get_notifications(request: Request, trialId: str | None = None, siteId: str | None = None, ctcaeGrade: int | None = None, serious: bool | None = None, acknowledged: bool | None = None, priority: str | None = None, dateFrom: str | None = None, dateTo: str | None = None, page: int = 1, pageSize: int = 20) -> NotificationListResponse:
    try:
        return list_notifications(request, trialId, siteId, ctcaeGrade, serious, acknowledged, priority, dateFrom, dateTo, page, pageSize)
    except HTTPException:
        raise

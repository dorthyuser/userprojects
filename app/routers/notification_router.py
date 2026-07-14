from fastapi import APIRouter, HTTPException, Query, Request, status

from app.schemas.notification_schema import NotificationListResponse
from app.services.notification_service import get_notifications

router = APIRouter(prefix="/v1/adverse-events/notifications", tags=["notifications"])


@router.get("", response_model=NotificationListResponse, status_code=status.HTTP_200_OK)
def list_notifications(
    request: Request,
    trialId: str | None = Query(default=None),
    siteId: str | None = Query(default=None),
    ctcaeGrade: int | None = Query(default=None),
    serious: bool | None = Query(default=None),
    acknowledged: bool | None = Query(default=None),
    priority: str | None = Query(default=None),
    dateFrom: str | None = Query(default=None),
    dateTo: str | None = Query(default=None),
    page: int = Query(default=1),
    pageSize: int = Query(default=20),
) -> NotificationListResponse:
    try:
        return get_notifications(
            request_id=getattr(request.state, "request_id", None),
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

from fastapi import APIRouter, Request

from app.schemas.notification_schema import NotificationListResponse, NotificationQueryParams
from app.services.notification_service import list_notifications

router = APIRouter(prefix="/v1/adverse-events/notifications", tags=["notifications"])


@router.get("", response_model=NotificationListResponse)
async def get_notifications(request: Request, params: NotificationQueryParams = NotificationQueryParams()) -> NotificationListResponse:
    return list_notifications(params, request)

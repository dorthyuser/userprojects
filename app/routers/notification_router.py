from fastapi import APIRouter, Request, status

from app.schemas.notification_schema import NotificationListResponse, NotificationQueryParams
from app.services.notification_service import list_notifications

router = APIRouter(prefix="/v1/adverse-events/notifications", tags=["notifications"])


@router.get("", response_model=NotificationListResponse, status_code=status.HTTP_200_OK)
def get_notifications(request: Request, params: NotificationQueryParams = Depends()) -> NotificationListResponse:
    return list_notifications(params=params, request=request)

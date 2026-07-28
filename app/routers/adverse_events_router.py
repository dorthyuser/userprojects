from fastapi import APIRouter, HTTPException, Request, status

from app.schemas.adverse_events_schema import AdverseEventCreateRequest, NotificationListResponse
from app.services.adverse_events_service import AdverseEventService

router = APIRouter(prefix="/v1/adverse-events", tags=["adverse-events"])
service = AdverseEventService()


@router.post("", response_model=object, status_code=status.HTTP_201_CREATED)
def create_adverse_event(request: Request, payload: AdverseEventCreateRequest) -> object:
    try:
        return service.create_adverse_event(request=request, payload=payload)
    except HTTPException:
        raise
    except Exception:
        raise HTTPException(status_code=status.HTTP_500_INTERNAL_SERVER_ERROR, detail="Internal Error")


@router.get("/notifications", response_model=NotificationListResponse)
def list_notifications(request: Request, trialId: str | None = None, siteId: str | None = None, ctcaeGrade: int | None = None, serious: bool | None = None, acknowledged: bool | None = None, priority: str | None = None, dateFrom: str | None = None, dateTo: str | None = None, page: int = 1, pageSize: int = 20) -> NotificationListResponse:
    try:
        return service.list_notifications(request=request, trialId=trialId, siteId=siteId, ctcaeGrade=ctcaeGrade, serious=serious, acknowledged=acknowledged, priority=priority, dateFrom=dateFrom, dateTo=dateTo, page=page, pageSize=pageSize)
    except HTTPException:
        raise
    except Exception:
        raise HTTPException(status_code=status.HTTP_500_INTERNAL_SERVER_ERROR, detail="Internal Error")

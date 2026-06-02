from fastapi import APIRouter, Query

from app.schemas.ae_schema import AdverseEventCreateRequest, NotificationListResponse, AdverseEventCreateResponse
from app.services.ae_service import create_adverse_event, get_notifications

router = APIRouter(prefix="/v1/adverse-events", tags=["adverse-events"])


@router.post("", response_model=AdverseEventCreateResponse, status_code=201)
def submit_adverse_event(payload: AdverseEventCreateRequest) -> AdverseEventCreateResponse:
    return create_adverse_event(payload)


@router.get("/notifications", response_model=NotificationListResponse)
def retrieve_notifications(
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

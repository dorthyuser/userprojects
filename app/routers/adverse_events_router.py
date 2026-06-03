from fastapi import APIRouter, Query, Request

from app.schemas.adverse_events_schema import AdverseEventCreateRequest
from app.services.adverse_events_service import (
    create_adverse_event,
    get_notifications,
)

router = APIRouter(prefix="/v1/adverse-events", tags=["adverse-events"])


@router.post("", status_code=201)
def submit_adverse_event(request: Request, payload: AdverseEventCreateRequest):
    return create_adverse_event(request, payload)


@router.get("/notifications", status_code=200)
def retrieve_notifications(
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
):
    return get_notifications(
        request,
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

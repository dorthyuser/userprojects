from fastapi import APIRouter, Request

from app.schemas.adverse_event_schema import AdverseEventCreateRequest
from app.services.adverse_event_service import (
    get_notifications_service,
    submit_adverse_event_service,
)

router = APIRouter(prefix="/v1/adverse-events", tags=["adverse-events"])


@router.post("", status_code=201)
def submit_adverse_event(request: Request, payload: AdverseEventCreateRequest):
    return submit_adverse_event_service(request=request, payload=payload)


@router.get("/notifications")
def get_notifications(request: Request):
    return get_notifications_service(request=request)

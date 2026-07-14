from fastapi import APIRouter, HTTPException, Request, status

from app.schemas.adverse_event_schema import AdverseEventCreateRequest, AdverseEventCreateResponse
from app.services.adverse_event_service import create_adverse_event

router = APIRouter(prefix="/v1/adverse-events", tags=["adverse-events"])


@router.post("", response_model=AdverseEventCreateResponse, status_code=status.HTTP_201_CREATED)
def submit_adverse_event(request: Request, payload: AdverseEventCreateRequest) -> AdverseEventCreateResponse:
    try:
        return create_adverse_event(payload=payload, request_id=getattr(request.state, "request_id", None))
    except HTTPException:
        raise

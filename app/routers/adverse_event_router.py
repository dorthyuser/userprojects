from fastapi import APIRouter, Request

from app.schemas.adverse_event_schema import AdverseEventCreateRequest, AdverseEventCreateResponse
from app.services.adverse_event_service import submit_adverse_event

router = APIRouter(prefix="/v1/adverse-events", tags=["adverse-events"])


@router.post("", response_model=AdverseEventCreateResponse, status_code=201)
async def create_adverse_event(payload: AdverseEventCreateRequest, request: Request) -> AdverseEventCreateResponse:
    return submit_adverse_event(payload, request)

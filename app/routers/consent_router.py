from fastapi import APIRouter, Header, Query, Request

from app.schemas.consent_schema import (
    ConsentAuditHistoryResponse,
    ConsentGrantRequest,
    ConsentGrantResponse,
    ConsentGetAllResponse,
    ConsentGetOneResponse,
    ConsentWithdrawRequest,
    ConsentWithdrawResponse
)
from app.services.consent_service import (
    get_audit_history_service,
    get_consent_all_service,
    get_consent_one_service,
    grant_consent_service,
    withdraw_consent_service
)

router = APIRouter(prefix="/consent", tags=["consent"])


@router.post("", response_model=ConsentGrantResponse, status_code=201)
def grant_consent(
    request: Request,
    payload: ConsentGrantRequest,
    idempotency_key: str = Header(..., alias="Idempotency-Key")
) -> ConsentGrantResponse:
    headers = dict(request.headers)
    event = request.scope.get("aws.event", {})
    return grant_consent_service(payload, headers, event)


@router.put("/{consent_id}/withdraw", response_model=ConsentWithdrawResponse)
def withdraw_consent(
    request: Request,
    consent_id: str,
    payload: ConsentWithdrawRequest,
    idempotency_key: str = Header(..., alias="Idempotency-Key")
) -> ConsentWithdrawResponse:
    headers = dict(request.headers)
    event = request.scope.get("aws.event", {})
    return withdraw_consent_service(consent_id, payload, headers, event)


@router.get("/{patient_id}", response_model=ConsentGetAllResponse)
def get_consent_all(request: Request, patient_id: str) -> ConsentGetAllResponse:
    event = request.scope.get("aws.event", {})
    return get_consent_all_service(patient_id, event)


@router.get("/{patient_id}/{purpose}", response_model=ConsentGetOneResponse)
def get_consent_one(request: Request, patient_id: str, purpose: str) -> ConsentGetOneResponse:
    event = request.scope.get("aws.event", {})
    return get_consent_one_service(patient_id, purpose, event)


@router.get("/{patient_id}/audit", response_model=ConsentAuditHistoryResponse)
def get_audit_history(
    request: Request,
    patient_id: str,
    limit: int = Query(100, ge=1, le=500),
    offset: int = Query(0, ge=0)
) -> ConsentAuditHistoryResponse:
    event = request.scope.get("aws.event", {})
    return get_audit_history_service(patient_id, limit, offset, event)

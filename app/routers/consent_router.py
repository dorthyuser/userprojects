from fastapi import APIRouter, Header, Query

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
    payload: ConsentGrantRequest,
    idempotency_key: str = Header(..., alias="Idempotency-Key")
) -> ConsentGrantResponse:
    return grant_consent_service(payload, idempotency_key)


@router.put("/{consent_id}/withdraw", response_model=ConsentWithdrawResponse)
def withdraw_consent(
    consent_id: str,
    payload: ConsentWithdrawRequest,
    idempotency_key: str = Header(..., alias="Idempotency-Key")
) -> ConsentWithdrawResponse:
    return withdraw_consent_service(consent_id, payload, idempotency_key)


@router.get("/{patient_id}", response_model=ConsentGetAllResponse)
def get_consent_all(patient_id: str) -> ConsentGetAllResponse:
    return get_consent_all_service(patient_id)


@router.get("/{patient_id}/{purpose}", response_model=ConsentGetOneResponse)
def get_consent_one(patient_id: str, purpose: str) -> ConsentGetOneResponse:
    return get_consent_one_service(patient_id, purpose)


@router.get("/{patient_id}/audit", response_model=ConsentAuditHistoryResponse)
def get_audit_history(
    patient_id: str,
    limit: int = Query(100, ge=1, le=500),
    offset: int = Query(0, ge=0)
) -> ConsentAuditHistoryResponse:
    return get_audit_history_service(patient_id, limit, offset)

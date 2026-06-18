from fastapi import APIRouter, Header, Query, Request, status

from app.schemas.consent_schema import (
    ConsentAuditHistoryResponse,
    ConsentGrantRequest,
    ConsentGrantResponse,
    ConsentListResponse,
    ConsentResponse,
    ConsentWithdrawRequest,
    ConsentWithdrawResponse,
)
from app.services.consent_service import (
    get_audit_history_service,
    get_consent_all_service,
    get_consent_one_service,
    grant_consent_service,
    withdraw_consent_service,
)

router = APIRouter(prefix="/consent", tags=["consent"])


@router.post("", response_model=ConsentGrantResponse, status_code=status.HTTP_201_CREATED)
def grant_consent(
    request: Request,
    payload: ConsentGrantRequest,
    idempotency_key: str = Header(..., alias="Idempotency-Key", max_length=128),
) -> ConsentGrantResponse:
    return grant_consent_service(request=request, payload=payload, idempotency_key=idempotency_key)


@router.put("/{consent_id}/withdraw", response_model=ConsentWithdrawResponse)
def withdraw_consent(
    request: Request,
    consent_id: str,
    payload: ConsentWithdrawRequest,
    idempotency_key: str = Header(..., alias="Idempotency-Key", max_length=128),
) -> ConsentWithdrawResponse:
    return withdraw_consent_service(
        request=request,
        consent_id=consent_id,
        payload=payload,
        idempotency_key=idempotency_key,
    )


@router.get("/{patient_id}", response_model=ConsentListResponse)
def get_consent_all(request: Request, patient_id: str) -> ConsentListResponse:
    return get_consent_all_service(request=request, patient_id=patient_id)


@router.get("/{patient_id}/{purpose}", response_model=ConsentResponse)
def get_consent_one(request: Request, patient_id: str, purpose: str) -> ConsentResponse:
    return get_consent_one_service(request=request, patient_id=patient_id, purpose=purpose)


@router.get("/{patient_id}/audit", response_model=ConsentAuditHistoryResponse)
def get_audit_history(
    request: Request,
    patient_id: str,
    limit: int = Query(100, ge=1, le=500),
    offset: int = Query(0, ge=0),
) -> ConsentAuditHistoryResponse:
    return get_audit_history_service(request=request, patient_id=patient_id, limit=limit, offset=offset)

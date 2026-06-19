from fastapi import APIRouter, Request

from app.schemas.consent_schema import (
    AuditHistoryResponse,
    ConsentByPatientResponse,
    ConsentRecordResponse,
    GrantConsentRequest,
    GrantConsentResponse,
    WithdrawConsentRequest,
    WithdrawConsentResponse,
)
from app.services.consent_service import (
    get_audit_history_service,
    get_consent_all_service,
    get_consent_one_service,
    grant_consent_service,
    withdraw_consent_service,
)

router = APIRouter(prefix="/consent", tags=["consent"])


@router.post("", response_model=GrantConsentResponse, status_code=201)
async def grant_consent(request: Request, payload: GrantConsentRequest) -> GrantConsentResponse:
    headers = dict(request.headers)
    event = request.scope.get("aws.event", {})
    return grant_consent_service(payload=payload, headers=headers, event=event)


@router.put("/{consent_id}/withdraw", response_model=WithdrawConsentResponse)
async def withdraw_consent(request: Request, consent_id: str, payload: WithdrawConsentRequest) -> WithdrawConsentResponse:
    headers = dict(request.headers)
    event = request.scope.get("aws.event", {})
    return withdraw_consent_service(consent_id=consent_id, payload=payload, headers=headers, event=event)


@router.get("/{patient_id}", response_model=ConsentByPatientResponse)
async def get_consent_all(request: Request, patient_id: str) -> ConsentByPatientResponse:
    headers = dict(request.headers)
    event = request.scope.get("aws.event", {})
    return get_consent_all_service(patient_id=patient_id, headers=headers, event=event)


@router.get("/{patient_id}/{purpose}", response_model=ConsentRecordResponse)
async def get_consent_one(request: Request, patient_id: str, purpose: str) -> ConsentRecordResponse:
    headers = dict(request.headers)
    event = request.scope.get("aws.event", {})
    return get_consent_one_service(patient_id=patient_id, purpose=purpose, headers=headers, event=event)


@router.get("/{patient_id}/audit", response_model=AuditHistoryResponse)
async def get_audit_history(request: Request, patient_id: str) -> AuditHistoryResponse:
    headers = dict(request.headers)
    event = request.scope.get("aws.event", {})
    return get_audit_history_service(patient_id=patient_id, headers=headers, event=event)

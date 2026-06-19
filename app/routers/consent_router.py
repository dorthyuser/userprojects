import json
import logging
from typing import Any

from fastapi import APIRouter, Body, Query, Request

from app.schemas.consent_schema import (
    ConsentAuditHistoryResponse,
    ConsentGrantRequest,
    ConsentGrantResponse,
    ConsentRecordResponse,
    ConsentStateResponse,
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

logger = logging.getLogger(__name__)
router = APIRouter(prefix="/consent", tags=["consent"])


@router.post("", response_model=ConsentGrantResponse, status_code=201)
def grant_consent(
    request: Request,
    payload: ConsentGrantRequest = Body(...)
) -> ConsentGrantResponse:
    headers = dict(request.headers)
    event = request.scope.get("aws.event", {})
    logger.info(json.dumps({"event": "route_entry", "method": "POST", "path": "/consent"}))
    return grant_consent_service(payload, headers, event)


@router.get("/{patient_id}", response_model=ConsentStateResponse)
def get_consent_all(
    request: Request,
    patient_id: str
) -> ConsentStateResponse:
    headers = dict(request.headers)
    event = request.scope.get("aws.event", {})
    logger.info(json.dumps({"event": "route_entry", "method": "GET", "path": "/consent/{patient_id}"}))
    return get_consent_all_service(patient_id, headers, event)


@router.get("/{patient_id}/{purpose}", response_model=ConsentRecordResponse)
def get_consent_one(
    request: Request,
    patient_id: str,
    purpose: str
) -> ConsentRecordResponse:
    headers = dict(request.headers)
    event = request.scope.get("aws.event", {})
    logger.info(json.dumps({"event": "route_entry", "method": "GET", "path": "/consent/{patient_id}/{purpose}"}))
    return get_consent_one_service(patient_id, purpose, headers, event)


@router.put("/{consent_id}/withdraw", response_model=ConsentWithdrawResponse)
def withdraw_consent(
    request: Request,
    consent_id: str,
    payload: ConsentWithdrawRequest = Body(default_factory=ConsentWithdrawRequest)
) -> ConsentWithdrawResponse:
    headers = dict(request.headers)
    event = request.scope.get("aws.event", {})
    logger.info(json.dumps({"event": "route_entry", "method": "PUT", "path": "/consent/{consent_id}/withdraw"}))
    return withdraw_consent_service(consent_id, payload, headers, event)


@router.get("/{patient_id}/audit", response_model=ConsentAuditHistoryResponse)
def get_audit_history(
    request: Request,
    patient_id: str,
    limit: int = Query(default=100, ge=1, le=500),
    offset: int = Query(default=0, ge=0)
) -> ConsentAuditHistoryResponse:
    headers = dict(request.headers)
    event = request.scope.get("aws.event", {})
    logger.info(json.dumps({"event": "route_entry", "method": "GET", "path": "/consent/{patient_id}/audit"}))
    return get_audit_history_service(patient_id, limit, offset, headers, event)

import json
import logging
import re
from datetime import datetime, timezone
from typing import Any

from dateutil.parser import isoparse
from fastapi import HTTPException

from app.schemas.adverse_event_schema import AdverseEventCreateRequest

logger = logging.getLogger(__name__)

_ALLOWED_OUTCOMES = {"ONGOING", "RESOLVED", "FATAL", "UNKNOWN"}
_ALLOWED_ACTIONS = {"NONE", "DOSE_REDUCED", "DRUG_WITHDRAWN", "HOSPITALISED"}
_ALLOWED_PRIORITIES = {"HIGH", "NORMAL"}


def validate_adverse_event_payload(payload: AdverseEventCreateRequest) -> AdverseEventCreateRequest:
    required_fields = ["trialId", "siteId", "patientId", "clinicianId", "eventDate", "aeTermCode", "aeTermName", "ctcaeGrade", "serious", "outcome", "actionTaken", "narrative", "reportedBy"]
    for field_name in required_fields:
        if getattr(payload, field_name) is None:
            logger.error(json.dumps({"step": "VALIDATION", "outcome": "FAILURE", "error": f"missing required field: {field_name}"}))
            raise HTTPException(status_code=422, detail="Validation Error")
    if not isinstance(payload.ctcaeGrade, int) or payload.ctcaeGrade < 1 or payload.ctcaeGrade > 5:
        logger.error(json.dumps({"step": "VALIDATION", "outcome": "FAILURE", "error": "invalid ctcaeGrade"}))
        raise HTTPException(status_code=422, detail="Validation Error")
    if payload.outcome not in _ALLOWED_OUTCOMES:
        logger.error(json.dumps({"step": "VALIDATION", "outcome": "FAILURE", "error": "invalid outcome"}))
        raise HTTPException(status_code=422, detail="Validation Error")
    if payload.actionTaken not in _ALLOWED_ACTIONS:
        logger.error(json.dumps({"step": "VALIDATION", "outcome": "FAILURE", "error": "invalid actionTaken"}))
        raise HTTPException(status_code=422, detail="Validation Error")
    if len(payload.narrative) > 2000:
        logger.error(json.dumps({"step": "VALIDATION", "outcome": "FAILURE", "error": "narrative too long"}))
        raise HTTPException(status_code=422, detail="Validation Error")
    if payload.ctcaeGrade >= 3:
        payload.serious = True
    if payload.ctcaeGrade == 5:
        payload.outcome = "FATAL"
    return payload


def validate_notification_query(
    trialId: str | None,
    siteId: str | None,
    ctcaeGrade: int | None,
    serious: bool | None,
    acknowledged: bool | None,
    priority: str | None,
    dateFrom: str | None,
    dateTo: str | None,
    page: int,
    pageSize: int,
) -> dict[str, Any]:
    if ctcaeGrade is not None and (not isinstance(ctcaeGrade, int) or ctcaeGrade < 1 or ctcaeGrade > 5):
        raise HTTPException(status_code=422, detail="Validation Error")
    if priority is not None and priority not in _ALLOWED_PRIORITIES:
        raise HTTPException(status_code=422, detail="Validation Error")
    parsed_date_from = isoparse(dateFrom) if dateFrom is not None else None
    parsed_date_to = isoparse(dateTo) if dateTo is not None else None
    if parsed_date_from is not None and parsed_date_from.tzinfo is None:
        raise HTTPException(status_code=422, detail="Validation Error")
    if parsed_date_to is not None and parsed_date_to.tzinfo is None:
        raise HTTPException(status_code=422, detail="Validation Error")
    if page < 1 or pageSize < 1 or pageSize > 100:
        raise HTTPException(status_code=422, detail="Validation Error")
    return {
        "trialId": trialId,
        "siteId": siteId,
        "ctcaeGrade": ctcaeGrade,
        "serious": serious,
        "acknowledged": acknowledged,
        "priority": priority,
        "dateFrom": parsed_date_from.astimezone(timezone.utc) if parsed_date_from is not None else None,
        "dateTo": parsed_date_to.astimezone(timezone.utc) if parsed_date_to is not None else None,
        "page": page,
        "pageSize": pageSize,
    }

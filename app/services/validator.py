import json
import logging
import os
from datetime import datetime
from typing import Any

from dateutil.parser import isoparse
from fastapi import HTTPException

from app.schemas.adverse_event_schema import AdverseEventCreateRequest
from app.schemas.notification_schema import NotificationQueryParams

logger = logging.getLogger(__name__)


def _raise_validation(rule: str) -> None:
    logger.error(json.dumps({"rule": rule}))
    raise HTTPException(status_code=422, detail="Validation Error")


def validate_and_coerce_adverse_event(payload: AdverseEventCreateRequest) -> AdverseEventCreateRequest:
    required_fields = [
        "trialId",
        "siteId",
        "patientId",
        "clinicianId",
        "eventDate",
        "aeTermCode",
        "aeTermName",
        "ctcaeGrade",
        "serious",
        "outcome",
        "actionTaken",
        "narrative",
        "reportedBy",
    ]
    for field_name in required_fields:
        if getattr(payload, field_name) is None:
            _raise_validation("MISSING_REQUIRED_FIELD")
    if not isinstance(payload.ctcaeGrade, int) or payload.ctcaeGrade < 1 or payload.ctcaeGrade > 5:
        _raise_validation("INVALID_CTCAE_GRADE")
    if payload.outcome not in {"ONGOING", "RESOLVED", "FATAL", "UNKNOWN"}:
        _raise_validation("INVALID_OUTCOME")
    if payload.actionTaken not in {"NONE", "DOSE_REDUCED", "DRUG_WITHDRAWN", "HOSPITALISED"}:
        _raise_validation("INVALID_ACTION_TAKEN")
    if len(payload.narrative) > 2000:
        _raise_validation("NARRATIVE_TOO_LONG")
    try:
        payload.eventDate = isoparse(str(payload.eventDate))
    except Exception:
        _raise_validation("INVALID_EVENT_DATE")
    if payload.ctcaeGrade >= 3:
        payload.serious = True
    if payload.ctcaeGrade == 5:
        payload.outcome = "FATAL"
    return payload


def validate_notification_query_params(params: NotificationQueryParams) -> NotificationQueryParams:
    if params.ctcaeGrade is not None and (params.ctcaeGrade < 1 or params.ctcaeGrade > 5):
        _raise_validation("INVALID_QUERY_PARAM")
    if params.priority is not None and params.priority not in {"HIGH", "NORMAL"}:
        _raise_validation("INVALID_QUERY_PARAM")
    if params.page < 1 or params.pageSize < 1 or params.pageSize > 100:
        _raise_validation("INVALID_QUERY_PARAM")
    if params.dateFrom is not None:
        try:
            params.dateFrom = isoparse(str(params.dateFrom))
        except Exception:
            _raise_validation("INVALID_QUERY_PARAM")
    if params.dateTo is not None:
        try:
            params.dateTo = isoparse(str(params.dateTo))
        except Exception:
            _raise_validation("INVALID_QUERY_PARAM")
    return params

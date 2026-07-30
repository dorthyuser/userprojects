from typing import Any, TYPE_CHECKING, Optional

from email_validator import validate_email
from fastapi import HTTPException

if TYPE_CHECKING:
    from app.schemas.adverse_event_schema import AdverseEventCreateRequest
    from app.schemas.notification_schema import NotificationQueryParams

_ALLOWED_OUTCOMES = {"ONGOING", "RESOLVED", "FATAL", "UNKNOWN"}
_ALLOWED_ACTIONS = {"NONE", "DOSE_REDUCED", "DRUG_WITHDRAWN", "HOSPITALISED"}
_ALLOWED_PRIORITIES = {"HIGH", "NORMAL"}


def _raise_validation(message: str) -> None:
    raise HTTPException(status_code=400, detail=message)


def validate_and_coerce_adverse_event(payload: "AdverseEventCreateRequest") -> "AdverseEventCreateRequest":
    # Basic domain validation and coercion
    if payload.ctcaeGrade < 1 or payload.ctcaeGrade > 5:
        _raise_validation("ctcaeGrade must be between 1 and 5")
    if payload.outcome not in _ALLOWED_OUTCOMES:
        _raise_validation("outcome invalid")
    if payload.actionTaken not in _ALLOWED_ACTIONS:
        _raise_validation("actionTaken invalid")
    if len(payload.narrative) > 2000:
        _raise_validation("narrative too long")

    # validate email format
    validate_email(str(payload.reportedBy))

    # Coerce serious/outcome based on grade
    if payload.ctcaeGrade >= 3:
        payload.serious = True
    if payload.ctcaeGrade == 5:
        payload.outcome = "FATAL"
        payload.serious = True
    return payload


def validate_notification_query(params: "NotificationQueryParams") -> "NotificationQueryParams":
    if params.ctcaeGrade is not None and (params.ctcaeGrade < 1 or params.ctcaeGrade > 5):
        _raise_validation("ctcaeGrade invalid")
    if params.priority is not None and params.priority not in _ALLOWED_PRIORITIES:
        _raise_validation("priority invalid")
    if params.page < 1:
        _raise_validation("page invalid")
    if params.pageSize < 1 or params.pageSize > 100:
        _raise_validation("pageSize invalid")
    return params

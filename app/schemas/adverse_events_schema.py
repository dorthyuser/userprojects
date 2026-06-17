from datetime import datetime
from typing import Optional
from pydantic import BaseModel, EmailStr, field_validator, model_validator


# ── Enums ──────────────────────────────────────────────────────────────────────

OUTCOME_VALUES    = {"ONGOING", "RESOLVED", "FATAL", "UNKNOWN"}
ACTION_TAKEN_VALUES = {"NONE", "DOSE_REDUCED", "DRUG_WITHDRAWN", "HOSPITALISED"}
PRIORITY_VALUES   = {"HIGH", "NORMAL"}


# ── POST Request ───────────────────────────────────────────────────────────────

class AdverseEventCreateRequest(BaseModel):
    trialId:       str
    siteId:        str
    patientId:     str
    clinicianId:   str
    eventDate:     datetime
    aeTermCode:    str
    aeTermName:    str
    ctcaeGrade:    int
    serious:       bool
    outcome:       str
    actionTaken:   str
    narrative:     str
    relatedDrugId: Optional[str] = None
    reportedBy:    EmailStr

    @field_validator("ctcaeGrade")
    @classmethod
    def validate_ctcae_grade(cls, v: int) -> int:
        if not (1 <= v <= 5):
            raise ValueError("INVALID_CTCAE_GRADE: ctcaeGrade must be an integer between 1 and 5")
        return v

    @field_validator("outcome")
    @classmethod
    def validate_outcome(cls, v: str) -> str:
        if v not in OUTCOME_VALUES:
            raise ValueError(f"INVALID_OUTCOME: outcome must be one of {OUTCOME_VALUES}")
        return v

    @field_validator("actionTaken")
    @classmethod
    def validate_action_taken(cls, v: str) -> str:
        if v not in ACTION_TAKEN_VALUES:
            raise ValueError(f"INVALID_ACTION_TAKEN: actionTaken must be one of {ACTION_TAKEN_VALUES}")
        return v

    @field_validator("narrative")
    @classmethod
    def validate_narrative(cls, v: str) -> str:
        if len(v) > 2000:
            raise ValueError("NARRATIVE_TOO_LONG: narrative exceeds 2000 characters")
        return v


# ── POST Response ──────────────────────────────────────────────────────────────

class AdverseEventCreateResponse(BaseModel):
    status:         str = "success"
    aeId:           str
    notificationId: str
    snsPublished:   bool
    snsMessageId:   Optional[str] = None
    receivedAt:     datetime


# ── GET — single notification ──────────────────────────────────────────────────

class NotificationResponse(BaseModel):
    notificationId: str
    aeId:           str
    trialId:        str
    siteId:         str
    patientId:      str
    aeTermName:     str
    ctcaeGrade:     int
    serious:        bool
    priority:       str
    outcome:        str
    acknowledged:   bool
    snsPublished:   bool
    createdAt:      datetime

    model_config = {"from_attributes": True}


# ── GET Response ───────────────────────────────────────────────────────────────

class NotificationListResponse(BaseModel):
    status:        str = "success"
    total:         int
    page:          int
    pageSize:      int
    notifications: list[NotificationResponse]

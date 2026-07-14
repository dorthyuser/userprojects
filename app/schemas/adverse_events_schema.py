from datetime import datetime
from typing import Any

from pydantic import AwareDatetime, BaseModel, ConfigDict, EmailStr, Field, field_validator


class AdverseEventCreateRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")

    trialId: str = Field(min_length=1, max_length=50)
    siteId: str = Field(min_length=1, max_length=50)
    patientId: str = Field(min_length=1, max_length=50)
    clinicianId: str = Field(min_length=1, max_length=50)
    eventDate: AwareDatetime
    aeTermCode: str = Field(min_length=1, max_length=20)
    aeTermName: str = Field(min_length=1, max_length=255)
    ctcaeGrade: int
    serious: bool
    outcome: str = Field(min_length=1, max_length=20)
    actionTaken: str = Field(min_length=1, max_length=30)
    narrative: str = Field(min_length=1, max_length=2000)
    relatedDrugId: str | None = Field(default=None, min_length=1, max_length=50)
    reportedBy: EmailStr

    @field_validator("ctcaeGrade")
    @classmethod
    def validate_ctcae_grade(cls, value: int) -> int:
        if not isinstance(value, int) or value < 1 or value > 5:
            raise ValueError("INVALID_CTCAE_GRADE")
        return value

    @field_validator("outcome")
    @classmethod
    def validate_outcome(cls, value: str) -> str:
        if value not in {"ONGOING", "RESOLVED", "FATAL", "UNKNOWN"}:
            raise ValueError("INVALID_OUTCOME")
        return value

    @field_validator("actionTaken")
    @classmethod
    def validate_action_taken(cls, value: str) -> str:
        if value not in {"NONE", "DOSE_REDUCED", "DRUG_WITHDRAWN", "HOSPITALISED"}:
            raise ValueError("INVALID_ACTION_TAKEN")
        return value

    @field_validator("narrative")
    @classmethod
    def validate_narrative(cls, value: str) -> str:
        if len(value) > 2000:
            raise ValueError("NARRATIVE_TOO_LONG")
        return value


class AdverseEventCreateResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    status: str
    aeId: str
    notificationId: str
    snsPublished: bool
    snsMessageId: str | None
    receivedAt: str


class NotificationResponseItem(BaseModel):
    model_config = ConfigDict(extra="forbid")

    notificationId: str
    aeId: str
    trialId: str
    siteId: str
    patientId: str
    aeTermName: str
    ctcaeGrade: int
    serious: bool
    priority: str
    outcome: str
    acknowledged: bool
    snsPublished: bool
    createdAt: datetime


class NotificationListResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    status: str
    total: int
    page: int
    pageSize: int
    notifications: list[NotificationResponseItem]

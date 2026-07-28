from datetime import datetime
from typing import Any

from pydantic import AwareDatetime, BaseModel, ConfigDict, EmailStr, Field, field_validator


class AdverseEventCreateRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")

    trialId: str
    siteId: str
    patientId: str
    clinicianId: str
    eventDate: AwareDatetime
    aeTermCode: str
    aeTermName: str
    ctcaeGrade: int
    serious: bool
    outcome: str
    actionTaken: str
    narrative: str
    relatedDrugId: str | None = None
    reportedBy: EmailStr

    @field_validator("trialId", "siteId", "patientId", "clinicianId", "aeTermCode", "aeTermName", "outcome", "actionTaken", "narrative", "reportedBy")
    @classmethod
    def non_empty(cls, value: str) -> str:
        if not str(value).strip():
            raise ValueError("must not be empty")
        return value

    @field_validator("narrative")
    @classmethod
    def narrative_length(cls, value: str) -> str:
        if len(value) > 2000:
            raise ValueError("narrative too long")
        return value


class AdverseEventCreateResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    status: str
    aeId: str
    notificationId: str
    snsPublished: bool
    snsMessageId: str | None
    message: str
    receivedAt: datetime


class NotificationListItem(BaseModel):
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
    acknowledgedBy: str | None
    acknowledgedAt: datetime | None
    snsPublished: bool
    snsMessageId: str | None
    createdAt: datetime


class NotificationListResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    status: str
    total: int
    page: int
    pageSize: int
    notifications: list[NotificationListItem]

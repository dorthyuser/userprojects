from datetime import datetime
from typing import Any

from pydantic import BaseModel, ConfigDict, EmailStr, Field, field_validator


class AdverseEventCreateRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")

    trialId: str = Field(min_length=1, max_length=50)
    siteId: str = Field(min_length=1, max_length=50)
    patientId: str = Field(min_length=1, max_length=50)
    clinicianId: str = Field(min_length=1, max_length=50)
    eventDate: datetime
    aeTermCode: str = Field(min_length=1, max_length=20)
    aeTermName: str = Field(min_length=1, max_length=255)
    ctcaeGrade: int
    serious: bool
    outcome: str = Field(min_length=1, max_length=20)
    actionTaken: str = Field(min_length=1, max_length=30)
    narrative: str = Field(min_length=1, max_length=2000)
    relatedDrugId: str | None = Field(default=None, max_length=50)
    reportedBy: EmailStr

    @field_validator("eventDate")
    @classmethod
    def ensure_timezone_aware(cls, value: datetime) -> datetime:
        if value.tzinfo is None or value.utcoffset() is None:
            raise ValueError("eventDate must be timezone-aware")
        return value

    @field_validator("narrative")
    @classmethod
    def validate_narrative_length(cls, value: str) -> str:
        if len(value) > 2000:
            raise ValueError("narrative exceeds maximum length")
        return value


class AdverseEventCreateResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    status: str
    aeId: str
    notificationId: str
    message: str
    receivedAt: datetime


class NotificationItem(BaseModel):
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
    createdAt: datetime


class NotificationListResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    status: str
    total: int
    page: int
    pageSize: int
    notifications: list[NotificationItem]

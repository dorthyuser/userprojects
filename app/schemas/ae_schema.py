from __future__ import annotations

from datetime import datetime

from pydantic import AwareDatetime, BaseModel, ConfigDict, EmailStr, Field, field_validator, model_validator


class AdverseEventCreateRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")

    trialId: str = Field(min_length=1, max_length=50)
    siteId: str = Field(min_length=1, max_length=50)
    patientId: str = Field(min_length=1, max_length=50)
    clinicianId: str = Field(min_length=1, max_length=50)
    eventDate: str
    aeTermCode: str = Field(min_length=1, max_length=20)
    aeTermName: str = Field(min_length=1, max_length=255)
    ctcaeGrade: int
    serious: bool
    outcome: str
    actionTaken: str
    narrative: str = Field(min_length=1, max_length=2000)
    relatedDrugId: str | None = Field(default=None, min_length=1, max_length=50)
    reportedBy: EmailStr

    @field_validator("trialId", "siteId", "patientId", "clinicianId", "aeTermCode", "aeTermName", "actionTaken", "outcome", "narrative")
    @classmethod
    def strip_strings(cls, value: str) -> str:
        return value.strip()

    @field_validator("eventDate")
    @classmethod
    def validate_event_date(cls, value: str) -> str:
        if not value:
            raise ValueError("eventDate is required")
        return value

    @model_validator(mode="after")
    def validate_related_drug(self) -> "AdverseEventCreateRequest":
        if self.relatedDrugId is not None and not self.relatedDrugId.strip():
            raise ValueError("relatedDrugId must not be blank")
        return self


class AdverseEventCreateResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    status: str
    aeId: str
    notificationId: str
    snsPublished: bool
    snsMessageId: str | None
    receivedAt: AwareDatetime


class NotificationRecordResponse(BaseModel):
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
    createdAt: AwareDatetime


class NotificationListResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    status: str
    total: int
    page: int
    pageSize: int
    notifications: list[NotificationRecordResponse]

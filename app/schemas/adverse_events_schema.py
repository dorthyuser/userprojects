from datetime import datetime

from pydantic import AwareDatetime, BaseModel, ConfigDict, EmailStr, Field, field_validator


class AdverseEventCreateRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")

    trialId: str
    siteId: str
    patientId: str
    clinicianId: str
    eventDate: str
    aeTermCode: str
    aeTermName: str
    ctcaeGrade: int
    serious: bool
    outcome: str
    actionTaken: str
    narrative: str
    relatedDrugId: str | None = None
    reportedBy: EmailStr

    @field_validator("narrative")
    @classmethod
    def validate_narrative(cls, value: str) -> str:
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
    receivedAt: str


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
    acknowledgedAt: str | None
    snsPublished: bool
    snsMessageId: str | None
    createdAt: str | None


class NotificationListResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    status: str
    total: int
    page: int
    pageSize: int
    notifications: list[NotificationItem]

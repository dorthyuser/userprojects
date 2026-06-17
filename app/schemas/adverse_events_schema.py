from datetime import datetime

from pydantic import BaseModel, ConfigDict, Field, field_validator
from dateutil import parser as date_parser


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
    outcome: str = Field(min_length=1, max_length=20)
    actionTaken: str = Field(min_length=1, max_length=30)
    narrative: str = Field(min_length=1, max_length=2000)
    relatedDrugId: str | None = Field(default=None, max_length=50)
    reportedBy: str = Field(min_length=3, max_length=254)

    @field_validator("ctcaeGrade")
    @classmethod
    def validate_ctcae_grade(cls, value: int) -> int:
        if not isinstance(value, int):
            raise ValueError("ctcaeGrade must be an integer")
        return value

    @field_validator("eventDate")
    @classmethod
    def validate_event_date(cls, value: str) -> str:
        try:
            parsed = date_parser.isoparse(value)
        except Exception as exc:
            raise ValueError("eventDate must be a valid ISO8601 datetime string") from exc
        if parsed.tzinfo is None:
            raise ValueError("eventDate must include timezone information")
        return value

    @field_validator("reportedBy")
    @classmethod
    def validate_reported_by(cls, value: str) -> str:
        # Minimal email sanity check to avoid pydantic's EmailStr dependency
        if "@" not in value or "." not in value.split("@")[-1]:
            raise ValueError("reportedBy must be a valid email address")
        return value


class AdverseEventCreateResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    status: str
    aeId: str
    notificationId: str
    snsPublished: bool
    snsMessageId: str | None
    receivedAt: datetime


class NotificationResponse(BaseModel):
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
    notifications: list[NotificationResponse]

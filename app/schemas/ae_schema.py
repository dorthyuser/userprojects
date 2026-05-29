from datetime import datetime
from typing import Any

from pydantic import BaseModel, ConfigDict, EmailStr, Field, field_validator
from pydantic.types import AwareDatetime


class AEReportRequest(BaseModel):
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
    outcome: str
    actionTaken: str
    narrative: str = Field(min_length=1, max_length=2000)
    relatedDrugId: str | None = Field(default=None, min_length=1, max_length=50)
    reportedBy: EmailStr

    @field_validator("ctcaeGrade")
    @classmethod
    def validate_ctcae_grade(cls, value: int) -> int:
        if not isinstance(value, int) or value < 1 or value > 5:
            raise ValueError("ctcaeGrade must be an integer between 1 and 5")
        return value

    @field_validator("outcome")
    @classmethod
    def validate_outcome(cls, value: str) -> str:
        allowed = {"ONGOING", "RESOLVED", "FATAL", "UNKNOWN"}
        if value not in allowed:
            raise ValueError("outcome value not in allowed enum")
        return value

    @field_validator("actionTaken")
    @classmethod
    def validate_action_taken(cls, value: str) -> str:
        allowed = {"NONE", "DOSE_REDUCED", "DRUG_WITHDRAWN", "HOSPITALISED"}
        if value not in allowed:
            raise ValueError("actionTaken value not in allowed enum")
        return value

    @field_validator("narrative")
    @classmethod
    def validate_narrative_length(cls, value: str) -> str:
        if len(value) > 2000:
            raise ValueError("narrative exceeds 2000 characters")
        return value


class AEReportResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    status: str
    aeId: str
    notificationId: str
    snsPublished: bool
    snsMessageId: str | None
    receivedAt: AwareDatetime


class NotificationsQueryParams(BaseModel):
    model_config = ConfigDict(extra="forbid")

    trialId: str | None = None
    siteId: str | None = None
    ctcaeGrade: int | None = None
    serious: bool | None = None
    acknowledged: bool | None = None
    priority: str | None = None
    dateFrom: AwareDatetime | None = None
    dateTo: AwareDatetime | None = None
    page: int = 1
    pageSize: int = 20

    @field_validator("ctcaeGrade")
    @classmethod
    def validate_ctcae_grade(cls, value: int | None) -> int | None:
        if value is not None and (value < 1 or value > 5):
            raise ValueError("ctcaeGrade must be between 1 and 5")
        return value

    @field_validator("priority")
    @classmethod
    def validate_priority(cls, value: str | None) -> str | None:
        if value is not None and value not in {"HIGH", "NORMAL"}:
            raise ValueError("priority value not in allowed enum")
        return value

    @field_validator("page")
    @classmethod
    def validate_page(cls, value: int) -> int:
        if value < 1:
            raise ValueError("page must be >= 1")
        return value

    @field_validator("pageSize")
    @classmethod
    def validate_page_size(cls, value: int) -> int:
        if value < 1 or value > 100:
            raise ValueError("pageSize must be between 1 and 100")
        return value


class AENotificationItem(BaseModel):
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


class AENotificationsResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    status: str
    total: int
    page: int
    pageSize: int
    notifications: list[AENotificationItem]
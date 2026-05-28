from datetime import datetime
from typing import Any, Literal

from pydantic import AwareDatetime, BaseModel, ConfigDict, EmailStr, Field, field_validator, model_validator


class AECreateRequest(BaseModel):
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
    outcome: Literal["ONGOING", "RESOLVED", "FATAL", "UNKNOWN"]
    actionTaken: Literal["NONE", "DOSE_REDUCED", "DRUG_WITHDRAWN", "HOSPITALISED"]
    narrative: str = Field(min_length=1, max_length=2000)
    relatedDrugId: str | None = Field(default=None, max_length=50)
    reportedBy: EmailStr

    @field_validator("ctcaeGrade")
    @classmethod
    def validate_ctcae_grade(cls, value: int) -> int:
        if value < 1 or value > 5:
            raise ValueError("ctcaeGrade must be between 1 and 5")
        return value

    @field_validator("narrative")
    @classmethod
    def validate_narrative(cls, value: str) -> str:
        if len(value) > 2000:
            raise ValueError("narrative exceeds 2000 characters")
        return value


class AECreateResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    status: Literal["success"]
    aeId: str
    notificationId: str
    snsPublished: bool
    snsMessageId: str | None
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
    priority: Literal["HIGH", "NORMAL"]
    outcome: str
    acknowledged: bool
    snsPublished: bool
    createdAt: datetime


class AENotificationsResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    status: Literal["success"]
    total: int
    page: int
    pageSize: int
    notifications: list[NotificationItem]


class NotificationQueryParams(BaseModel):
    model_config = ConfigDict(extra="forbid")

    trialId: str | None = None
    siteId: str | None = None
    ctcaeGrade: int | None = None
    serious: bool | None = None
    acknowledged: bool | None = None
    priority: str | None = None
    dateFrom: str | None = None
    dateTo: str | None = None
    page: int = 1
    pageSize: int = 20

    @field_validator("page")
    @classmethod
    def validate_page(cls, value: int) -> int:
        return 1 if value < 1 else value

    @field_validator("pageSize")
    @classmethod
    def validate_page_size(cls, value: int) -> int:
        if value < 1:
            return 20
        return 100 if value > 100 else value

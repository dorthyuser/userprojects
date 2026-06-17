from datetime import datetime
from typing import Any

from pydantic import AwareDatetime, BaseModel, ConfigDict, EmailStr, Field, field_validator, model_validator


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
    outcome: str
    actionTaken: str
    narrative: str = Field(min_length=1, max_length=2000)
    relatedDrugId: str | None = Field(default=None, min_length=1, max_length=50)
    reportedBy: EmailStr

    @field_validator("ctcaeGrade")
    @classmethod
    def validate_ctcae_grade(cls, value: int) -> int:
        if value < 1 or value > 5:
            raise ValueError("INVALID_CTCAE_GRADE")
        return value

    @field_validator("outcome")
    @classmethod
    def validate_outcome(cls, value: str) -> str:
        allowed = {"ONGOING", "RESOLVED", "FATAL", "UNKNOWN"}
        if value not in allowed:
            raise ValueError("INVALID_OUTCOME")
        return value

    @field_validator("actionTaken")
    @classmethod
    def validate_action_taken(cls, value: str) -> str:
        allowed = {"NONE", "DOSE_REDUCED", "DRUG_WITHDRAWN", "HOSPITALISED"}
        if value not in allowed:
            raise ValueError("INVALID_ACTION_TAKEN")
        return value

    @model_validator(mode="after")
    def validate_required_fields(self) -> "AdverseEventCreateRequest":
        required_fields = [
            "trialId",
            "siteId",
            "patientId",
            "clinicianId",
            "eventDate",
            "aeTermCode",
            "aeTermName",
            "ctcaeGrade",
            "serious",
            "outcome",
            "actionTaken",
            "narrative",
            "reportedBy"
        ]
        for field_name in required_fields:
            if getattr(self, field_name) is None:
                raise ValueError("MISSING_REQUIRED_FIELD")
        if self.ctcaeGrade >= 3 and self.serious is not True:
            object.__setattr__(self, "serious", True)
        if self.ctcaeGrade == 5 and self.outcome != "FATAL":
            object.__setattr__(self, "outcome", "FATAL")
        return self


class AdverseEventCreateResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    status: str
    aeId: str
    notificationId: str
    snsPublished: bool
    snsMessageId: str | None
    receivedAt: datetime


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


class NotificationQueryParams(BaseModel):
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

    @field_validator("priority")
    @classmethod
    def validate_priority(cls, value: str | None) -> str | None:
        if value is None:
            return value
        if value not in {"HIGH", "NORMAL"}:
            raise ValueError("INVALID_QUERY_PARAM")
        return value

    @field_validator("page")
    @classmethod
    def validate_page(cls, value: int) -> int:
        if value < 1:
            raise ValueError("INVALID_QUERY_PARAM")
        return value

    @field_validator("pageSize")
    @classmethod
    def validate_page_size(cls, value: int) -> int:
        if value < 1 or value > 100:
            raise ValueError("INVALID_QUERY_PARAM")
        return value
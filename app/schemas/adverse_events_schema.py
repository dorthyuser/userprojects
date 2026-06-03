from datetime import datetime
from typing import Any, Literal

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
    outcome: Literal["ONGOING", "RESOLVED", "FATAL", "UNKNOWN"]
    actionTaken: Literal["NONE", "DOSE_REDUCED", "DRUG_WITHDRAWN", "HOSPITALISED"]
    narrative: str = Field(min_length=1, max_length=2000)
    relatedDrugId: str | None = Field(default=None, min_length=1, max_length=50)
    reportedBy: EmailStr

    @field_validator("ctcaeGrade")
    @classmethod
    def validate_ctcae_grade(cls, value: int) -> int:
        if not isinstance(value, int) or value < 1 or value > 5:
            raise ValueError("ctcaeGrade must be an integer between 1 and 5")
        return value

    @field_validator("narrative")
    @classmethod
    def validate_narrative(cls, value: str) -> str:
        if len(value) > 2000:
            raise ValueError("narrative exceeds 2000 characters")
        return value

    @model_validator(mode="after")
    def validate_coercion_rules(self) -> "AdverseEventCreateRequest":
        return self


class AdverseEventCreateResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    status: Literal["success"]
    aeId: str
    notificationId: str
    snsPublished: bool
    snsMessageId: str | None
    receivedAt: AwareDatetime


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
    priority: Literal["HIGH", "NORMAL"]
    outcome: str
    acknowledged: bool
    snsPublished: bool
    createdAt: AwareDatetime


class NotificationListResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    status: Literal["success"]
    total: int
    page: int
    pageSize: int
    notifications: list[NotificationResponse]

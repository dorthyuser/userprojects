from datetime import datetime
from typing import Annotated, Literal

from pydantic import AwareDatetime, BaseModel, ConfigDict, EmailStr, Field, field_validator, model_validator

AllowedOutcome = Literal["ONGOING", "RESOLVED", "FATAL", "UNKNOWN"]
AllowedActionTaken = Literal["NONE", "DOSE_REDUCED", "DRUG_WITHDRAWN", "HOSPITALISED"]
AllowedPriority = Literal["HIGH", "NORMAL"]


class AdverseEventCreateRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")

    trialId: str = Field(min_length=1, max_length=50)
    siteId: str = Field(min_length=1, max_length=50)
    patientId: str = Field(min_length=1, max_length=50)
    clinicianId: str = Field(min_length=1, max_length=50)
    eventDate: AwareDatetime
    aeTermCode: str = Field(min_length=1, max_length=20)
    aeTermName: str = Field(min_length=1, max_length=255)
    ctcaeGrade: int = Field(ge=1, le=5)
    serious: bool
    outcome: AllowedOutcome
    actionTaken: AllowedActionTaken
    narrative: str = Field(min_length=1)
    relatedDrugId: str | None = Field(default=None, max_length=50)
    reportedBy: EmailStr

    @field_validator("narrative")
    @classmethod
    def validate_narrative_length(cls, value: str) -> str:
        if len(value) > 2000:
            raise ValueError("narrative exceeds 2000 characters")
        return value


class AdverseEventCreateResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    status: Literal["success"]
    aeId: str
    notificationId: str
    snsPublished: bool
    snsMessageId: str | None
    receivedAt: AwareDatetime


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
    priority: AllowedPriority
    outcome: AllowedOutcome
    acknowledged: bool
    snsPublished: bool
    createdAt: AwareDatetime


class NotificationListResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    status: Literal["success"]
    total: int
    page: int
    pageSize: int
    notifications: list[NotificationResponseItem]

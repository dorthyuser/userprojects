from datetime import datetime
from typing import Annotated, Any

from pydantic import AwareDatetime, BaseModel, ConfigDict, EmailStr, Field, field_validator


class AdverseEventCreateRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")

    trialId: Annotated[str, Field(min_length=1, max_length=50)]
    siteId: Annotated[str, Field(min_length=1, max_length=50)]
    patientId: Annotated[str, Field(min_length=1, max_length=50)]
    clinicianId: Annotated[str, Field(min_length=1, max_length=50)]
    eventDate: AwareDatetime
    aeTermCode: Annotated[str, Field(min_length=1, max_length=20)]
    aeTermName: Annotated[str, Field(min_length=1, max_length=255)]
    ctcaeGrade: int
    serious: bool
    outcome: str
    actionTaken: str
    narrative: Annotated[str, Field(min_length=1, max_length=2000)]
    relatedDrugId: str | None = Field(default=None, max_length=50)
    reportedBy: EmailStr

    @field_validator("outcome")
    @classmethod
    def validate_outcome(cls, value: str) -> str:
        allowed = {"ONGOING", "RESOLVED", "FATAL", "UNKNOWN"}
        if value not in allowed:
            raise ValueError("Invalid outcome")
        return value

    @field_validator("actionTaken")
    @classmethod
    def validate_action_taken(cls, value: str) -> str:
        allowed = {"NONE", "DOSE_REDUCED", "DRUG_WITHDRAWN", "HOSPITALISED"}
        if value not in allowed:
            raise ValueError("Invalid actionTaken")
        return value


class AdverseEventCreateResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    status: str
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
    notifications: list[NotificationResponseItem]
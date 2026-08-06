from datetime import datetime

from pydantic import AwareDatetime, BaseModel, ConfigDict, EmailStr, Field, field_validator


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
    outcome: str = Field(min_length=1, max_length=20)
    actionTaken: str = Field(min_length=1, max_length=30)
    narrative: str = Field(min_length=1, max_length=2000)
    relatedDrugId: str | None = Field(default=None, max_length=50)
    reportedBy: EmailStr

    @field_validator("outcome")
    @classmethod
    def validate_outcome(cls, value: str) -> str:
        allowed = {"ONGOING", "RESOLVED", "FATAL", "UNKNOWN"}
        if value not in allowed:
            raise ValueError("invalid outcome")
        return value

    @field_validator("actionTaken")
    @classmethod
    def validate_action_taken(cls, value: str) -> str:
        allowed = {"NONE", "DOSE_REDUCED", "DRUG_WITHDRAWN", "HOSPITALISED"}
        if value not in allowed:
            raise ValueError("invalid actionTaken")
        return value

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
    createdAt: str


class NotificationListResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    status: str
    total: int
    page: int
    pageSize: int
    notifications: list[NotificationItem]

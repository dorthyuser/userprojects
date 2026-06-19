from datetime import datetime
from typing import Optional

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
    relatedDrugId: Optional[str] = Field(default=None, min_length=1, max_length=50)
    reportedBy: EmailStr
    idempotencyWindowS: int = Field(default=60, ge=1, le=3600)

    @field_validator("narrative")
    @classmethod
    def validate_narrative_length(cls, value: str) -> str:
        if len(value) > 2000:
            raise ValueError("narrative exceeds maximum length")
        return value


class AdverseEventCreateResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    status: str
    aeId: str
    notificationId: str
    snsPublished: bool
    snsMessageId: Optional[str]
    message: str
    receivedAt: datetime

from __future__ import annotations

from datetime import datetime
from typing import Any, Literal
from uuid import UUID

from pydantic import AwareDatetime, BaseModel, ConfigDict, Field, field_validator, model_validator


class ConsentGrantRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")

    patient_id: UUID
    purpose: Literal["treatment", "research", "marketing", "data_sharing"]
    legal_basis: Literal["consent", "vital_interest", "legal_obligation"]
    scope: list[str] = Field(min_length=1, max_length=50)
    consent_version: str = Field(max_length=32)
    channel: Literal["web", "paper", "phone", "in_person"]
    expires_at: AwareDatetime | None = None

    @field_validator("scope")
    @classmethod
    def validate_scope(cls, value: list[str]) -> list[str]:
        if not value or len(value) < 1:
            raise ValueError("scope must contain at least one item")
        if any(not isinstance(item, str) or not item.strip() for item in value):
            raise ValueError("scope items must be non-empty strings")
        return value

    @field_validator("expires_at")
    @classmethod
    def validate_expires_at(cls, value: AwareDatetime | None) -> AwareDatetime | None:
        if value is not None and value <= datetime.now(value.tzinfo):
            raise ValueError("expires_at must be in the future")
        return value


class ConsentWithdrawRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")

    reason: str | None = Field(default=None, max_length=256)


class ConsentRecordSchema(BaseModel):
    model_config = ConfigDict(extra="forbid")

    consent_id: UUID
    patient_id: UUID
    purpose: Literal["treatment", "research", "marketing", "data_sharing"]
    status: Literal["granted", "withdrawn", "expired"]
    legal_basis: Literal["consent", "vital_interest", "legal_obligation"]
    scope: list[str]
    consent_version: str
    channel: Literal["web", "paper", "phone", "in_person"]
    granted_at: AwareDatetime
    expires_at: AwareDatetime | None
    withdrawn_at: AwareDatetime | None


class ConsentGrantResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    consent_id: UUID
    status: Literal["granted"]
    audit_id: int
    occurred_at: AwareDatetime


class ConsentWithdrawResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    consent_id: UUID
    status: Literal["withdrawn"]
    withdrawn_at: AwareDatetime
    audit_id: int


class ConsentListResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    patient_id: UUID
    consents: list[ConsentRecordSchema]


class ConsentResponse(ConsentRecordSchema):
    pass


class AuditRecordSchema(BaseModel):
    model_config = ConfigDict(extra="forbid")

    audit_id: int
    action: Literal["GRANT", "WITHDRAW", "UPDATE", "EXPIRE", "VIEW"]
    actor_id: str
    actor_role: Literal["patient", "clinician", "dpo", "system"]
    occurred_at: AwareDatetime


class ConsentAuditHistoryResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    patient_id: UUID
    entries: list[AuditRecordSchema]

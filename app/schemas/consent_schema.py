from __future__ import annotations

from datetime import datetime
from typing import Literal
from uuid import UUID

from pydantic import AwareDatetime, BaseModel, ConfigDict, Field, field_validator


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
        if not value:
            raise ValueError("scope must contain at least one item")
        for item in value:
            if not isinstance(item, str) or not item.strip():
                raise ValueError("scope items must be non-empty strings")
        return value

    @field_validator("expires_at")
    @classmethod
    def validate_expires_at(cls, value: AwareDatetime | None) -> AwareDatetime | None:
        if value is not None and value <= datetime.now(tz=value.tzinfo):
            raise ValueError("expires_at must be in the future")
        return value


class ConsentWithdrawRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")

    reason: str | None = Field(default=None, max_length=256)

    @field_validator("reason")
    @classmethod
    def validate_reason(cls, value: str | None) -> str | None:
        if value is not None and not value.strip():
            raise ValueError("reason must not be empty when provided")
        return value


class ConsentRecord(BaseModel):
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
    expires_at: AwareDatetime | None = None
    withdrawn_at: AwareDatetime | None = None


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


class ConsentGetAllResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    patient_id: UUID
    consents: list[ConsentRecord]


class ConsentGetOneResponse(ConsentRecord):
    pass


class AuditRecord(BaseModel):
    model_config = ConfigDict(extra="forbid")

    audit_id: int
    action: Literal["GRANT", "WITHDRAW", "UPDATE", "EXPIRE", "VIEW"]
    actor_id: str
    actor_role: Literal["patient", "clinician", "dpo", "system"]
    occurred_at: AwareDatetime


class ConsentAuditHistoryResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    patient_id: UUID
    entries: list[AuditRecord]

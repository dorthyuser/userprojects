from __future__ import annotations

from datetime import datetime
from typing import Annotated, Any, Literal
from uuid import UUID

from pydantic import AwareDatetime, BaseModel, ConfigDict, Field, field_validator, model_validator

PurposeType = Literal["treatment", "research", "marketing", "data_sharing"]
LegalBasisType = Literal["consent", "vital_interest", "legal_obligation"]
ChannelType = Literal["web", "paper", "phone", "in_person"]
StatusType = Literal["granted", "withdrawn", "expired"]
ActionType = Literal["GRANT", "WITHDRAW", "UPDATE", "EXPIRE", "VIEW"]
ActorRoleType = Literal["patient", "clinician", "dpo", "system"]


class ConsentGrantRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")

    patient_id: UUID
    purpose: PurposeType
    legal_basis: LegalBasisType
    scope: list[str] = Field(min_length=1, max_length=50)
    consent_version: str = Field(max_length=32)
    channel: ChannelType
    expires_at: AwareDatetime | None = None

    @field_validator("scope")
    @classmethod
    def validate_scope(cls, value: list[str]) -> list[str]:
        if not value:
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

    @field_validator("reason")
    @classmethod
    def validate_reason(cls, value: str | None) -> str | None:
        if value is not None and not value.strip():
            raise ValueError("reason must not be empty")
        return value


class ConsentRecordResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    consent_id: UUID
    patient_id: UUID
    purpose: PurposeType
    status: StatusType
    legal_basis: LegalBasisType
    scope: list[str]
    consent_version: str
    channel: ChannelType
    granted_at: AwareDatetime
    expires_at: AwareDatetime | None
    withdrawn_at: AwareDatetime | None


class ConsentStateResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    patient_id: UUID
    consents: list[ConsentRecordResponse]


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


class AuditRecordResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    audit_id: int
    action: ActionType
    actor_id: str
    actor_role: ActorRoleType
    occurred_at: AwareDatetime


class ConsentAuditHistoryResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    patient_id: UUID
    entries: list[AuditRecordResponse]

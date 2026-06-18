from datetime import datetime
from typing import Any
from uuid import UUID

from pydantic import AwareDatetime, BaseModel, ConfigDict, Field, field_validator, model_validator


class ConsentGrantRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")

    patient_id: UUID
    purpose: str
    legal_basis: str
    scope: list[str]
    consent_version: str = Field(max_length=32)
    channel: str
    expires_at: AwareDatetime | None = None

    @field_validator("purpose")
    @classmethod
    def validate_purpose(cls, value: str) -> str:
        if value not in {"treatment", "research", "marketing", "data_sharing"}:
            raise ValueError("Validation Error")
        return value

    @field_validator("legal_basis")
    @classmethod
    def validate_legal_basis(cls, value: str) -> str:
        if value not in {"consent", "vital_interest", "legal_obligation"}:
            raise ValueError("Validation Error")
        return value

    @field_validator("scope")
    @classmethod
    def validate_scope(cls, value: list[str]) -> list[str]:
        if not (1 <= len(value) <= 50):
            raise ValueError("Validation Error")
        if any(not isinstance(item, str) or not item.strip() for item in value):
            raise ValueError("Validation Error")
        return value

    @field_validator("channel")
    @classmethod
    def validate_channel(cls, value: str) -> str:
        if value not in {"web", "paper", "phone", "in_person"}:
            raise ValueError("Validation Error")
        return value

    @model_validator(mode="after")
    def validate_expires_at_future(self) -> "ConsentGrantRequest":
        if self.expires_at is not None and self.expires_at <= datetime.now(self.expires_at.tzinfo):
            raise ValueError("Validation Error")
        return self


class ConsentWithdrawRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")

    reason: str | None = Field(default=None, max_length=256)


class ConsentRecord(BaseModel):
    model_config = ConfigDict(extra="forbid")

    consent_id: UUID
    patient_id: UUID
    purpose: str
    status: str
    legal_basis: str
    scope: list[str]
    consent_version: str
    channel: str
    granted_at: AwareDatetime
    expires_at: AwareDatetime | None
    withdrawn_at: AwareDatetime | None


class ConsentGrantResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    consent_id: UUID
    status: str
    audit_id: int
    occurred_at: AwareDatetime


class ConsentWithdrawResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    consent_id: UUID
    status: str
    withdrawn_at: AwareDatetime
    audit_id: int


class ConsentGetAllResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    patient_id: UUID
    consents: list[ConsentRecord]


class ConsentGetOneResponse(ConsentRecord):
    model_config = ConfigDict(extra="forbid")


class AuditRecord(BaseModel):
    model_config = ConfigDict(extra="forbid")

    audit_id: int
    action: str
    actor_id: str
    actor_role: str
    occurred_at: AwareDatetime

    @field_validator("action")
    @classmethod
    def validate_action(cls, value: str) -> str:
        if value not in {"GRANT", "WITHDRAW", "UPDATE", "EXPIRE", "VIEW"}:
            raise ValueError("Validation Error")
        return value

    @field_validator("actor_role")
    @classmethod
    def validate_actor_role(cls, value: str) -> str:
        if value not in {"patient", "clinician", "dpo", "system"}:
            raise ValueError("Validation Error")
        return value


class ConsentAuditHistoryResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    patient_id: UUID
    entries: list[AuditRecord]

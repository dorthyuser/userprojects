from __future__ import annotations

from dataclasses import dataclass
from datetime import datetime
from uuid import UUID


@dataclass(slots=True)
class ConsentModel:
    consent_id: UUID
    patient_id: UUID
    purpose: str
    status: str
    legal_basis: str
    scope: list[str]
    consent_version: str
    channel: str
    granted_at: datetime
    expires_at: datetime | None
    withdrawn_at: datetime | None


@dataclass(slots=True)
class AuditModel:
    audit_id: int
    event_id: UUID
    consent_id: UUID | None
    patient_id: UUID
    action: str
    previous_state: dict[str, object] | None
    new_state: dict[str, object] | None
    actor_id: str
    actor_role: str
    source_ip: str | None
    user_agent: str | None
    occurred_at: datetime
    record_hash: str | None

from __future__ import annotations

from dataclasses import dataclass
from datetime import datetime
from uuid import UUID


@dataclass(slots=True)
class ConsentRecord:
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
class AuditRecord:
    audit_id: int
    action: str
    actor_id: str
    actor_role: str
    occurred_at: datetime

from dataclasses import dataclass
from datetime import datetime
from uuid import UUID

from app.schemas.consent_schema import AuditRecord, ConsentRecord


@dataclass(slots=True)
class ConsentState:
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
class AuditState:
    audit_id: int
    event_id: UUID
    consent_id: UUID | None
    patient_id: UUID
    action: str
    actor_id: str
    actor_role: str
    source_ip: str | None
    user_agent: str | None
    occurred_at: datetime
    record_hash: str | None


@dataclass(slots=True)
class ConsentRecordRow:
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

    def to_schema(self) -> ConsentRecord:
        return ConsentRecord(
            consent_id=self.consent_id,
            patient_id=self.patient_id,
            purpose=self.purpose,
            status=self.status,
            legal_basis=self.legal_basis,
            scope=self.scope,
            consent_version=self.consent_version,
            channel=self.channel,
            granted_at=self.granted_at,
            expires_at=self.expires_at,
            withdrawn_at=self.withdrawn_at,
        )


@dataclass(slots=True)
class AuditRecordRow:
    audit_id: int
    action: str
    actor_id: str
    actor_role: str
    occurred_at: datetime

    def to_schema(self) -> AuditRecord:
        return AuditRecord(
            audit_id=self.audit_id,
            action=self.action,
            actor_id=self.actor_id,
            actor_role=self.actor_role,
            occurred_at=self.occurred_at,
        )


ConsentRecord.from_row = staticmethod(
    lambda row: ConsentRecord(
        consent_id=row[0],
        patient_id=row[1],
        purpose=row[2],
        status=row[3],
        legal_basis=row[4],
        scope=row[5],
        consent_version=row[6],
        channel=row[7],
        granted_at=row[8],
        expires_at=row[9],
        withdrawn_at=row[10],
    )
)

AuditRecord.from_row = staticmethod(
    lambda row: AuditRecord(
        audit_id=row[0],
        action=row[1],
        actor_id=row[2],
        actor_role=row[3],
        occurred_at=row[4],
    )
)

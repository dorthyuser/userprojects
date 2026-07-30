from dataclasses import dataclass
from datetime import datetime
from typing import Optional


@dataclass(slots=True)
class AdverseEventRecord:
    ae_id: str
    trial_id: str
    site_id: str
    patient_id: str
    clinician_id: str
    event_date: datetime
    ae_term_code: str
    ae_term_name: str
    ctcae_grade: int
    serious: bool
    outcome: str
    action_taken: str
    narrative: str
    related_drug_id: Optional[str]
    reported_by: str
    submitted_at: datetime

    @classmethod
    def from_request(cls, ae_id: str, payload, submitted_at: datetime) -> "AdverseEventRecord":
        return cls(
            ae_id=ae_id,
            trial_id=payload.trialId,
            site_id=payload.siteId,
            patient_id=payload.patientId,
            clinician_id=payload.clinicianId,
            event_date=payload.eventDate,
            ae_term_code=payload.aeTermCode,
            ae_term_name=payload.aeTermName,
            ctcae_grade=payload.ctcaeGrade,
            serious=payload.serious,
            outcome=payload.outcome,
            action_taken=payload.actionTaken,
            narrative=payload.narrative,
            related_drug_id=payload.relatedDrugId,
            reported_by=str(payload.reportedBy),
            submitted_at=submitted_at,
        )


@dataclass(slots=True)
class NotificationRecord:
    notification_id: str
    ae_id: str
    trial_id: str
    site_id: str
    patient_id: str
    ae_term_name: str
    ctcae_grade: int
    serious: bool
    outcome: str
    priority: str
    acknowledged: bool
    acknowledged_by: Optional[str]
    acknowledged_at: Optional[datetime]
    sns_published: bool
    sns_message_id: Optional[str]

    @classmethod
    def from_adverse_event(cls, notification_id: str, ae_record: AdverseEventRecord) -> "NotificationRecord":
        return cls(
            notification_id=notification_id,
            ae_id=ae_record.ae_id,
            trial_id=ae_record.trial_id,
            site_id=ae_record.site_id,
            patient_id=ae_record.patient_id,
            ae_term_name=ae_record.ae_term_name,
            ctcae_grade=ae_record.ctcae_grade,
            serious=ae_record.serious,
            outcome=ae_record.outcome,
            priority="HIGH" if ae_record.ctcae_grade >= 3 else "NORMAL",
            acknowledged=False,
            acknowledged_by=None,
            acknowledged_at=None,
            sns_published=False,
            sns_message_id=None,
        )

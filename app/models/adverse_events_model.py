from dataclasses import dataclass
from datetime import datetime


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
    related_drug_id: str | None
    reported_by: str


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
    acknowledged_by: str | None
    acknowledged_at: datetime | None

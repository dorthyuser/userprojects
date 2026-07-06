from dataclasses import dataclass
from datetime import datetime


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
    priority: str
    outcome: str
    acknowledged: bool
    sns_published: bool
    created_at: datetime

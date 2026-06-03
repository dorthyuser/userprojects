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
    outcome: str
    priority: str
    acknowledged: bool
    sns_published: bool
    created_at: datetime

    def to_dict(self) -> dict[str, object]:
        return {
            "notificationId": self.notification_id,
            "aeId": self.ae_id,
            "trialId": self.trial_id,
            "siteId": self.site_id,
            "patientId": self.patient_id,
            "aeTermName": self.ae_term_name,
            "ctcaeGrade": self.ctcae_grade,
            "serious": self.serious,
            "priority": self.priority,
            "outcome": self.outcome,
            "acknowledged": self.acknowledged,
            "snsPublished": self.sns_published,
            "createdAt": self.created_at,
        }

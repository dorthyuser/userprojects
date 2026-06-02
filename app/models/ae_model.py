from dataclasses import dataclass
from datetime import datetime


@dataclass(slots=True)
class NotificationRecord:
    notificationId: str
    aeId: str
    trialId: str
    siteId: str
    patientId: str
    aeTermName: str
    ctcaeGrade: int
    serious: bool
    outcome: str
    priority: str
    acknowledged: bool
    snsPublished: bool
    createdAt: datetime

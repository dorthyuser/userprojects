from __future__ import annotations

from dataclasses import dataclass
from datetime import datetime
from typing import Optional


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
    acknowledged_by: Optional[str]
    acknowledged_at: Optional[datetime]
    created_at: datetime

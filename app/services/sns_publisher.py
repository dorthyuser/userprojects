import json
import logging
import os
from dataclasses import dataclass
from datetime import datetime, timezone
from typing import Any

import boto3

from app.models.adverse_event_model import AdverseEventRecord, NotificationRecord

logger = logging.getLogger(__name__)
_sns_client = boto3.client("sns", region_name=os.getenv("AWS_REGION", "eu-west-2"))


@dataclass(slots=True)
class SnsPublishResult:
    sns_published: bool
    sns_message_id: str | None
    received_at: datetime


def publish_adverse_event_notification(ae_record: AdverseEventRecord, notification_record: NotificationRecord) -> SnsPublishResult:
    received_at = datetime.now(timezone.utc)
    try:
        subject = f"Adverse Event: {ae_record.ae_term_name} — {ae_record.trial_id}"
        if ae_record.ctcae_grade == 5:
            subject = f"[FATAL][SAE] Adverse Event: {ae_record.ae_term_name} — {ae_record.trial_id}"
        elif ae_record.serious and ae_record.ctcae_grade >= 3:
            subject = f"[SAE][HIGH] Adverse Event: {ae_record.ae_term_name} — {ae_record.trial_id}"
        elif ae_record.serious and ae_record.ctcae_grade < 3:
            subject = f"[SAE] Adverse Event: {ae_record.ae_term_name} — {ae_record.trial_id}"
        elif not ae_record.serious and ae_record.ctcae_grade >= 3:
            subject = f"[HIGH] Adverse Event: {ae_record.ae_term_name} — {ae_record.trial_id}"
        message = {
            "ae_id": ae_record.ae_id,
            "notification_id": notification_record.notification_id,
            "trial_id": ae_record.trial_id,
            "site_id": ae_record.site_id,
            "patient_id": ae_record.patient_id,
            "ae_term": f"{ae_record.ae_term_name} ({ae_record.ae_term_code})",
            "ctcae_grade": ae_record.ctcae_grade,
            "serious": ae_record.serious,
            "priority": notification_record.priority,
            "outcome": ae_record.outcome,
            "event_date": ae_record.event_date.isoformat().replace("+00:00", "Z"),
            "reported_by": ae_record.reported_by,
            "submitted_at": ae_record.submitted_at.isoformat().replace("+00:00", "Z"),
        }
        response = _sns_client.publish(
            TopicArn=os.environ["SNS_TOPIC_ARN"],
            Subject=subject,
            Message=json.dumps(message),
            MessageAttributes={
                "ctcae_grade": {"DataType": "Number", "StringValue": str(ae_record.ctcae_grade)},
                "serious": {"DataType": "String", "StringValue": str(ae_record.serious).lower()},
                "priority": {"DataType": "String", "StringValue": notification_record.priority},
                "fatal": {"DataType": "String", "StringValue": str(ae_record.ctcae_grade == 5).lower()},
                "trial_id": {"DataType": "String", "StringValue": ae_record.trial_id},
                "site_id": {"DataType": "String", "StringValue": ae_record.site_id},
            },
        )
        return SnsPublishResult(True, response.get("MessageId"), received_at)
    except Exception as exc:
        logger.error("SNS publish failed: %s", str(exc), exc_info=True)
        return SnsPublishResult(False, None, received_at)

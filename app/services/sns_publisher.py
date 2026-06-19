import json
import logging
import os
from datetime import datetime, timezone

import boto3

from app.models.adverse_event_model import AdverseEventRecord, NotificationRecord

logger = logging.getLogger(__name__)
_sns_client = boto3.client("sns", region_name=os.environ.get("AWS_REGION", "eu-west-2"))


def _utc_now() -> str:
    return datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")


def publish_sns_notification(adverse_event: AdverseEventRecord, notification: NotificationRecord) -> dict[str, object]:
    subject = "Adverse Event: {} — {}".format(adverse_event.ae_term_name, adverse_event.trial_id)
    if adverse_event.ctcae_grade == 5:
        subject = "[FATAL][SAE] Adverse Event: {} — {}".format(adverse_event.ae_term_name, adverse_event.trial_id)
    elif adverse_event.serious and adverse_event.ctcae_grade >= 3:
        subject = "[SAE][HIGH] Adverse Event: {} — {}".format(adverse_event.ae_term_name, adverse_event.trial_id)
    elif adverse_event.serious and adverse_event.ctcae_grade < 3:
        subject = "[SAE] Adverse Event: {} — {}".format(adverse_event.ae_term_name, adverse_event.trial_id)
    elif not adverse_event.serious and adverse_event.ctcae_grade >= 3:
        subject = "[HIGH] Adverse Event: {} — {}".format(adverse_event.ae_term_name, adverse_event.trial_id)
    message = {
        "ae_id": adverse_event.ae_id,
        "notification_id": notification.notification_id,
        "trial_id": adverse_event.trial_id,
        "site_id": adverse_event.site_id,
        "patient_id": adverse_event.patient_id,
        "ae_term": f"{adverse_event.ae_term_name} ({adverse_event.ae_term_code})",
        "ctcae_grade": adverse_event.ctcae_grade,
        "serious": adverse_event.serious,
        "priority": notification.priority,
        "outcome": adverse_event.outcome,
        "event_date": adverse_event.event_date.isoformat().replace("+00:00", "Z"),
        "reported_by": adverse_event.reported_by,
        "submitted_at": adverse_event.submitted_at.isoformat().replace("+00:00", "Z"),
    }
    attributes = {
        "ctcae_grade": {"DataType": "Number", "StringValue": str(adverse_event.ctcae_grade)},
        "serious": {"DataType": "String", "StringValue": str(adverse_event.serious).lower()},
        "priority": {"DataType": "String", "StringValue": notification.priority},
        "fatal": {"DataType": "String", "StringValue": str(adverse_event.ctcae_grade == 5).lower()},
        "trial_id": {"DataType": "String", "StringValue": adverse_event.trial_id},
        "site_id": {"DataType": "String", "StringValue": adverse_event.site_id},
    }
    try:
        response = _sns_client.publish(
            TopicArn=os.environ["SNS_TOPIC_ARN"],
            Subject=subject,
            Message=json.dumps(message),
            MessageAttributes=attributes,
        )
        return {"sns_published": True, "sns_message_id": response.get("MessageId")}
    except Exception as exc:
        logger.error(
            json.dumps(
                {
                    "timestamp": _utc_now(),
                    "level": "ERROR",
                    "ae_id": adverse_event.ae_id,
                    "notification_id": notification.notification_id,
                    "trial_id": adverse_event.trial_id,
                    "error": f"{exc.__class__.__name__}: {str(exc)}",
                }
            )
        )
        return {"sns_published": False, "sns_message_id": None}

import json
import logging
from datetime import datetime

import boto3

logger = logging.getLogger(__name__)


def publish_message(
    ae_id: str,
    notification_id: str,
    trial_id: str,
    site_id: str,
    patient_id: str,
    ae_term_name: str,
    ae_term_code: str,
    ctcae_grade: int,
    serious: bool,
    priority: str,
    outcome: str,
    event_date: datetime,
    reported_by: str,
    submitted_at: datetime,
) -> str:
    target = __import__("os").environ["NOTIFICATION_TARGET"]
    client = boto3.client("sns")
    body = {
        "aeId": ae_id,
        "notificationId": notification_id,
        "trialId": trial_id,
        "siteId": site_id,
        "patientId": patient_id,
        "aeTerm": f"{ae_term_name} ({ae_term_code})",
        "ctcaeGrade": ctcae_grade,
        "serious": serious,
        "priority": priority,
        "outcome": outcome,
        "eventDate": event_date.isoformat().replace("+00:00", "Z"),
        "reportedBy": reported_by,
        "submittedAt": submitted_at.isoformat().replace("+00:00", "Z"),
    }
    attributes = {
        "ctcae_grade": {"DataType": "Number", "StringValue": str(ctcae_grade)},
        "serious": {"DataType": "String", "StringValue": str(serious).lower()},
        "priority": {"DataType": "String", "StringValue": priority},
        "fatal": {"DataType": "String", "StringValue": str(ctcae_grade == 5).lower()},
        "trial_id": {"DataType": "String", "StringValue": trial_id},
        "site_id": {"DataType": "String", "StringValue": site_id},
    }
    subject = f"Adverse Event: {ae_term_name} — {trial_id}"
    if ctcae_grade == 5:
        subject = f"[FATAL][SAE] {subject}"
    elif serious and ctcae_grade >= 3:
        subject = f"[SAE][HIGH] {subject}"
    elif serious and ctcae_grade < 3:
        subject = f"[SAE] {subject}"
    elif not serious and ctcae_grade >= 3:
        subject = f"[HIGH] {subject}"
    response = client.publish(
        TopicArn=target,
        Message=json.dumps(body),
        MessageAttributes=attributes,
        Subject=subject,
    )
    message_id = response.get("MessageId")
    if not message_id:
        raise RuntimeError("Messaging service did not return a message id")
    return message_id

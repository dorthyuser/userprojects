import json
import logging
from datetime import datetime, timezone
from typing import Any

from fastapi import HTTPException, Request

from app.db.connection import get_conn, release_conn
from app.models.adverse_event_model import AdverseEventRecord, NotificationRecord
from app.schemas.adverse_event_schema import AdverseEventCreateRequest, AdverseEventCreateResponse
from app.services.sns_publisher import publish_sns_notification
from app.services.validator import validate_and_coerce_adverse_event

logger = logging.getLogger(__name__)


def _utc_now() -> datetime:
    return datetime.now(timezone.utc)


def _log(message: str) -> None:
    logger.info(message)


def create_adverse_event(payload: AdverseEventCreateRequest, request: Request) -> AdverseEventCreateResponse:
    start = _utc_now()
    _log(json.dumps({"operation": "create_adverse_event", "resource": "adverse_event"}))
    validated = validate_and_coerce_adverse_event(payload)
    conn = None
    try:
        conn = get_conn()
        try:
            conn.rollback()
        except Exception:
            pass
        conn.autocommit = False
        with conn.cursor() as cursor:
            _log(json.dumps({"table": "trials", "operation": "SELECT"}))
            cursor.execute("SELECT id FROM trials WHERE trial_id = %s AND status = 'ACTIVE'", (validated.trialId,))
            if cursor.fetchone() is None:
                raise HTTPException(status_code=400, detail="Validation Error")
            _log(json.dumps({"table": "trial_enrolments", "operation": "SELECT"}))
            cursor.execute(
                "SELECT id FROM trial_enrolments WHERE trial_id = %s AND patient_id = %s AND status = 'ENROLLED'",
                (validated.trialId, validated.patientId),
            )
            if cursor.fetchone() is None:
                raise HTTPException(status_code=400, detail="Validation Error")
            _log(json.dumps({"table": "adverse_events", "operation": "SELECT"}))
            cursor.execute(
                "SELECT ae_id FROM adverse_events WHERE trial_id = %s AND patient_id = %s AND ae_term_code = %s AND ctcae_grade = %s AND submitted_at >= NOW() - (%s || ' seconds')::interval ORDER BY submitted_at DESC LIMIT 1",
                (validated.trialId, validated.patientId, validated.aeTermCode, validated.ctcaeGrade, int(validated.idempotencyWindowS)),
            )
            duplicate = cursor.fetchone()
            if duplicate is not None:
                raise HTTPException(status_code=409, detail="Validation Error")
            _log(json.dumps({"table": "adverse_events", "operation": "INSERT"}))
            cursor.execute(
                "SELECT 'AE-' || TO_CHAR(NOW(),'YYYY') || '-' || LPAD(NEXTVAL('ae_id_seq')::text, 6, '0')"
            )
            ae_row = cursor.fetchone()
            if ae_row is None:
                conn.rollback()
                raise HTTPException(status_code=500, detail="Internal Error")
            ae_id = ae_row[0]
            _log(json.dumps({"table": "ae_notifications", "operation": "INSERT"}))
            cursor.execute(
                "SELECT 'NOTIF-' || TO_CHAR(NOW(),'YYYY') || '-' || LPAD(NEXTVAL('notif_id_seq')::text, 6, '0')"
            )
            notif_row = cursor.fetchone()
            if notif_row is None:
                conn.rollback()
                raise HTTPException(status_code=500, detail="Internal Error")
            notification_id = notif_row[0]
            submitted_at = _utc_now()
            adverse_event = AdverseEventRecord.from_request(ae_id=ae_id, payload=validated, submitted_at=submitted_at)
            notification = NotificationRecord.from_adverse_event(notification_id=notification_id, adverse_event=adverse_event)
            cursor.execute(
                "INSERT INTO adverse_events (ae_id, trial_id, site_id, patient_id, clinician_id, event_date, ae_term_code, ae_term_name, ctcae_grade, serious, outcome, action_taken, narrative, related_drug_id, reported_by, submitted_at, created_at, updated_at) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s)",
                (
                    adverse_event.ae_id,
                    adverse_event.trial_id,
                    adverse_event.site_id,
                    adverse_event.patient_id,
                    adverse_event.clinician_id,
                    adverse_event.event_date,
                    adverse_event.ae_term_code,
                    adverse_event.ae_term_name,
                    adverse_event.ctcae_grade,
                    adverse_event.serious,
                    adverse_event.outcome,
                    adverse_event.action_taken,
                    adverse_event.narrative,
                    adverse_event.related_drug_id,
                    adverse_event.reported_by,
                    adverse_event.submitted_at,
                    adverse_event.created_at,
                    adverse_event.updated_at,
                ),
            )
            cursor.execute(
                "INSERT INTO ae_notifications (notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, outcome, priority, acknowledged, acknowledged_by, acknowledged_at, sns_published, sns_message_id, created_at, updated_at) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s)",
                (
                    notification.notification_id,
                    notification.ae_id,
                    notification.trial_id,
                    notification.site_id,
                    notification.patient_id,
                    notification.ae_term_name,
                    notification.ctcae_grade,
                    notification.serious,
                    notification.outcome,
                    notification.priority,
                    notification.acknowledged,
                    notification.acknowledged_by,
                    notification.acknowledged_at,
                    notification.sns_published,
                    notification.sns_message_id,
                    notification.created_at,
                    notification.updated_at,
                ),
            )
            conn.commit()
        sns_result = publish_sns_notification(adverse_event=adverse_event, notification=notification)
        if sns_result["sns_published"] and sns_result["sns_message_id"] is not None:
            try:
                conn = get_conn()
                try:
                    conn.rollback()
                except Exception:
                    pass
                conn.autocommit = False
                with conn.cursor() as cursor:
                    _log(json.dumps({"table": "ae_notifications", "operation": "UPDATE"}))
                    cursor.execute(
                        "UPDATE ae_notifications SET sns_published = %s, sns_message_id = %s WHERE notification_id = %s",
                        (True, sns_result["sns_message_id"], notification.notification_id),
                    )
                conn.commit()
            except Exception as exc:
                if conn is not None:
                    conn.rollback()
                logger.warning("SNS status update failed: %s", str(exc))
        duration_ms = int((_utc_now() - start).total_seconds() * 1000)
        return AdverseEventCreateResponse(
            status="success",
            aeId=adverse_event.ae_id,
            notificationId=notification.notification_id,
            snsPublished=sns_result["sns_published"],
            snsMessageId=sns_result["sns_message_id"],
            message=(
                "Adverse event recorded. Notification stored and SNS dispatched."
                if sns_result["sns_published"]
                else "Adverse event recorded. Notification stored. SNS dispatch failed — logged."
            ),
            receivedAt=submitted_at,
        )
    except HTTPException:
        raise
    except Exception as exc:
        logger.error("Unexpected error: %s", str(exc), exc_info=True)
        raise HTTPException(status_code=500, detail="Internal Error")
    finally:
        if conn is not None:
            release_conn(conn)

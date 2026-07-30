import json
import logging
import os
from datetime import datetime, timezone
from typing import Any

from fastapi import HTTPException, Request

from app.db.connection import get_conn, release_conn
from app.models.adverse_event_model import AdverseEventRecord, NotificationRecord
from app.schemas.adverse_event_schema import AdverseEventCreateRequest, AdverseEventCreateResponse
from app.services.sns_publisher import publish_adverse_event_notification
from app.services.validator import validate_and_coerce_adverse_event

logger = logging.getLogger(__name__)


def _utc_now() -> datetime:
    return datetime.now(timezone.utc)


def _log(message: str) -> None:
    logger.info(message)


def submit_adverse_event(payload: AdverseEventCreateRequest, request: Request) -> AdverseEventCreateResponse:
    start = _utc_now()
    request_id = getattr(request.state, "request_id", None)
    _log("adverse_event_submit_start")
    validated = validate_and_coerce_adverse_event(payload)
    conn = None
    ae_id = None
    notification_id = None
    try:
        conn = get_conn()
        conn.rollback()
        conn.autocommit = False
        with conn.cursor() as cursor:
            logger.info(json.dumps({"step": "DB_WRITE", "operation": "SELECT", "table": "trials"}))
            cursor.execute("SELECT id FROM trials WHERE trial_id = %s AND status = 'ACTIVE'", (validated.trialId,))
            if cursor.fetchone() is None:
                conn.rollback()
                raise HTTPException(status_code=400, detail="Validation Error")
            logger.info(json.dumps({"step": "DB_WRITE", "operation": "SELECT", "table": "trial_enrolments"}))
            cursor.execute(
                "SELECT id FROM trial_enrolments WHERE trial_id = %s AND patient_id = %s AND status = 'ENROLLED'",
                (validated.trialId, validated.patientId),
            )
            if cursor.fetchone() is None:
                conn.rollback()
                raise HTTPException(status_code=400, detail="Validation Error")
            logger.info(json.dumps({"step": "DB_WRITE", "operation": "SELECT", "table": "adverse_events"}))
            cursor.execute(
                "SELECT ae_id FROM adverse_events WHERE trial_id = %s AND patient_id = %s AND ae_term_code = %s AND ctcae_grade = %s AND submitted_at >= NOW() - (%s || ' seconds')::interval ORDER BY submitted_at DESC LIMIT 1",
                (validated.trialId, validated.patientId, validated.aeTermCode, validated.ctcaeGrade, int(os.getenv("IDEMPOTENCY_WINDOW_S", "60"))),
            )
            duplicate = cursor.fetchone()
            if duplicate is not None:
                conn.rollback()
                raise HTTPException(status_code=409, detail="Validation Error")
            logger.info(json.dumps({"step": "DB_WRITE", "operation": "SELECT", "table": "ae_id_seq"}))
            cursor.execute("SELECT 'AE-' || TO_CHAR(NOW(),'YYYY') || '-' || LPAD(NEXTVAL('ae_id_seq')::text, 6, '0')")
            ae_row = cursor.fetchone()
            if ae_row is None:
                conn.rollback()
                raise HTTPException(status_code=500, detail="Internal Error")
            ae_id = ae_row[0]
            logger.info(json.dumps({"step": "DB_WRITE", "operation": "SELECT", "table": "notif_id_seq"}))
            cursor.execute("SELECT 'NOTIF-' || TO_CHAR(NOW(),'YYYY') || '-' || LPAD(NEXTVAL('notif_id_seq')::text, 6, '0')")
            notif_row = cursor.fetchone()
            if notif_row is None:
                conn.rollback()
                raise HTTPException(status_code=500, detail="Internal Error")
            notification_id = notif_row[0]
            submitted_at = _utc_now()
            ae_record = AdverseEventRecord.from_request(ae_id, validated, submitted_at)
            notification_record = NotificationRecord.from_adverse_event(notification_id, ae_record)
            logger.info(json.dumps({"step": "DB_WRITE", "operation": "INSERT", "table": "adverse_events"}))
            cursor.execute(
                "INSERT INTO adverse_events (ae_id, trial_id, site_id, patient_id, clinician_id, event_date, ae_term_code, ae_term_name, ctcae_grade, serious, outcome, action_taken, narrative, related_drug_id, reported_by, submitted_at, created_at, updated_at) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, NOW(), NOW()) RETURNING id",
                (
                    ae_record.ae_id,
                    ae_record.trial_id,
                    ae_record.site_id,
                    ae_record.patient_id,
                    ae_record.clinician_id,
                    ae_record.event_date,
                    ae_record.ae_term_code,
                    ae_record.ae_term_name,
                    ae_record.ctcae_grade,
                    ae_record.serious,
                    ae_record.outcome,
                    ae_record.action_taken,
                    ae_record.narrative,
                    ae_record.related_drug_id,
                    ae_record.reported_by,
                    ae_record.submitted_at,
                ),
            )
            if cursor.fetchone() is None:
                conn.rollback()
                raise HTTPException(status_code=500, detail="Internal Error")
            logger.info(json.dumps({"step": "DB_WRITE", "operation": "INSERT", "table": "ae_notifications"}))
            cursor.execute(
                "INSERT INTO ae_notifications (notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, outcome, priority, acknowledged, acknowledged_by, acknowledged_at, sns_published, sns_message_id, created_at, updated_at) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, NOW(), NOW()) RETURNING id",
                (
                    notification_record.notification_id,
                    notification_record.ae_id,
                    notification_record.trial_id,
                    notification_record.site_id,
                    notification_record.patient_id,
                    notification_record.ae_term_name,
                    notification_record.ctcae_grade,
                    notification_record.serious,
                    notification_record.outcome,
                    notification_record.priority,
                    notification_record.acknowledged,
                    notification_record.acknowledged_by,
                    notification_record.acknowledged_at,
                    notification_record.sns_published,
                    notification_record.sns_message_id,
                ),
            )
            if cursor.fetchone() is None:
                conn.rollback()
                raise HTTPException(status_code=500, detail="Internal Error")
        conn.commit()
    except HTTPException:
        raise
    except Exception as exc:
        if conn is not None:
            conn.rollback()
        logger.error("Database error: %s", str(exc), exc_info=True)
        raise HTTPException(status_code=503, detail="Database Error") from exc
    finally:
        if conn is not None:
            release_conn(conn)
    sns_result = publish_adverse_event_notification(ae_record, notification_record)
    if sns_result.sns_published and sns_result.sns_message_id:
        conn = None
        try:
            conn = get_conn()
            conn.rollback()
            conn.autocommit = False
            with conn.cursor() as cursor:
                logger.info(json.dumps({"step": "DB_WRITE", "operation": "UPDATE", "table": "ae_notifications"}))
                cursor.execute(
                    "UPDATE ae_notifications SET sns_published = %s, sns_message_id = %s, updated_at = NOW() WHERE notification_id = %s",
                    (True, sns_result.sns_message_id, notification_record.notification_id),
                )
            conn.commit()
        except Exception as exc:
            if conn is not None:
                conn.rollback()
            logger.warning("SNS status update failed: %s", str(exc))
        finally:
            if conn is not None:
                release_conn(conn)
    duration_ms = int((_utc_now() - start).total_seconds() * 1000)
    return AdverseEventCreateResponse(
        status="success",
        aeId=ae_record.ae_id,
        notificationId=notification_record.notification_id,
        snsPublished=sns_result.sns_published,
        snsMessageId=sns_result.sns_message_id,
        message=("Adverse event recorded. Notification stored and SNS dispatched." if sns_result.sns_published else "Adverse event recorded. Notification stored. SNS dispatch failed — logged."),
        receivedAt=sns_result.received_at.isoformat().replace("+00:00", "Z"),
    )

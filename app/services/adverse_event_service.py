import json
import logging
from datetime import datetime, timezone
from typing import Any

from fastapi import HTTPException

from app.db.connection import get_conn, release_conn
from app.models.adverse_event_model import AdverseEventRecord, NotificationRecord
from app.schemas.adverse_event_schema import AdverseEventCreateRequest, AdverseEventCreateResponse
from app.services.sns_publisher import publish_adverse_event_notification
from app.services.validator import validate_adverse_event_payload

logger = logging.getLogger(__name__)


def _utc_now() -> datetime:
    return datetime.now(timezone.utc)


def create_adverse_event(payload: AdverseEventCreateRequest, request_id: str | None = None) -> AdverseEventCreateResponse:
    logger.info(json.dumps({"operation": "create_adverse_event", "resource": "adverse_event"}))
    validated = validate_adverse_event_payload(payload)
    conn = None
    try:
        conn = get_conn()
        try:
            conn.rollback()
        except Exception:
            pass
        conn.autocommit = False
        with conn.cursor() as cursor:
            cursor.execute(
                "SELECT id FROM trials WHERE trial_id = %s AND status = 'ACTIVE'",
                (validated.trialId,),
            )
            if cursor.fetchone() is None:
                raise HTTPException(status_code=400, detail="Validation Error")
            cursor.execute(
                "SELECT id FROM trial_enrolments WHERE trial_id = %s AND patient_id = %s AND status = 'ENROLLED'",
                (validated.trialId, validated.patientId),
            )
            if cursor.fetchone() is None:
                raise HTTPException(status_code=400, detail="Validation Error")
            cursor.execute(
                "SELECT ae_id FROM adverse_events WHERE trial_id = %s AND patient_id = %s AND ae_term_code = %s AND ctcae_grade = %s AND submitted_at >= NOW() - (%s || ' seconds')::interval ORDER BY submitted_at DESC LIMIT 1",
                (validated.trialId, validated.patientId, validated.aeTermCode, validated.ctcaeGrade, 60),
            )
            duplicate = cursor.fetchone()
            if duplicate is not None:
                raise HTTPException(status_code=409, detail="Validation Error")
            cursor.execute("SELECT 'AE-' || TO_CHAR(NOW(),'YYYY') || '-' || LPAD(NEXTVAL('ae_id_seq')::text, 6, '0')")
            ae_row = cursor.fetchone()
            if ae_row is None:
                raise HTTPException(status_code=500, detail="Internal Error")
            ae_id = ae_row[0]
            cursor.execute("SELECT 'NOTIF-' || TO_CHAR(NOW(),'YYYY') || '-' || LPAD(NEXTVAL('notif_id_seq')::text, 6, '0')")
            notif_row = cursor.fetchone()
            if notif_row is None:
                raise HTTPException(status_code=500, detail="Internal Error")
            notification_id = notif_row[0]
            submitted_at = _utc_now()
            ae_record = AdverseEventRecord(
                ae_id=ae_id,
                trial_id=validated.trialId,
                site_id=validated.siteId,
                patient_id=validated.patientId,
                clinician_id=validated.clinicianId,
                event_date=validated.eventDate,
                ae_term_code=validated.aeTermCode,
                ae_term_name=validated.aeTermName,
                ctcae_grade=validated.ctcaeGrade,
                serious=validated.serious,
                outcome=validated.outcome,
                action_taken=validated.actionTaken,
                narrative=validated.narrative,
                related_drug_id=validated.relatedDrugId,
                reported_by=validated.reportedBy,
                submitted_at=submitted_at,
            )
            notification_record = NotificationRecord(
                notification_id=notification_id,
                ae_id=ae_id,
                trial_id=validated.trialId,
                site_id=validated.siteId,
                patient_id=validated.patientId,
                ae_term_name=validated.aeTermName,
                ctcae_grade=validated.ctcaeGrade,
                serious=validated.serious,
                outcome=validated.outcome,
                priority="HIGH" if validated.ctcaeGrade >= 3 else "NORMAL",
                acknowledged=False,
                acknowledged_by=None,
                acknowledged_at=None,
                sns_published=False,
                sns_message_id=None,
                created_at=submitted_at,
            )
            cursor.execute(
                "INSERT INTO adverse_events (ae_id, trial_id, site_id, patient_id, clinician_id, event_date, ae_term_code, ae_term_name, ctcae_grade, serious, outcome, action_taken, narrative, related_drug_id, reported_by, submitted_at) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s)",
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
            cursor.execute(
                "INSERT INTO ae_notifications (notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, outcome, priority, acknowledged, acknowledged_by, acknowledged_at, sns_published, sns_message_id, created_at) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s::varchar, %s, %s, %s, %s, %s, %s)",
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
                    notification_record.created_at,
                ),
            )
            conn.commit()
        sns_result = publish_adverse_event_notification(ae_record=ae_record, notification_record=notification_record)
        if sns_result.get("sns_published"):
            try:
                conn = get_conn()
                try:
                    conn.rollback()
                except Exception:
                    pass
                conn.autocommit = False
                with conn.cursor() as cursor:
                    cursor.execute(
                        "UPDATE ae_notifications SET sns_published = %s, sns_message_id = %s WHERE notification_id = %s",
                        (True, sns_result.get("sns_message_id"), notification_record.notification_id),
                    )
                conn.commit()
            except Exception as exc:
                if conn is not None:
                    try:
                        conn.rollback()
                    except Exception:
                        pass
                logger.warning(json.dumps({"step": "SNS_STATUS_UPDATE", "outcome": "FAILURE", "error": str(exc)}))
        return AdverseEventCreateResponse(
            status="success",
            aeId=ae_record.ae_id,
            notificationId=notification_record.notification_id,
            snsPublished=bool(sns_result.get("sns_published")),
            snsMessageId=sns_result.get("sns_message_id"),
            message=(
                "Adverse event recorded. Notification stored and SNS dispatched."
                if sns_result.get("sns_published")
                else "Adverse event recorded. Notification stored. SNS dispatch failed — logged."
            ),
            receivedAt=submitted_at,
        )
    except HTTPException:
        if conn is not None:
            try:
                conn.rollback()
            except Exception:
                pass
        raise
    except Exception as exc:
        if conn is not None:
            try:
                conn.rollback()
            except Exception:
                pass
        logger.error(json.dumps({"step": "DB_WRITE", "outcome": "FAILURE", "error": str(exc)}))
        raise HTTPException(status_code=500, detail="Internal Error")
    finally:
        if conn is not None:
            release_conn(conn)

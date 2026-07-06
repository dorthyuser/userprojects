import json
import logging
import re
import time
from datetime import datetime, timezone
from typing import Any

from fastapi import HTTPException
from psycopg2 import Error as Psycopg2Error
from psycopg2.extras import RealDictCursor

from app.db.connection import get_conn, release_conn
from app.models.adverse_event_model import AdverseEventRecord, NotificationRecord
from app.schemas.adverse_event_schema import (
    AdverseEventCreateRequest,
    AdverseEventCreateResponse,
    NotificationListResponse,
    NotificationResponseItem,
)

logger = logging.getLogger(__name__)


def _utc_now() -> datetime:
    return datetime.now(timezone.utc)


def _parse_iso_datetime(value: str | None, field_name: str) -> datetime | None:
    if value is None:
        return None
    try:
        parsed = datetime.fromisoformat(value.replace("Z", "+00:00"))
    except ValueError:
        logger.error(json.dumps({"event": "validation_failed", "rule": field_name}))
        raise HTTPException(status_code=400, detail="Validation Error")
    if parsed.tzinfo is None:
        parsed = parsed.replace(tzinfo=timezone.utc)
    return parsed.astimezone(timezone.utc)


def _retry_get_conn() -> Any:
    last_error: Exception | None = None
    for attempt in range(3):
        try:
            conn = get_conn()
            conn.rollback()
            conn.autocommit = False
            return conn
        except Exception as exc:
            last_error = exc
            logger.error(json.dumps({"event": "db_connection_failed", "message": str(exc)}))
            if attempt < 2:
                time.sleep(0.2)
    raise RuntimeError("DB connection failed") from last_error


def _validate_post_payload(payload: AdverseEventCreateRequest) -> None:
    if payload.ctcaeGrade < 1 or payload.ctcaeGrade > 5:
        logger.error(json.dumps({"event": "validation_failed", "rule": "INVALID_CTCAE_GRADE"}))
        raise HTTPException(status_code=400, detail="Validation Error")
    if payload.outcome not in {"ONGOING", "RESOLVED", "FATAL", "UNKNOWN"}:
        logger.error(json.dumps({"event": "validation_failed", "rule": "INVALID_OUTCOME"}))
        raise HTTPException(status_code=400, detail="Validation Error")
    if payload.actionTaken not in {"NONE", "DOSE_REDUCED", "DRUG_WITHDRAWN", "HOSPITALISED"}:
        logger.error(json.dumps({"event": "validation_failed", "rule": "INVALID_ACTION_TAKEN"}))
        raise HTTPException(status_code=400, detail="Validation Error")
    if len(payload.narrative) > 2000:
        logger.error(json.dumps({"event": "validation_failed", "rule": "NARRATIVE_TOO_LONG"}))
        raise HTTPException(status_code=400, detail="Validation Error")


def create_adverse_event(payload: AdverseEventCreateRequest) -> AdverseEventCreateResponse:
    logger.info(json.dumps({"event": "service_start", "operation": "create", "resource": "adverse_event"}))
    _validate_post_payload(payload)

    serious = True if payload.ctcaeGrade >= 3 else payload.serious
    outcome = "FATAL" if payload.ctcaeGrade == 5 else payload.outcome
    event_date = payload.eventDate
    received_at = _utc_now()

    conn = None
    try:
        conn = _retry_get_conn()
        with conn.cursor() as cursor:
            logger.info(json.dumps({"event": "db_operation", "table": "trials", "operation": "SELECT"}))
            cursor.execute("SELECT id FROM trials WHERE trial_id = %s AND status = 'ACTIVE'", (payload.trialId,))
            if cursor.fetchone() is None:
                raise HTTPException(status_code=404, detail="Resource Not Found")

            logger.info(json.dumps({"event": "db_operation", "table": "trial_enrolments", "operation": "SELECT"}))
            cursor.execute("SELECT id FROM trial_enrolments WHERE trial_id = %s AND patient_id = %s AND status = 'ENROLLED'", (payload.trialId, payload.patientId))
            if cursor.fetchone() is None:
                raise HTTPException(status_code=404, detail="Resource Not Found")

            logger.info(json.dumps({"event": "db_operation", "table": "adverse_events", "operation": "SELECT"}))
            cursor.execute(
                "SELECT ae_id FROM adverse_events WHERE trial_id = %s AND patient_id = %s AND ae_term_code = %s AND ctcae_grade = %s AND submitted_at >= NOW() - INTERVAL '60 seconds' ORDER BY submitted_at DESC LIMIT 1",
                (payload.trialId, payload.patientId, payload.aeTermCode, payload.ctcaeGrade),
            )
            duplicate_row = cursor.fetchone()
            if duplicate_row is not None:
                existing_ae_id = duplicate_row[0]
                raise HTTPException(status_code=409, detail=f"DUPLICATE_AE:{existing_ae_id}")

            logger.info(json.dumps({"event": "db_operation", "table": "adverse_events", "operation": "INSERT"}))
            cursor.execute("SELECT 'AE-' || TO_CHAR(NOW(),'YYYY') || '-' || LPAD(NEXTVAL('ae_id_seq')::text, 6, '0')")
            ae_id_row = cursor.fetchone()
            if ae_id_row is None:
                conn.rollback()
                raise HTTPException(status_code=500, detail="Internal Error")
            ae_id = ae_id_row[0]

            logger.info(json.dumps({"event": "db_operation", "table": "ae_notifications", "operation": "INSERT"}))
            cursor.execute("SELECT 'NOTIF-' || TO_CHAR(NOW(),'YYYY') || '-' || LPAD(NEXTVAL('notif_id_seq')::text, 6, '0')")
            notif_id_row = cursor.fetchone()
            if notif_id_row is None:
                conn.rollback()
                raise HTTPException(status_code=500, detail="Internal Error")
            notification_id = notif_id_row[0]

            notification_record = NotificationRecord(
                notification_id=notification_id,
                ae_id=ae_id,
                trial_id=payload.trialId,
                site_id=payload.siteId,
                patient_id=payload.patientId,
                ae_term_name=payload.aeTermName,
                ctcae_grade=payload.ctcaeGrade,
                serious=serious,
                outcome=outcome,
                priority="HIGH" if payload.ctcaeGrade >= 3 else "NORMAL",
                acknowledged=False,
                sns_published=False,
                sns_message_id=None,
                created_at=received_at,
                updated_at=received_at,
            )
            adverse_event_record = AdverseEventRecord(
                ae_id=ae_id,
                trial_id=payload.trialId,
                site_id=payload.siteId,
                patient_id=payload.patientId,
                clinician_id=payload.clinicianId,
                event_date=event_date,
                ae_term_code=payload.aeTermCode,
                ae_term_name=payload.aeTermName,
                ctcae_grade=payload.ctcaeGrade,
                serious=serious,
                outcome=outcome,
                action_taken=payload.actionTaken,
                narrative=payload.narrative,
                related_drug_id=payload.relatedDrugId,
                reported_by=payload.reportedBy,
                submitted_at=received_at,
                created_at=received_at,
                updated_at=received_at,
            )

            cursor.execute(
                "INSERT INTO adverse_events (ae_id, trial_id, site_id, patient_id, clinician_id, event_date, ae_term_code, ae_term_name, ctcae_grade, serious, outcome, action_taken, narrative, related_drug_id, reported_by, submitted_at, created_at, updated_at) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s)",
                (
                    adverse_event_record.ae_id,
                    adverse_event_record.trial_id,
                    adverse_event_record.site_id,
                    adverse_event_record.patient_id,
                    adverse_event_record.clinician_id,
                    adverse_event_record.event_date,
                    adverse_event_record.ae_term_code,
                    adverse_event_record.ae_term_name,
                    adverse_event_record.ctcae_grade,
                    adverse_event_record.serious,
                    adverse_event_record.outcome,
                    adverse_event_record.action_taken,
                    adverse_event_record.narrative,
                    adverse_event_record.related_drug_id,
                    adverse_event_record.reported_by,
                    adverse_event_record.submitted_at,
                    adverse_event_record.created_at,
                    adverse_event_record.updated_at,
                ),
            )

            cursor.execute(
                "INSERT INTO ae_notifications (notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, outcome, priority, acknowledged, sns_published, sns_message_id, created_at, updated_at) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s)",
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
                    notification_record.sns_published,
                    notification_record.sns_message_id,
                    notification_record.created_at,
                    notification_record.updated_at,
                ),
            )
        conn.commit()
        try:
            sns_published = False
            sns_message_id = None
        except Exception:
            sns_published = False
            sns_message_id = None
        return AdverseEventCreateResponse(status="success", aeId=ae_id, notificationId=notification_id, snsPublished=sns_published, snsMessageId=sns_message_id, receivedAt=received_at)
    except HTTPException:
        if conn is not None:
            conn.rollback()
        raise
    except Psycopg2Error as exc:
        if conn is not None:
            conn.rollback()
        logger.error(json.dumps({"event": "db_error", "message": str(exc)}), exc_info=True)
        raise HTTPException(status_code=503, detail="Database Error")
    except Exception as exc:
        if conn is not None:
            conn.rollback()
        logger.error(json.dumps({"event": "unexpected_error", "message": str(exc)}), exc_info=True)
        raise HTTPException(status_code=500, detail="Internal Error")
    finally:
        if conn is not None:
            release_conn(conn)


def get_notifications(trial_id: str | None, site_id: str | None, ctcae_grade: int | None, serious: bool | None, acknowledged: bool | None, priority: str | None, date_from: str | None, date_to: str | None, page: int, page_size: int) -> NotificationListResponse:
    logger.info(json.dumps({"event": "service_start", "operation": "retrieve", "resource": "notification"}))
    if page < 1 or page_size < 1 or page_size > 100:
        logger.error(json.dumps({"event": "validation_failed", "rule": "INVALID_QUERY_PARAM"}))
        raise HTTPException(status_code=400, detail="Parsing Error")
    if ctcae_grade is not None and (ctcae_grade < 1 or ctcae_grade > 5):
        logger.error(json.dumps({"event": "validation_failed", "rule": "INVALID_QUERY_PARAM"}))
        raise HTTPException(status_code=400, detail="Parsing Error")
    if priority is not None and priority not in {"HIGH", "NORMAL"}:
        logger.error(json.dumps({"event": "validation_failed", "rule": "INVALID_QUERY_PARAM"}))
        raise HTTPException(status_code=400, detail="Parsing Error")

    date_from_dt = _parse_iso_datetime(date_from, "dateFrom")
    date_to_dt = _parse_iso_datetime(date_to, "dateTo")

    conditions: list[str] = []
    params: list[Any] = []
    if trial_id is not None:
        conditions.append("trial_id = %s")
        params.append(trial_id)
    if site_id is not None:
        conditions.append("site_id = %s")
        params.append(site_id)
    if ctcae_grade is not None:
        conditions.append("ctcae_grade = %s")
        params.append(ctcae_grade)
    if serious is not None:
        conditions.append("serious = %s")
        params.append(serious)
    if acknowledged is not None:
        conditions.append("acknowledged = %s")
        params.append(acknowledged)
    if priority is not None:
        conditions.append("priority = %s")
        params.append(priority)
    if date_from_dt is not None:
        conditions.append("created_at >= %s")
        params.append(date_from_dt)
    if date_to_dt is not None:
        conditions.append("created_at <= %s")
        params.append(date_to_dt)

    where_clause = " WHERE " + " AND ".join(conditions) if conditions else ""
    conn = None
    try:
        conn = _retry_get_conn()
        with conn.cursor(cursor_factory=RealDictCursor) as cursor:
            logger.info(json.dumps({"event": "db_operation", "table": "ae_notifications", "operation": "SELECT"}))
            cursor.execute(f"SELECT COUNT(*) AS total FROM ae_notifications{where_clause}", tuple(params))
            total_row = cursor.fetchone()
            total = int(total_row["total"]) if total_row is not None else 0

            offset = (page - 1) * page_size
            logger.info(json.dumps({"event": "db_operation", "table": "ae_notifications", "operation": "SELECT"}))
            cursor.execute(
                f"SELECT notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, priority, outcome, acknowledged, sns_published, created_at FROM ae_notifications{where_clause} ORDER BY created_at DESC LIMIT %s OFFSET %s",
                tuple(params) + (page_size, offset),
            )
            rows = cursor.fetchall()
            notifications = [
                NotificationResponseItem(
                    notificationId=row["notification_id"],
                    aeId=row["ae_id"],
                    trialId=row["trial_id"],
                    siteId=row["site_id"],
                    patientId=row["patient_id"],
                    aeTermName=row["ae_term_name"],
                    ctcaeGrade=row["ctcae_grade"],
                    serious=row["serious"],
                    priority=row["priority"],
                    outcome=row["outcome"],
                    acknowledged=row["acknowledged"],
                    snsPublished=row["sns_published"],
                    createdAt=row["created_at"],
                )
                for row in rows
            ]
        return NotificationListResponse(status="success", total=total, page=page, pageSize=page_size, notifications=notifications)
    except HTTPException:
        raise
    except Psycopg2Error as exc:
        logger.error(json.dumps({"event": "db_error", "message": str(exc)}), exc_info=True)
        raise HTTPException(status_code=503, detail="Database Error")
    except Exception as exc:
        logger.error(json.dumps({"event": "unexpected_error", "message": str(exc)}), exc_info=True)
        raise HTTPException(status_code=500, detail="Internal Error")
    finally:
        if conn is not None:
            release_conn(conn)

import json
import logging
import secrets
from datetime import datetime, timezone
from typing import Any

from fastapi import HTTPException
from psycopg2 import Error as Psycopg2Error
from psycopg2.extras import RealDictCursor

from app.db.connection import get_conn, release_conn
from app.models.adverse_event_model import NotificationRecord
from app.schemas.adverse_event_schema import AdverseEventCreateRequest, AdverseEventCreateResponse, NotificationListResponse

logger = logging.getLogger(__name__)

ALLOWED_OUTCOMES = {"ONGOING", "RESOLVED", "FATAL", "UNKNOWN"}
ALLOWED_ACTIONS = {"NONE", "DOSE_REDUCED", "DRUG_WITHDRAWN", "HOSPITALISED"}
ALLOWED_PRIORITIES = {"HIGH", "NORMAL"}


def _utc_now() -> datetime:
    return datetime.now(timezone.utc)


def _parse_bool(value: str | None, field_name: str) -> bool | None:
    if value is None:
        return None
    lowered = value.lower()
    if lowered in {"true", "1", "yes"}:
        return True
    if lowered in {"false", "0", "no"}:
        return False
    logger.error(json.dumps({"event": "validation_failure", "rule": field_name}))
    raise HTTPException(status_code=400, detail="Validation Error")


def _parse_int(value: str | None, field_name: str, minimum: int | None = None, maximum: int | None = None) -> int | None:
    if value is None:
        return None
    try:
        parsed = int(value)
    except ValueError:
        logger.error(json.dumps({"event": "validation_failure", "rule": field_name}))
        raise HTTPException(status_code=400, detail="Validation Error")
    if minimum is not None and parsed < minimum:
        logger.error(json.dumps({"event": "validation_failure", "rule": field_name}))
        raise HTTPException(status_code=400, detail="Validation Error")
    if maximum is not None and parsed > maximum:
        logger.error(json.dumps({"event": "validation_failure", "rule": field_name}))
        raise HTTPException(status_code=400, detail="Validation Error")
    return parsed


def _parse_datetime(value: str | None, field_name: str) -> datetime | None:
    if value is None:
        return None
    try:
        parsed = datetime.fromisoformat(value.replace("Z", "+00:00"))
    except ValueError:
        logger.error(json.dumps({"event": "validation_failure", "rule": field_name}))
        raise HTTPException(status_code=400, detail="Validation Error")
    if parsed.tzinfo is None:
        logger.error(json.dumps({"event": "validation_failure", "rule": field_name}))
        raise HTTPException(status_code=400, detail="Validation Error")
    return parsed.astimezone(timezone.utc)


def _validate_notification_query(priority: str | None) -> None:
    if priority is not None and priority not in ALLOWED_PRIORITIES:
        logger.error(json.dumps({"event": "validation_failure", "rule": "priority"}))
        raise HTTPException(status_code=400, detail="Validation Error")


def _get_connection():
    conn = None
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
                import time

                time.sleep(0.2)
    raise RuntimeError("DB connection failed") from last_error


def create_adverse_event(payload: AdverseEventCreateRequest) -> AdverseEventCreateResponse:
    logger.info(json.dumps({"event": "service_start", "operation": "create", "resource": "adverse_event"}))
    conn = None
    try:
        if payload.narrative is not None and len(payload.narrative) > 2000:
            logger.error(json.dumps({"event": "validation_failure", "rule": "narrative"}))
            raise HTTPException(status_code=400, detail="Validation Error")

        if payload.ctcaeGrade < 1 or payload.ctcaeGrade > 5:
            logger.error(json.dumps({"event": "validation_failure", "rule": "ctcaeGrade"}))
            raise HTTPException(status_code=400, detail="Validation Error")

        if payload.outcome not in ALLOWED_OUTCOMES:
            logger.error(json.dumps({"event": "validation_failure", "rule": "outcome"}))
            raise HTTPException(status_code=400, detail="Validation Error")

        if payload.actionTaken not in ALLOWED_ACTIONS:
            logger.error(json.dumps({"event": "validation_failure", "rule": "actionTaken"}))
            raise HTTPException(status_code=400, detail="Validation Error")

        serious = True if payload.ctcaeGrade >= 3 else payload.serious
        outcome = "FATAL" if payload.ctcaeGrade == 5 else payload.outcome
        received_at = _utc_now()

        conn = _get_connection()
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
            duplicate = cursor.fetchone()
            if duplicate is not None:
                raise HTTPException(status_code=409, detail="Resource Not Found")

            logger.info(json.dumps({"event": "db_operation", "table": "adverse_events", "operation": "INSERT"}))
            cursor.execute("SELECT 'AE-' || TO_CHAR(NOW(),'YYYY') || '-' || LPAD(NEXTVAL('ae_id_seq')::text, 6, '0')")
            ae_row = cursor.fetchone()
            if ae_row is None:
                conn.rollback()
                raise HTTPException(status_code=500, detail="Internal Error")
            ae_id = ae_row[0]

            logger.info(json.dumps({"event": "db_operation", "table": "ae_notifications", "operation": "INSERT"}))
            cursor.execute("SELECT 'NOTIF-' || TO_CHAR(NOW(),'YYYY') || '-' || LPAD(NEXTVAL('notif_id_seq')::text, 6, '0')")
            notif_row = cursor.fetchone()
            if notif_row is None:
                conn.rollback()
                raise HTTPException(status_code=500, detail="Internal Error")
            notification_id = notif_row[0]

            priority = "HIGH" if payload.ctcaeGrade >= 3 else "NORMAL"
            cursor.execute(
                "INSERT INTO adverse_events (ae_id, trial_id, site_id, patient_id, clinician_id, event_date, ae_term_code, ae_term_name, ctcae_grade, serious, outcome, action_taken, narrative, related_drug_id, reported_by, submitted_at, created_at, updated_at) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, NOW(), NOW())",
                (ae_id, payload.trialId, payload.siteId, payload.patientId, payload.clinicianId, payload.eventDate, payload.aeTermCode, payload.aeTermName, payload.ctcaeGrade, serious, outcome, payload.actionTaken, payload.narrative, payload.relatedDrugId, payload.reportedBy, received_at),
            )
            cursor.execute(
                "INSERT INTO ae_notifications (notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, outcome, priority, acknowledged, sns_published, sns_message_id, created_at, updated_at) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, FALSE, FALSE, NULL, NOW(), NOW())",
                (notification_id, ae_id, payload.trialId, payload.siteId, payload.patientId, payload.aeTermName, payload.ctcaeGrade, serious, outcome, priority),
            )
            conn.commit()

        try:
            sns_published = False
            sns_message_id = None
        except Exception as exc:
            logger.error(json.dumps({"event": "notification_stub_error", "message": str(exc)}))
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
        logger.error(json.dumps({"event": "database_error", "message": str(exc)}))
        raise HTTPException(status_code=503, detail="Database Error") from exc
    except Exception as exc:
        if conn is not None:
            conn.rollback()
        logger.error(json.dumps({"event": "unexpected_error", "message": str(exc)}), exc_info=True)
        raise HTTPException(status_code=500, detail="Internal Error") from exc
    finally:
        if conn is not None:
            release_conn(conn)


def list_notifications(trialId: str | None, siteId: str | None, ctcaeGrade: int | None, serious: bool | None, acknowledged: bool | None, priority: str | None, dateFrom: str | None, dateTo: str | None, page: int, pageSize: int) -> NotificationListResponse:
    logger.info(json.dumps({"event": "service_start", "operation": "list", "resource": "notifications"}))
    conn = None
    try:
        if page < 1 or pageSize < 1 or pageSize > 100:
            logger.error(json.dumps({"event": "validation_failure", "rule": "pagination"}))
            raise HTTPException(status_code=400, detail="Validation Error")
        _validate_notification_query(priority)
        date_from = _parse_datetime(dateFrom, "dateFrom")
        date_to = _parse_datetime(dateTo, "dateTo")
        if ctcaeGrade is not None and (ctcaeGrade < 1 or ctcaeGrade > 5):
            logger.error(json.dumps({"event": "validation_failure", "rule": "ctcaeGrade"}))
            raise HTTPException(status_code=400, detail="Validation Error")

        conditions: list[str] = []
        params: list[Any] = []
        if trialId is not None:
            conditions.append("trial_id = %s")
            params.append(trialId)
        if siteId is not None:
            conditions.append("site_id = %s")
            params.append(siteId)
        if ctcaeGrade is not None:
            conditions.append("ctcae_grade = %s")
            params.append(ctcaeGrade)
        if serious is not None:
            conditions.append("serious = %s")
            params.append(serious)
        if acknowledged is not None:
            conditions.append("acknowledged = %s")
            params.append(acknowledged)
        if priority is not None:
            conditions.append("priority = %s")
            params.append(priority)
        if date_from is not None:
            conditions.append("created_at >= %s")
            params.append(date_from)
        if date_to is not None:
            conditions.append("created_at <= %s")
            params.append(date_to)

        where_clause = " WHERE " + " AND ".join(conditions) if conditions else ""
        conn = _get_connection()
        with conn.cursor(cursor_factory=RealDictCursor) as cursor:
            logger.info(json.dumps({"event": "db_operation", "table": "ae_notifications", "operation": "SELECT"}))
            cursor.execute(f"SELECT COUNT(*) AS total FROM ae_notifications{where_clause}", tuple(params))
            total_row = cursor.fetchone()
            total = int(total_row["total"]) if total_row is not None else 0

            logger.info(json.dumps({"event": "db_operation", "table": "ae_notifications", "operation": "SELECT"}))
            data_params = list(params) + [pageSize, (page - 1) * pageSize]
            cursor.execute(
                f"SELECT notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, outcome, priority, acknowledged, sns_published, created_at FROM ae_notifications{where_clause} ORDER BY created_at DESC LIMIT %s OFFSET %s",
                tuple(data_params),
            )
            rows = cursor.fetchall()

        notifications = [
            NotificationRecord(
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
        return NotificationListResponse(status="success", total=total, page=page, pageSize=pageSize, notifications=notifications)
    except HTTPException:
        raise
    except Psycopg2Error as exc:
        logger.error(json.dumps({"event": "database_error", "message": str(exc)}))
        raise HTTPException(status_code=503, detail="Database Error") from exc
    except Exception as exc:
        logger.error(json.dumps({"event": "unexpected_error", "message": str(exc)}), exc_info=True)
        raise HTTPException(status_code=500, detail="Internal Error") from exc
    finally:
        if conn is not None:
            release_conn(conn)

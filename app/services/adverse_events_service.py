import json
import logging
import re
from datetime import UTC, datetime, timedelta
from typing import Any

from fastapi import HTTPException
from psycopg2 import Error as Psycopg2Error
from pydantic import ValidationError

from app.db.connection import get_conn, release_conn
from app.models.adverse_events_model import AdverseEventRecord, NotificationRecord
from app.schemas.adverse_events_schema import (
    AdverseEventCreateRequest,
    AdverseEventCreateResponse,
    NotificationListResponse,
    NotificationResponseItem,
)

logger = logging.getLogger(__name__)
EMAIL_RE = re.compile(r"^[^@\s]+@[^@\s]+\.[^@\s]+$")


def _utc_now() -> datetime:
    return datetime.now(UTC)


def _parse_iso_datetime(value: str, field_name: str) -> datetime:
    try:
        parsed = datetime.fromisoformat(value.replace("Z", "+00:00"))
    except ValueError as exc:
        logger.error(json.dumps({"event": "validation_failure", "rule": field_name}))
        raise HTTPException(status_code=400, detail="Validation Error") from exc
    if parsed.tzinfo is None:
        logger.error(json.dumps({"event": "validation_failure", "rule": field_name}))
        raise HTTPException(status_code=400, detail="Validation Error")
    return parsed.astimezone(UTC)


def _validate_payload(payload: AdverseEventCreateRequest) -> None:
    if not EMAIL_RE.match(payload.reportedBy):
        logger.error(json.dumps({"event": "validation_failure", "rule": "reportedBy"}))
        raise HTTPException(status_code=422, detail="Validation Error")


def submit_adverse_event_service(payload: AdverseEventCreateRequest) -> AdverseEventCreateResponse:
    logger.info(json.dumps({"event": "service_start", "operation": "submit", "resource": "adverse_event"}))
    _validate_payload(payload)
    event_date = _parse_iso_datetime(payload.eventDate, "eventDate")
    coerced_serious = True if payload.ctcaeGrade >= 3 else payload.serious
    coerced_outcome = "FATAL" if payload.ctcaeGrade == 5 else payload.outcome

    conn = None
    try:
        conn = get_conn()
        conn.rollback()
        conn.autocommit = False
        with conn.cursor() as cursor:
            logger.info(json.dumps({"event": "db_operation", "table": "trials", "operation": "SELECT"}))
            cursor.execute("SELECT id FROM trials WHERE trial_id = %s AND status = 'ACTIVE'", (payload.trialId,))
            if cursor.fetchone() is None:
                raise HTTPException(status_code=404, detail="Resource Not Found")

            logger.info(json.dumps({"event": "db_operation", "table": "trial_enrolments", "operation": "SELECT"}))
            cursor.execute(
                "SELECT id FROM trial_enrolments WHERE trial_id = %s AND patient_id = %s AND status = 'ENROLLED'",
                (payload.trialId, payload.patientId),
            )
            if cursor.fetchone() is None:
                raise HTTPException(status_code=404, detail="Resource Not Found")

            logger.info(json.dumps({"event": "db_operation", "table": "adverse_events", "operation": "SELECT"}))
            cursor.execute(
                "SELECT ae_id FROM adverse_events WHERE trial_id = %s AND patient_id = %s AND ae_term_code = %s AND ctcae_grade = %s AND submitted_at >= NOW() - INTERVAL '60 seconds' ORDER BY submitted_at DESC LIMIT 1",
                (payload.trialId, payload.patientId, payload.aeTermCode, payload.ctcaeGrade),
            )
            duplicate = cursor.fetchone()
            if duplicate is not None:
                raise HTTPException(status_code=409, detail="Validation Error")

            logger.info(json.dumps({"event": "db_operation", "table": "ae_id_seq", "operation": "SELECT"}))
            cursor.execute("SELECT 'AE-' || TO_CHAR(NOW(),'YYYY') || '-' || LPAD(NEXTVAL('ae_id_seq')::text, 6, '0')")
            ae_row = cursor.fetchone()
            if ae_row is None:
                conn.rollback()
                raise HTTPException(status_code=500, detail="Internal Error")
            ae_id = ae_row[0]

            logger.info(json.dumps({"event": "db_operation", "table": "notif_id_seq", "operation": "SELECT"}))
            cursor.execute("SELECT 'NOTIF-' || TO_CHAR(NOW(),'YYYY') || '-' || LPAD(NEXTVAL('notif_id_seq')::text, 6, '0')")
            notif_row = cursor.fetchone()
            if notif_row is None:
                conn.rollback()
                raise HTTPException(status_code=500, detail="Internal Error")
            notification_id = notif_row[0]

            submitted_at = _utc_now()
            adverse_event = AdverseEventRecord(
                ae_id=ae_id,
                trial_id=payload.trialId,
                site_id=payload.siteId,
                patient_id=payload.patientId,
                clinician_id=payload.clinicianId,
                event_date=event_date,
                ae_term_code=payload.aeTermCode,
                ae_term_name=payload.aeTermName,
                ctcae_grade=payload.ctcaeGrade,
                serious=coerced_serious,
                outcome=coerced_outcome,
                action_taken=payload.actionTaken,
                narrative=payload.narrative,
                related_drug_id=payload.relatedDrugId,
                reported_by=payload.reportedBy,
                submitted_at=submitted_at,
            )
            notification = NotificationRecord(
                notification_id=notification_id,
                ae_id=ae_id,
                trial_id=payload.trialId,
                site_id=payload.siteId,
                patient_id=payload.patientId,
                ae_term_name=payload.aeTermName,
                ctcae_grade=payload.ctcaeGrade,
                serious=coerced_serious,
                outcome=coerced_outcome,
                priority="HIGH" if payload.ctcaeGrade >= 3 else "NORMAL",
                acknowledged=False,
                sns_published=False,
                sns_message_id=None,
                created_at=submitted_at,
            )

            logger.info(json.dumps({"event": "db_operation", "table": "adverse_events", "operation": "INSERT"}))
            cursor.execute(
                "INSERT INTO adverse_events (ae_id, trial_id, site_id, patient_id, clinician_id, event_date, ae_term_code, ae_term_name, ctcae_grade, serious, outcome, action_taken, narrative, related_drug_id, reported_by, submitted_at) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s) RETURNING id",
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
                ),
            )
            if cursor.fetchone() is None:
                conn.rollback()
                raise HTTPException(status_code=500, detail="Internal Error")

            logger.info(json.dumps({"event": "db_operation", "table": "ae_notifications", "operation": "INSERT"}))
            cursor.execute(
                "INSERT INTO ae_notifications (notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, outcome, priority, acknowledged, sns_published, sns_message_id, created_at) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s) RETURNING id",
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
                    notification.sns_published,
                    notification.sns_message_id,
                    notification.created_at,
                ),
            )
            if cursor.fetchone() is None:
                conn.rollback()
                raise HTTPException(status_code=500, detail="Internal Error")

        conn.commit()
        try:
            sns_published = False
            sns_message_id = None
        except Exception as exc:
            logger.error(json.dumps({"event": "notification_stub_error", "message": str(exc)}))
            sns_published = False
            sns_message_id = None
        return AdverseEventCreateResponse(
            status="success",
            aeId=ae_id,
            notificationId=notification_id,
            snsPublished=sns_published,
            snsMessageId=sns_message_id,
            receivedAt=submitted_at,
        )
    except HTTPException:
        if conn is not None:
            conn.rollback()
        raise
    except Psycopg2Error as exc:
        if conn is not None:
            conn.rollback()
        logger.error(json.dumps({"event": "db_error", "message": str(exc)}))
        raise HTTPException(status_code=503, detail="Database Error") from exc
    except ValidationError as exc:
        if conn is not None:
            conn.rollback()
        logger.error(json.dumps({"event": "validation_error", "message": str(exc)}))
        raise HTTPException(status_code=422, detail="Validation Error") from exc
    except Exception as exc:
        if conn is not None:
            conn.rollback()
        logger.error(json.dumps({"event": "unexpected_error", "message": str(exc)}))
        raise HTTPException(status_code=500, detail="Internal Error") from exc
    finally:
        if conn is not None:
            release_conn(conn)


def _parse_bool(value: str, field_name: str) -> bool:
    lowered = value.lower()
    if lowered in {"true", "1", "yes"}:
        return True
    if lowered in {"false", "0", "no"}:
        return False
    logger.error(json.dumps({"event": "validation_failure", "rule": field_name}))
    raise HTTPException(status_code=400, detail="Validation Error")


def get_notifications_service(query_params: dict[str, str]) -> NotificationListResponse:
    logger.info(json.dumps({"event": "service_start", "operation": "retrieve", "resource": "notifications"}))
    conditions: list[str] = []
    params: list[Any] = []

    try:
        trial_id = query_params.get("trialId")
        if trial_id:
            conditions.append("trial_id = %s")
            params.append(trial_id)

        site_id = query_params.get("siteId")
        if site_id:
            conditions.append("site_id = %s")
            params.append(site_id)

        ctcae_grade = query_params.get("ctcaeGrade")
        if ctcae_grade is not None:
            grade_int = int(ctcae_grade)
            if grade_int < 1 or grade_int > 5:
                raise ValueError
            conditions.append("ctcae_grade = %s")
            params.append(grade_int)

        serious = query_params.get("serious")
        if serious is not None:
            conditions.append("serious = %s")
            params.append(_parse_bool(serious, "serious"))

        acknowledged = query_params.get("acknowledged")
        if acknowledged is not None:
            conditions.append("acknowledged = %s")
            params.append(_parse_bool(acknowledged, "acknowledged"))

        priority = query_params.get("priority")
        if priority is not None:
            if priority not in {"HIGH", "NORMAL"}:
                raise ValueError
            conditions.append("priority = %s")
            params.append(priority)

        date_from = query_params.get("dateFrom")
        if date_from is not None:
            conditions.append("created_at >= %s")
            params.append(_parse_iso_datetime(date_from, "dateFrom"))

        date_to = query_params.get("dateTo")
        if date_to is not None:
            conditions.append("created_at <= %s")
            params.append(_parse_iso_datetime(date_to, "dateTo"))

        page = int(query_params.get("page", "1"))
        page_size = int(query_params.get("pageSize", "20"))
        if page < 1 or page_size < 1 or page_size > 100:
            raise ValueError
    except (ValueError, TypeError):
        logger.error(json.dumps({"event": "validation_failure", "rule": "query_params"}))
        raise HTTPException(status_code=400, detail="Validation Error")

    where_clause = ""
    if conditions:
        where_clause = " WHERE " + " AND ".join(conditions)

    conn = None
    try:
        conn = get_conn()
        conn.rollback()
        conn.autocommit = True
        with conn.cursor() as cursor:
            count_sql = "SELECT COUNT(*) FROM ae_notifications" + where_clause
            logger.info(json.dumps({"event": "db_operation", "table": "ae_notifications", "operation": "SELECT"}))
            cursor.execute(count_sql, tuple(params))
            total_row = cursor.fetchone()
            total = int(total_row[0]) if total_row is not None else 0

            data_sql = (
                "SELECT notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, outcome, priority, acknowledged, sns_published, created_at "
                "FROM ae_notifications"
                + where_clause
                + " ORDER BY created_at DESC LIMIT %s OFFSET %s"
            )
            logger.info(json.dumps({"event": "db_operation", "table": "ae_notifications", "operation": "SELECT"}))
            cursor.execute(data_sql, tuple(params) + (page_size, (page - 1) * page_size))
            rows = cursor.fetchall()

        notifications = [
            NotificationResponseItem(
                notificationId=row[0],
                aeId=row[1],
                trialId=row[2],
                siteId=row[3],
                patientId=row[4],
                aeTermName=row[5],
                ctcaeGrade=row[6],
                serious=row[7],
                priority=row[9],
                outcome=row[8],
                acknowledged=row[10],
                snsPublished=row[11],
                createdAt=row[12],
            )
            for row in rows
        ]
        return NotificationListResponse(status="success", total=total, page=page, pageSize=page_size, notifications=notifications)
    except HTTPException:
        raise
    except Psycopg2Error as exc:
        logger.error(json.dumps({"event": "db_error", "message": str(exc)}))
        raise HTTPException(status_code=503, detail="Database Error") from exc
    except Exception as exc:
        logger.error(json.dumps({"event": "unexpected_error", "message": str(exc)}))
        raise HTTPException(status_code=500, detail="Internal Error") from exc
    finally:
        if conn is not None:
            release_conn(conn)

import json
import logging
import secrets
from dataclasses import asdict
from datetime import datetime, timezone
from typing import Any

from fastapi import HTTPException
from psycopg2 import Error as Psycopg2Error

from app.db.connection import get_conn, release_conn
from app.models.adverse_event_model import AdverseEventRecord, NotificationRecord
from app.schemas.adverse_event_schema import (
    AdverseEventCreateRequest,
    AdverseEventCreateResponse,
    NotificationItem,
    NotificationListResponse,
)
from app.services.logging_service import JsonLogAdapter
from app.services.messaging_service import publish_message

logger = logging.getLogger(__name__)

ALLOWED_OUTCOMES = {"ONGOING", "RESOLVED", "FATAL", "UNKNOWN"}
ALLOWED_ACTIONS = {"NONE", "DOSE_REDUCED", "DRUG_WITHDRAWN", "HOSPITALISED"}
ALLOWED_PRIORITIES = {"HIGH", "NORMAL"}


def _utc_now() -> datetime:
    return datetime.now(timezone.utc)


def _log(step: str, outcome: str, request_id: str, payload: dict[str, Any]) -> None:
    JsonLogAdapter.emit(logger, payload | {"step": step, "outcome": outcome, "request_id": request_id})


def _validate_request(payload: AdverseEventCreateRequest) -> None:
    if payload.ctcaeGrade not in {1, 2, 3, 4, 5}:
        raise HTTPException(status_code=400, detail="INVALID_CTCAE_GRADE")
    if payload.outcome not in ALLOWED_OUTCOMES:
        raise HTTPException(status_code=400, detail="INVALID_OUTCOME")
    if payload.actionTaken not in ALLOWED_ACTIONS:
        raise HTTPException(status_code=400, detail="INVALID_ACTION_TAKEN")
    if len(payload.narrative) > 2000:
        raise HTTPException(status_code=400, detail="NARRATIVE_TOO_LONG")


def _coerce_payload(payload: AdverseEventCreateRequest) -> AdverseEventCreateRequest:
    serious = True if payload.ctcaeGrade >= 3 else payload.serious
    outcome = "FATAL" if payload.ctcaeGrade == 5 else payload.outcome
    return payload.model_copy(update={"serious": serious, "outcome": outcome})


def _build_subject(ae_term_name: str, trial_id: str, serious: bool, ctcae_grade: int) -> str:
    if ctcae_grade == 5:
        return f"[FATAL][SAE] Adverse Event: {ae_term_name} — {trial_id}"
    if serious and ctcae_grade >= 3:
        return f"[SAE][HIGH] Adverse Event: {ae_term_name} — {trial_id}"
    if serious and ctcae_grade < 3:
        return f"[SAE] Adverse Event: {ae_term_name} — {trial_id}"
    if not serious and ctcae_grade >= 3:
        return f"[HIGH] Adverse Event: {ae_term_name} — {trial_id}"
    return f"Adverse Event: {ae_term_name} — {trial_id}"


def create_adverse_event(payload: AdverseEventCreateRequest) -> AdverseEventCreateResponse:
    request_id = secrets.token_hex(16)
    start = _utc_now()
    logger.info(json.dumps({"event": "service_start", "operation": "create_adverse_event", "resource": "adverse_events"}))
    _validate_request(payload)
    coerced = _coerce_payload(payload)
    conn = None
    try:
        conn = get_conn()
        conn.rollback()
        conn.autocommit = False
        with conn.cursor() as cursor:
            cursor.execute("SELECT 1 FROM trials WHERE trial_id = %s AND active = TRUE", (coerced.trialId,))
            if cursor.fetchone() is None:
                raise HTTPException(status_code=400, detail="TRIAL_NOT_FOUND")
            cursor.execute(
                "SELECT 1 FROM trial_enrolments WHERE trial_id = %s AND patient_id = %s",
                (coerced.trialId, coerced.patientId),
            )
            if cursor.fetchone() is None:
                raise HTTPException(status_code=400, detail="PATIENT_NOT_FOUND")
            window_seconds = 60
            cursor.execute(
                "SELECT ae_id FROM adverse_events WHERE trial_id = %s AND patient_id = %s AND ae_term_code = %s AND created_at >= NOW() - (%s || ' seconds')::interval ORDER BY created_at DESC LIMIT 1",
                (coerced.trialId, coerced.patientId, coerced.aeTermCode, window_seconds),
            )
            duplicate = cursor.fetchone()
            if duplicate is not None:
                raise HTTPException(status_code=409, detail="DUPLICATE_AE")
            cursor.execute("SELECT nextval('ae_id_seq')")
            ae_seq_row = cursor.fetchone()
            if ae_seq_row is None:
                raise HTTPException(status_code=500, detail="DB_ERROR")
            cursor.execute("SELECT nextval('notif_id_seq')")
            notif_seq_row = cursor.fetchone()
            if notif_seq_row is None:
                raise HTTPException(status_code=500, detail="DB_ERROR")
            year = _utc_now().year
            ae_id = f"AE-{year}-{int(ae_seq_row[0]):06d}"
            notification_id = f"NOTIF-{year}-{int(notif_seq_row[0]):06d}"
            priority = "HIGH" if coerced.ctcaeGrade >= 3 else "NORMAL"
            record = AdverseEventRecord(
                ae_id=ae_id,
                trial_id=coerced.trialId,
                site_id=coerced.siteId,
                patient_id=coerced.patientId,
                clinician_id=coerced.clinicianId,
                event_date=coerced.eventDate,
                ae_term_code=coerced.aeTermCode,
                ae_term_name=coerced.aeTermName,
                ctcae_grade=coerced.ctcaeGrade,
                serious=coerced.serious,
                outcome=coerced.outcome,
                action_taken=coerced.actionTaken,
                narrative=coerced.narrative,
                related_drug_id=coerced.relatedDrugId,
                reported_by=coerced.reportedBy,
                created_at=_utc_now(),
                updated_at=_utc_now(),
            )
            notification = NotificationRecord(
                notification_id=notification_id,
                ae_id=ae_id,
                trial_id=coerced.trialId,
                site_id=coerced.siteId,
                patient_id=coerced.patientId,
                ae_term_name=coerced.aeTermName,
                ctcae_grade=coerced.ctcaeGrade,
                serious=coerced.serious,
                outcome=coerced.outcome,
                priority=priority,
                acknowledged=False,
                acknowledged_by=None,
                acknowledged_at=None,
                notified=False,
                message_id=None,
                created_at=_utc_now(),
                updated_at=_utc_now(),
            )
            logger.info(json.dumps({"event": "db_operation", "table": "adverse_events", "operation": "INSERT"}))
            cursor.execute(
                "INSERT INTO adverse_events (ae_id, trial_id, site_id, patient_id, clinician_id, event_date, ae_term_code, ae_term_name, ctcae_grade, serious, outcome, action_taken, narrative, related_drug_id, reported_by, created_at, updated_at) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s)",
                (
                    record.ae_id,
                    record.trial_id,
                    record.site_id,
                    record.patient_id,
                    record.clinician_id,
                    record.event_date,
                    record.ae_term_code,
                    record.ae_term_name,
                    record.ctcae_grade,
                    record.serious,
                    record.outcome,
                    record.action_taken,
                    record.narrative,
                    record.related_drug_id,
                    record.reported_by,
                    record.created_at,
                    record.updated_at,
                ),
            )
            logger.info(json.dumps({"event": "db_operation", "table": "ae_notifications", "operation": "INSERT"}))
            cursor.execute(
                "INSERT INTO ae_notifications (notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, outcome, priority, acknowledged, acknowledged_by, acknowledged_at, notified, message_id, created_at, updated_at) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s)",
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
                    notification.notified,
                    notification.message_id,
                    notification.created_at,
                    notification.updated_at,
                ),
            )
            logger.info(json.dumps({"event": "db_operation", "table": "ae_audit_log", "operation": "INSERT"}))
            cursor.execute(
                "INSERT INTO ae_audit_log (ae_id, action, performed_by, details) VALUES (%s, %s, %s, %s)",
                (
                    ae_id,
                    "CREATED",
                    coerced.reportedBy,
                    json.dumps(asdict(record), default=str),
                ),
            )
        conn.commit()
    except HTTPException:
        if conn is not None:
            conn.rollback()
        raise
    except Psycopg2Error as exc:
        if conn is not None:
            conn.rollback()
        logger.error("Database error: %s", str(exc), exc_info=True)
        raise HTTPException(status_code=503, detail="Database Error") from exc
    except Exception as exc:
        if conn is not None:
            conn.rollback()
        logger.error("Unexpected error: %s", str(exc), exc_info=True)
        raise HTTPException(status_code=500, detail="Internal Error") from exc
    finally:
        if conn is not None:
            release_conn(conn)
    notified = False
    message_id = None
    try:
        message_id = publish_message(
            ae_id=ae_id,
            notification_id=notification_id,
            trial_id=coerced.trialId,
            site_id=coerced.siteId,
            patient_id=coerced.patientId,
            ae_term_name=coerced.aeTermName,
            ae_term_code=coerced.aeTermCode,
            ctcae_grade=coerced.ctcaeGrade,
            serious=coerced.serious,
            priority=priority,
            outcome=coerced.outcome,
            event_date=coerced.eventDate,
            reported_by=coerced.reportedBy,
            submitted_at=_utc_now(),
        )
        notified = True
    except Exception as exc:
        logger.error("Message dispatch failed for ae_id=%s: %s", ae_id, str(exc), exc_info=True)
        notified = False
    if message_id is not None:
        try:
            conn = get_conn()
            conn.rollback()
            conn.autocommit = False
            with conn.cursor() as cursor:
                logger.info(json.dumps({"event": "db_operation", "table": "ae_notifications", "operation": "UPDATE"}))
                cursor.execute(
                    "UPDATE ae_notifications SET notified = %s, message_id = %s, updated_at = NOW() WHERE notification_id = %s",
                    (True, message_id, notification_id),
                )
            conn.commit()
        except Exception as exc:
            logger.warning("Notification update failed for notification_id=%s: %s", notification_id, str(exc), exc_info=True)
        finally:
            if conn is not None:
                release_conn(conn)
    received_at = _utc_now().isoformat().replace("+00:00", "Z")
    duration_ms = int((_utc_now() - start).total_seconds() * 1000)
    _log(
        "MESSAGING",
        "SUCCESS",
        request_id,
        {
            "ae_id": ae_id,
            "notification_id": notification_id,
            "trial_id": coerced.trialId,
            "patient_id": coerced.patientId,
            "ctcae_grade": coerced.ctcaeGrade,
            "serious": coerced.serious,
            "duration_ms": duration_ms,
        },
    )
    return AdverseEventCreateResponse(
        status="success",
        aeId=ae_id,
        notificationId=notification_id,
        notified=notified,
        messageId=message_id,
        message="Adverse event recorded. Notification stored and dispatched." if notified else "Adverse event recorded. Notification stored. Dispatch failed — logged.",
        receivedAt=received_at,
    )


def get_notifications(
    trialId: str | None,
    siteId: str | None,
    ctcaeGrade: int | None,
    serious: bool | None,
    acknowledged: bool | None,
    priority: str | None,
    dateFrom: str | None,
    dateTo: str | None,
    page: int,
    pageSize: int,
) -> NotificationListResponse:
    if pageSize > 100:
        raise HTTPException(status_code=400, detail="Validation Error")
    conn = None
    try:
        conn = get_conn()
        conn.rollback()
        with conn.cursor() as cursor:
            where_clauses = []
            params: list[Any] = []
            if trialId is not None:
                where_clauses.append("trial_id = %s")
                params.append(trialId)
            if siteId is not None:
                where_clauses.append("site_id = %s")
                params.append(siteId)
            if ctcaeGrade is not None:
                where_clauses.append("ctcae_grade = %s")
                params.append(ctcaeGrade)
            if serious is not None:
                where_clauses.append("serious = %s")
                params.append(serious)
            if acknowledged is not None:
                where_clauses.append("acknowledged = %s")
                params.append(acknowledged)
            if priority is not None:
                where_clauses.append("priority = %s")
                params.append(priority)
            if dateFrom is not None:
                where_clauses.append("created_at >= %s")
                params.append(dateFrom)
            if dateTo is not None:
                where_clauses.append("created_at <= %s")
                params.append(dateTo)
            where_sql = " WHERE " + " AND ".join(where_clauses) if where_clauses else ""
            logger.info(json.dumps({"event": "db_operation", "table": "ae_notifications", "operation": "SELECT"}))
            cursor.execute(f"SELECT COUNT(*) FROM ae_notifications{where_sql}", tuple(params))
            total_row = cursor.fetchone()
            total = int(total_row[0]) if total_row else 0
            offset = (page - 1) * pageSize
            cursor.execute(
                f"SELECT notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, priority, outcome, acknowledged, acknowledged_by, acknowledged_at, notified, created_at FROM ae_notifications{where_sql} ORDER BY created_at DESC LIMIT %s OFFSET %s",
                tuple(params) + (pageSize, offset),
            )
            rows = cursor.fetchall()
            notifications = [
                NotificationItem(
                    notificationId=row[0],
                    aeId=row[1],
                    trialId=row[2],
                    siteId=row[3],
                    patientId=row[4],
                    aeTermName=row[5],
                    ctcaeGrade=row[6],
                    serious=row[7],
                    priority=row[8],
                    outcome=row[9],
                    acknowledged=row[10],
                    acknowledgedBy=row[11],
                    acknowledgedAt=row[12],
                    notified=row[13],
                    createdAt=row[14].isoformat().replace("+00:00", "Z"),
                )
                for row in rows
            ]
            return NotificationListResponse(status="success", total=total, page=page, pageSize=pageSize, notifications=notifications)
    except HTTPException:
        raise
    except Psycopg2Error as exc:
        logger.error("Database error: %s", str(exc), exc_info=True)
        raise HTTPException(status_code=503, detail="Database Error") from exc
    except Exception as exc:
        logger.error("Unexpected error: %s", str(exc), exc_info=True)
        raise HTTPException(status_code=500, detail="Internal Error") from exc
    finally:
        if conn is not None:
            release_conn(conn)

import json
import logging
import secrets
from dataclasses import asdict
from datetime import datetime, timezone
from typing import Any

import psycopg2
from fastapi import HTTPException, Request

from app.db.connection import get_conn, release_conn
from app.models.adverse_event_model import AdverseEventRecord, NotificationRecord
from app.schemas.adverse_event_schema import (
    AdverseEventCreateRequest,
    AdverseEventCreateResponse,
    NotificationListResponse,
    NotificationResponseItem,
)

logger = logging.getLogger(__name__)

ALLOWED_OUTCOMES = {"ONGOING", "RESOLVED", "FATAL", "UNKNOWN"}
ALLOWED_ACTIONS = {"NONE", "DOSE_REDUCED", "DRUG_WITHDRAWN", "HOSPITALISED"}
ALLOWED_PRIORITIES = {"HIGH", "NORMAL"}


def _utc_now() -> datetime:
    return datetime.now(timezone.utc)


def _log_event(level: str, message: str, **fields: Any) -> None:
    payload = {"timestamp": _utc_now().isoformat(), "level": level, "message": message}
    payload.update({k: v for k, v in fields.items() if v is not None})
    logger.log(getattr(logging, level), json.dumps(payload, default=str))


def _validate_required_fields(payload: AdverseEventCreateRequest) -> None:
    required_fields = [
        payload.trialId,
        payload.siteId,
        payload.patientId,
        payload.clinicianId,
        payload.eventDate,
        payload.aeTermCode,
        payload.aeTermName,
        payload.ctcaeGrade,
        payload.serious,
        payload.outcome,
        payload.actionTaken,
        payload.narrative,
        payload.reportedBy,
    ]
    if any(value is None or value == "" for value in required_fields):
        raise HTTPException(status_code=422, detail="Validation Error")


def _apply_coercions(payload: AdverseEventCreateRequest) -> tuple[bool, str, str]:
    serious = True if payload.ctcaeGrade >= 3 else payload.serious
    outcome = "FATAL" if payload.ctcaeGrade == 5 else payload.outcome
    priority = "HIGH" if payload.ctcaeGrade >= 3 else "NORMAL"
    return serious, outcome, priority


def _build_ae_id(year: int, seq: int) -> str:
    return f"AE-{year}-{seq:06d}"


def _build_notification_id(year: int, seq: int) -> str:
    return f"NOTIF-{year}-{seq:06d}"


def submit_adverse_event_service(request: Request, payload: AdverseEventCreateRequest) -> AdverseEventCreateResponse:
    start = _utc_now()
    _log_event("INFO", "submit adverse event", step="VALIDATION", resource="adverse-event", trial_id=payload.trialId, patient_id=payload.patientId, ctcae_grade=payload.ctcaeGrade)
    _validate_required_fields(payload)
    if payload.ctcaeGrade < 1 or payload.ctcaeGrade > 5:
        raise HTTPException(status_code=422, detail="Validation Error")
    if payload.outcome not in ALLOWED_OUTCOMES:
        raise HTTPException(status_code=422, detail="Validation Error")
    if payload.actionTaken not in ALLOWED_ACTIONS:
        raise HTTPException(status_code=422, detail="Validation Error")
    if len(payload.narrative) > 2000:
        raise HTTPException(status_code=422, detail="Validation Error")

    serious, outcome, priority = _apply_coercions(payload)
    conn = None
    try:
        conn = get_conn()
        conn.rollback()
        with conn.cursor() as cursor:
            _log_event("INFO", "select trial", step="DB_WRITE", operation="SELECT", table="trials")
            cursor.execute("SELECT id FROM trials WHERE trial_id = %s AND status = 'ACTIVE'", (payload.trialId,))
            if cursor.fetchone() is None:
                raise HTTPException(status_code=400, detail="Validation Error")

            _log_event("INFO", "select enrolment", step="DB_WRITE", operation="SELECT", table="trial_enrolments")
            cursor.execute(
                "SELECT id FROM trial_enrolments WHERE trial_id = %s AND patient_id = %s AND status = 'ENROLLED'",
                (payload.trialId, payload.patientId),
            )
            if cursor.fetchone() is None:
                raise HTTPException(status_code=400, detail="Validation Error")

            _log_event("INFO", "duplicate check", step="DB_WRITE", operation="SELECT", table="adverse_events")
            cursor.execute(
                "SELECT ae_id FROM adverse_events WHERE trial_id = %s AND patient_id = %s AND ae_term_code = %s AND submitted_at >= NOW() - INTERVAL '60 seconds' ORDER BY submitted_at DESC LIMIT 1",
                (payload.trialId, payload.patientId, payload.aeTermCode),
            )
            duplicate_row = cursor.fetchone()
            if duplicate_row is not None:
                existing_ae_id = duplicate_row[0]
                raise HTTPException(status_code=409, detail={"code": "DUPLICATE_AE", "aeId": existing_ae_id})

            _log_event("INFO", "generate identifiers", step="DB_WRITE")
            cursor.execute("SELECT nextval('ae_id_seq')")
            ae_seq_row = cursor.fetchone()
            if ae_seq_row is None:
                raise HTTPException(status_code=500, detail="Internal Error")
            cursor.execute("SELECT nextval('notif_id_seq')")
            notif_seq_row = cursor.fetchone()
            if notif_seq_row is None:
                raise HTTPException(status_code=500, detail="Internal Error")

            year = payload.eventDate.astimezone(timezone.utc).year
            ae_id = _build_ae_id(year, int(ae_seq_row[0]))
            notification_id = _build_notification_id(year, int(notif_seq_row[0]))
            received_at = _utc_now()

            ae_record = AdverseEventRecord(
                ae_id=ae_id,
                trial_id=payload.trialId,
                site_id=payload.siteId,
                patient_id=payload.patientId,
                clinician_id=payload.clinicianId,
                event_date=payload.eventDate,
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
                priority=priority,
                acknowledged=False,
                acknowledged_by=None,
                acknowledged_at=None,
                created_at=received_at,
                updated_at=received_at,
            )

            _log_event("INFO", "begin transaction", step="DB_WRITE")
            cursor.execute("BEGIN")
            _log_event("INFO", "insert adverse_events", step="DB_WRITE", operation="INSERT", table="adverse_events")
            cursor.execute(
                "INSERT INTO adverse_events (ae_id, trial_id, site_id, patient_id, clinician_id, event_date, ae_term_code, ae_term_name, ctcae_grade, serious, outcome, action_taken, narrative, related_drug_id, reported_by, submitted_at, created_at, updated_at) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s)",
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
                    ae_record.created_at,
                    ae_record.updated_at,
                ),
            )
            _log_event("INFO", "insert ae_notifications", step="DB_WRITE", operation="INSERT", table="ae_notifications")
            cursor.execute(
                "INSERT INTO ae_notifications (notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, outcome, priority, acknowledged, acknowledged_by, acknowledged_at, created_at, updated_at) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s)",
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
                    notification_record.created_at,
                    notification_record.updated_at,
                ),
            )
            _log_event("INFO", "insert ae_audit_log", step="DB_WRITE", operation="INSERT", table="ae_audit_log")
            cursor.execute(
                "INSERT INTO ae_audit_log (ae_id, action, performed_by, sae, notes, performed_at) VALUES (%s, %s, %s, %s, %s, %s)",
                (
                    ae_record.ae_id,
                    "CREATED",
                    payload.reportedBy,
                    serious,
                    "AE created and notification stored",
                    received_at,
                ),
            )
            conn.commit()
            _log_event("INFO", "commit transaction", step="DB_WRITE", ae_id=ae_id, notification_id=notification_id)
            return AdverseEventCreateResponse(
                status="success",
                aeId=ae_id,
                notificationId=notification_id,
                message="Adverse event recorded and notification stored.",
                receivedAt=received_at,
            )
    except HTTPException:
        if conn is not None:
            conn.rollback()
        raise
    except psycopg2.Error as exc:
        if conn is not None:
            conn.rollback()
        _log_event("ERROR", f"database error: {exc}", step="DB_WRITE", outcome="FAILURE")
        raise HTTPException(status_code=500, detail="Internal Error")
    except Exception as exc:
        if conn is not None:
            conn.rollback()
        _log_event("ERROR", f"unexpected error: {exc}", step="DB_WRITE", outcome="FAILURE")
        raise HTTPException(status_code=500, detail="Internal Error")
    finally:
        if conn is not None:
            release_conn(conn)


def get_notifications_service(request: Request) -> NotificationListResponse:
    params = request.query_params
    page = int(params.get("page", 1))
    page_size = int(params.get("pageSize", 20))
    if page_size > 100:
        raise HTTPException(status_code=422, detail="Validation Error")

    filters: list[str] = []
    values: list[Any] = []
    if params.get("trialId"):
        filters.append("trial_id = %s")
        values.append(params.get("trialId"))
    if params.get("siteId"):
        filters.append("site_id = %s")
        values.append(params.get("siteId"))
    if params.get("ctcaeGrade"):
        filters.append("ctcae_grade = %s")
        values.append(int(params.get("ctcaeGrade")))
    if params.get("serious") is not None:
        filters.append("serious = %s")
        values.append(params.get("serious").lower() == "true")
    if params.get("acknowledged") is not None:
        filters.append("acknowledged = %s")
        values.append(params.get("acknowledged").lower() == "true")
    if params.get("priority"):
        filters.append("priority = %s")
        values.append(params.get("priority"))
    if params.get("dateFrom"):
        filters.append("created_at >= %s")
        values.append(params.get("dateFrom"))
    if params.get("dateTo"):
        filters.append("created_at <= %s")
        values.append(params.get("dateTo"))

    where_clause = " WHERE " + " AND ".join(filters) if filters else ""
    conn = None
    try:
        conn = get_conn()
        conn.rollback()
        with conn.cursor() as cursor:
            _log_event("INFO", "count notifications", step="DB_WRITE", operation="SELECT", table="ae_notifications")
            cursor.execute(f"SELECT COUNT(*) FROM ae_notifications{where_clause}", tuple(values))
            total = int(cursor.fetchone()[0])
            offset = (page - 1) * page_size
            _log_event("INFO", "select notifications", step="DB_WRITE", operation="SELECT", table="ae_notifications")
            cursor.execute(
                f"SELECT notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, priority, outcome, acknowledged, acknowledged_by, acknowledged_at, created_at FROM ae_notifications{where_clause} ORDER BY created_at DESC LIMIT %s OFFSET %s",
                tuple(values) + (page_size, offset),
            )
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
                    priority=row[8],
                    outcome=row[9],
                    acknowledged=row[10],
                    acknowledgedBy=row[11],
                    acknowledgedAt=row[12],
                    createdAt=row[13],
                )
                for row in rows
            ]
            return NotificationListResponse(status="success", total=total, page=page, pageSize=page_size, notifications=notifications)
    except psycopg2.Error as exc:
        _log_event("ERROR", f"database error: {exc}", step="DB_WRITE", outcome="FAILURE")
        raise HTTPException(status_code=500, detail="Internal Error")
    except Exception as exc:
        _log_event("ERROR", f"unexpected error: {exc}", step="DB_WRITE", outcome="FAILURE")
        raise HTTPException(status_code=500, detail="Internal Error")
    finally:
        if conn is not None:
            release_conn(conn)

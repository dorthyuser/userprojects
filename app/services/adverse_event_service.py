import json
import logging
import os
import secrets
from dataclasses import asdict
from datetime import UTC, datetime, timedelta
from typing import Any

import psycopg2
from fastapi import HTTPException, Request

from app.db.connection import get_conn, release_conn
from app.models.adverse_event_model import AdverseEventRecord, NotificationRecord
from app.schemas.adverse_event_schema import (
    AdverseEventCreateRequest,
    AdverseEventCreateResponse,
    NotificationItem,
    NotificationListResponse,
)

logger = logging.getLogger(__name__)

ALLOWED_OUTCOMES = {"ONGOING", "RESOLVED", "FATAL", "UNKNOWN"}
ALLOWED_ACTIONS = {"NONE", "DOSE_REDUCED", "DRUG_WITHDRAWN", "HOSPITALISED"}
ALLOWED_PRIORITIES = {"HIGH", "NORMAL"}


def _log(level: str, step: str, outcome: str, request_id: str, message: str, **fields: Any) -> None:
    payload = {
        "timestamp": datetime.now(UTC).isoformat(),
        "level": level,
        "request_id": request_id,
        "step": step,
        "outcome": outcome,
        "message": message,
    }
    payload.update(fields)
    logger.info(json.dumps(payload, default=str))


def _request_id(request: Request) -> str:
    return request.headers.get("x-request-id") or secrets.token_hex(16)


def _utc_now() -> datetime:
    return datetime.now(UTC)


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
        raise HTTPException(status_code=400, detail="MISSING_REQUIRED_FIELD")


def _apply_coercions(payload: AdverseEventCreateRequest) -> dict[str, Any]:
    serious = True if payload.ctcaeGrade >= 3 else payload.serious
    outcome = "FATAL" if payload.ctcaeGrade == 5 else payload.outcome
    priority = "HIGH" if payload.ctcaeGrade >= 3 else "NORMAL"
    return {
        "serious": serious,
        "outcome": outcome,
        "priority": priority,
    }


def _generate_identifier(prefix: str, year: int, sequence_value: int) -> str:
    return f"{prefix}-{year}-{sequence_value:06d}"


def create_adverse_event(request: Request, payload: AdverseEventCreateRequest) -> AdverseEventCreateResponse:
    request_id = _request_id(request)
    started_at = _utc_now()
    _log("INFO", "VALIDATION", "SUCCESS", request_id, "create adverse event", resource="adverse_events")

    _validate_required_fields(payload)

    if not isinstance(payload.ctcaeGrade, int) or payload.ctcaeGrade < 1 or payload.ctcaeGrade > 5:
        _log("WARNING", "VALIDATION", "FAILURE", request_id, "invalid ctcae grade")
        raise HTTPException(status_code=400, detail="INVALID_CTCAE_GRADE")
    if payload.outcome not in ALLOWED_OUTCOMES:
        _log("WARNING", "VALIDATION", "FAILURE", request_id, "invalid outcome")
        raise HTTPException(status_code=400, detail="INVALID_OUTCOME")
    if payload.actionTaken not in ALLOWED_ACTIONS:
        _log("WARNING", "VALIDATION", "FAILURE", request_id, "invalid action taken")
        raise HTTPException(status_code=400, detail="INVALID_ACTION_TAKEN")
    if len(payload.narrative) > 2000:
        _log("WARNING", "VALIDATION", "FAILURE", request_id, "narrative too long")
        raise HTTPException(status_code=400, detail="NARRATIVE_TOO_LONG")

    coerced = _apply_coercions(payload)
    conn = None
    try:
        conn = get_conn()
        conn.rollback()
        with conn.cursor() as cursor:
            _log("INFO", "DB_WRITE", "SUCCESS", request_id, "select trial", trial_id=payload.trialId)
            cursor.execute("SELECT id FROM trials WHERE trial_id = %s AND status = 'ACTIVE'", (payload.trialId,))
            if cursor.fetchone() is None:
                raise HTTPException(status_code=400, detail="TRIAL_NOT_FOUND")

            _log("INFO", "DB_WRITE", "SUCCESS", request_id, "select enrolment", trial_id=payload.trialId)
            cursor.execute(
                "SELECT id FROM trial_enrolments WHERE trial_id = %s AND patient_id = %s AND status = 'ENROLLED'",
                (payload.trialId, payload.patientId),
            )
            if cursor.fetchone() is None:
                raise HTTPException(status_code=400, detail="PATIENT_NOT_FOUND")

            _log("INFO", "DB_WRITE", "SUCCESS", request_id, "duplicate check", trial_id=payload.trialId)
            cursor.execute(
                """
                SELECT ae_id
                FROM adverse_events
                WHERE trial_id = %s
                  AND patient_id = %s
                  AND ae_term_code = %s
                  AND submitted_at >= NOW() - INTERVAL '60 seconds'
                ORDER BY submitted_at DESC
                LIMIT 1
                """,
                (payload.trialId, payload.patientId, payload.aeTermCode),
            )
            duplicate = cursor.fetchone()
            if duplicate is not None:
                ae_id = duplicate[0]
                raise HTTPException(status_code=409, detail={"code": "DUPLICATE_AE", "aeId": ae_id})

            year = started_at.year
            cursor.execute("SELECT nextval('ae_id_seq')")
            ae_seq_row = cursor.fetchone()
            if ae_seq_row is None:
                raise HTTPException(status_code=500, detail="DB_ERROR")
            ae_id = _generate_identifier("AE", year, int(ae_seq_row[0]))

            cursor.execute("SELECT nextval('notif_id_seq')")
            notif_seq_row = cursor.fetchone()
            if notif_seq_row is None:
                raise HTTPException(status_code=500, detail="DB_ERROR")
            notification_id = _generate_identifier("NOTIF", year, int(notif_seq_row[0]))

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
                serious=coerced["serious"],
                outcome=coerced["outcome"],
                action_taken=payload.actionTaken,
                narrative=payload.narrative,
                related_drug_id=payload.relatedDrugId,
                reported_by=payload.reportedBy,
                submitted_at=started_at,
                created_at=started_at,
                updated_at=started_at,
            )
            notif_record = NotificationRecord(
                notification_id=notification_id,
                ae_id=ae_id,
                trial_id=payload.trialId,
                site_id=payload.siteId,
                patient_id=payload.patientId,
                ae_term_name=payload.aeTermName,
                ctcae_grade=payload.ctcaeGrade,
                serious=coerced["serious"],
                outcome=coerced["outcome"],
                priority=coerced["priority"],
                acknowledged=False,
                acknowledged_by=None,
                acknowledged_at=None,
                created_at=started_at,
                updated_at=started_at,
            )

            _log("INFO", "DB_WRITE", "SUCCESS", request_id, "insert adverse_events", ae_id=ae_id)
            cursor.execute(
                """
                INSERT INTO adverse_events (
                    ae_id, trial_id, site_id, patient_id, clinician_id, event_date,
                    ae_term_code, ae_term_name, ctcae_grade, serious, outcome,
                    action_taken, narrative, related_drug_id, reported_by,
                    submitted_at, created_at, updated_at
                ) VALUES (
                    %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s
                )
                RETURNING id
                """,
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
            if cursor.fetchone() is None:
                raise HTTPException(status_code=500, detail="DB_ERROR")

            _log("INFO", "DB_WRITE", "SUCCESS", request_id, "insert ae_notifications", notification_id=notification_id)
            cursor.execute(
                """
                INSERT INTO ae_notifications (
                    notification_id, ae_id, trial_id, site_id, patient_id,
                    ae_term_name, ctcae_grade, serious, outcome, priority,
                    acknowledged, acknowledged_by, acknowledged_at, created_at, updated_at
                ) VALUES (
                    %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s
                )
                RETURNING id
                """,
                (
                    notif_record.notification_id,
                    notif_record.ae_id,
                    notif_record.trial_id,
                    notif_record.site_id,
                    notif_record.patient_id,
                    notif_record.ae_term_name,
                    notif_record.ctcae_grade,
                    notif_record.serious,
                    notif_record.outcome,
                    notif_record.priority,
                    notif_record.acknowledged,
                    notif_record.acknowledged_by,
                    notif_record.acknowledged_at,
                    notif_record.created_at,
                    notif_record.updated_at,
                ),
            )
            if cursor.fetchone() is None:
                raise HTTPException(status_code=500, detail="DB_ERROR")

            _log("INFO", "DB_WRITE", "SUCCESS", request_id, "insert ae_audit_log", ae_id=ae_id)
            cursor.execute(
                """
                INSERT INTO ae_audit_log (
                    ae_id, action, performed_by, sae, notes, performed_at
                ) VALUES (%s, %s, %s, %s, %s, %s)
                RETURNING id
                """,
                (
                    ae_id,
                    "CREATED",
                    payload.reportedBy,
                    coerced["serious"],
                    "AE created and notification stored",
                    started_at,
                ),
            )
            if cursor.fetchone() is None:
                raise HTTPException(status_code=500, detail="DB_ERROR")

        conn.commit()
        return AdverseEventCreateResponse(
            status="success",
            aeId=ae_id,
            notificationId=notification_id,
            message="Adverse event recorded and notification stored.",
            receivedAt=started_at,
        )
    except HTTPException:
        if conn is not None:
            conn.rollback()
        raise
    except psycopg2.Error as exc:
        if conn is not None:
            conn.rollback()
        logger.error("Database error: %s", str(exc), exc_info=True)
        raise HTTPException(status_code=503, detail="Database Error")
    except Exception as exc:
        if conn is not None:
            conn.rollback()
        logger.error("Unexpected error: %s", str(exc), exc_info=True)
        raise HTTPException(status_code=500, detail="Internal Error")
    finally:
        if conn is not None:
            release_conn(conn)


def get_notifications(request: Request) -> NotificationListResponse:
    request_id = _request_id(request)
    params = request.query_params
    page = int(params.get("page", 1))
    page_size = int(params.get("pageSize", 20))
    if page_size > 100:
        raise HTTPException(status_code=400, detail="Validation Error")

    filters: list[str] = []
    values: list[Any] = []

    trial_id = params.get("trialId")
    site_id = params.get("siteId")
    ctcae_grade = params.get("ctcaeGrade")
    serious = params.get("serious")
    acknowledged = params.get("acknowledged")
    priority = params.get("priority")
    date_from = params.get("dateFrom")
    date_to = params.get("dateTo")

    if trial_id:
        filters.append("trial_id = %s")
        values.append(trial_id)
    if site_id:
        filters.append("site_id = %s")
        values.append(site_id)
    if ctcae_grade:
        filters.append("ctcae_grade = %s")
        values.append(int(ctcae_grade))
    if serious is not None:
        filters.append("serious = %s")
        values.append(str(serious).lower() == "true")
    if acknowledged is not None:
        filters.append("acknowledged = %s")
        values.append(str(acknowledged).lower() == "true")
    if priority:
        if priority not in ALLOWED_PRIORITIES:
            raise HTTPException(status_code=422, detail="Validation Error")
        filters.append("priority = %s")
        values.append(priority)
    if date_from:
        filters.append("created_at >= %s")
        values.append(date_from)
    if date_to:
        filters.append("created_at <= %s")
        values.append(date_to)

    where_clause = " WHERE " + " AND ".join(filters) if filters else ""
    offset = (page - 1) * page_size
    conn = None
    try:
        conn = get_conn()
        conn.rollback()
        with conn.cursor() as cursor:
            _log("INFO", "DB_WRITE", "SUCCESS", request_id, "count ae_notifications", resource="ae_notifications")
            cursor.execute(f"SELECT COUNT(*) FROM ae_notifications{where_clause}", tuple(values))
            total_row = cursor.fetchone()
            total = int(total_row[0]) if total_row is not None else 0

            _log("INFO", "DB_WRITE", "SUCCESS", request_id, "select ae_notifications", resource="ae_notifications")
            cursor.execute(
                f"""
                SELECT notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name,
                       ctcae_grade, serious, priority, outcome, acknowledged,
                       acknowledged_by, acknowledged_at, created_at
                FROM ae_notifications
                {where_clause}
                ORDER BY created_at DESC
                LIMIT %s OFFSET %s
                """,
                tuple(values) + (page_size, offset),
            )
            rows = cursor.fetchall() or []
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
                    createdAt=row[13],
                )
                for row in rows
            ]
            return NotificationListResponse(
                status="success",
                total=total,
                page=page,
                pageSize=page_size,
                notifications=notifications,
            )
    except psycopg2.Error as exc:
        if conn is not None:
            conn.rollback()
        logger.error("Database error: %s", str(exc), exc_info=True)
        raise HTTPException(status_code=503, detail="Database Error")
    except Exception as exc:
        if conn is not None:
            conn.rollback()
        logger.error("Unexpected error: %s", str(exc), exc_info=True)
        raise HTTPException(status_code=500, detail="Internal Error")
    finally:
        if conn is not None:
            release_conn(conn)

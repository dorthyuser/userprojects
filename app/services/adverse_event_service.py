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

logger = logging.getLogger(__name__)

ALLOWED_OUTCOMES = {"ONGOING", "RESOLVED", "FATAL", "UNKNOWN"}
ALLOWED_ACTIONS = {"NONE", "DOSE_REDUCED", "DRUG_WITHDRAWN", "HOSPITALISED"}
ALLOWED_PRIORITIES = {"HIGH", "NORMAL"}


def _utc_now() -> datetime:
    return datetime.now(timezone.utc)


def _log(level: str, message: str, **fields: Any) -> None:
    payload = {"timestamp": _utc_now().isoformat(), "level": level, "message": message}
    payload.update(fields)
    logger.info(json.dumps(payload))


def _raise_validation(detail: str) -> None:
    _log("WARNING", "validation_failure", step="VALIDATION", outcome="FAILURE", error=detail)
    raise HTTPException(status_code=422, detail="Validation Error")


def _coerce_payload(payload: AdverseEventCreateRequest) -> tuple[dict[str, Any], str]:
    required_fields = [
        "trialId",
        "siteId",
        "patientId",
        "clinicianId",
        "eventDate",
        "aeTermCode",
        "aeTermName",
        "ctcaeGrade",
        "serious",
        "outcome",
        "actionTaken",
        "narrative",
        "reportedBy",
    ]
    for field_name in required_fields:
        value = getattr(payload, field_name)
        if value is None or (isinstance(value, str) and not value.strip()):
            _raise_validation(f"missing_required_field:{field_name}")

    if not isinstance(payload.ctcaeGrade, int) or payload.ctcaeGrade < 1 or payload.ctcaeGrade > 5:
        _raise_validation("invalid_ctcae_grade")
    if payload.outcome not in ALLOWED_OUTCOMES:
        _raise_validation("invalid_outcome")
    if payload.actionTaken not in ALLOWED_ACTIONS:
        _raise_validation("invalid_action_taken")
    if len(payload.narrative) > 2000:
        _raise_validation("narrative_too_long")

    serious = True if payload.ctcaeGrade >= 3 else bool(payload.serious)
    outcome = "FATAL" if payload.ctcaeGrade == 5 else payload.outcome
    priority = "HIGH" if payload.ctcaeGrade >= 3 else "NORMAL"

    coerced = {
        "trial_id": payload.trialId,
        "site_id": payload.siteId,
        "patient_id": payload.patientId,
        "clinician_id": payload.clinicianId,
        "event_date": payload.eventDate,
        "ae_term_code": payload.aeTermCode,
        "ae_term_name": payload.aeTermName,
        "ctcae_grade": payload.ctcaeGrade,
        "serious": serious,
        "outcome": outcome,
        "action_taken": payload.actionTaken,
        "narrative": payload.narrative,
        "related_drug_id": payload.relatedDrugId,
        "reported_by": payload.reportedBy,
        "priority": priority,
    }
    return coerced, priority


def _generate_identifier(prefix: str, seq_value: int) -> str:
    year = _utc_now().year
    return f"{prefix}-{year}-{seq_value:06d}"


def create_adverse_event(payload: AdverseEventCreateRequest, request_id: str) -> AdverseEventCreateResponse:
    _log("INFO", "service_start", step="VALIDATION", outcome="SUCCESS", request_id=request_id)
    coerced, priority = _coerce_payload(payload)
    conn = None
    try:
        conn = get_conn()
        conn.rollback()
        with conn.cursor() as cursor:
            _log("INFO", "db_select", step="DB_WRITE", outcome="SUCCESS", table="trials", operation="SELECT")
            cursor.execute(
                "SELECT id FROM trials WHERE trial_id = %s AND status = 'ACTIVE'",
                (coerced["trial_id"],),
            )
            if cursor.fetchone() is None:
                raise HTTPException(status_code=400, detail="Validation Error")

            _log("INFO", "db_select", step="DB_WRITE", outcome="SUCCESS", table="trial_enrolments", operation="SELECT")
            cursor.execute(
                "SELECT id FROM trial_enrolments WHERE trial_id = %s AND patient_id = %s AND status = 'ENROLLED'",
                (coerced["trial_id"], coerced["patient_id"]),
            )
            if cursor.fetchone() is None:
                raise HTTPException(status_code=400, detail="Validation Error")

            _log("INFO", "db_select", step="DB_WRITE", outcome="SUCCESS", table="adverse_events", operation="SELECT")
            cursor.execute(
                "SELECT ae_id FROM adverse_events WHERE trial_id = %s AND patient_id = %s AND ae_term_code = %s AND submitted_at >= NOW() - INTERVAL '60 seconds' ORDER BY submitted_at DESC LIMIT 1",
                (coerced["trial_id"], coerced["patient_id"], coerced["ae_term_code"]),
            )
            existing = cursor.fetchone()
            if existing is not None:
                ae_id = existing[0]
                raise HTTPException(status_code=409, detail=f"DUPLICATE_AE:{ae_id}")

            _log("INFO", "db_select", step="DB_WRITE", outcome="SUCCESS", table="ae_id_seq", operation="SELECT")
            cursor.execute("SELECT nextval('ae_id_seq')")
            ae_seq_row = cursor.fetchone()
            if ae_seq_row is None:
                raise RuntimeError("Failed to generate ae sequence")
            ae_id = _generate_identifier("AE", int(ae_seq_row[0]))

            _log("INFO", "db_select", step="DB_WRITE", outcome="SUCCESS", table="notif_id_seq", operation="SELECT")
            cursor.execute("SELECT nextval('notif_id_seq')")
            notif_seq_row = cursor.fetchone()
            if notif_seq_row is None:
                raise RuntimeError("Failed to generate notification sequence")
            notification_id = _generate_identifier("NOTIF", int(notif_seq_row[0]))

            _log("INFO", "db_insert", step="DB_WRITE", outcome="SUCCESS", table="adverse_events", operation="INSERT")
            cursor.execute(
                "INSERT INTO adverse_events (ae_id, trial_id, site_id, patient_id, clinician_id, event_date, ae_term_code, ae_term_name, ctcae_grade, serious, outcome, action_taken, narrative, related_drug_id, reported_by, submitted_at) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, NOW()) RETURNING id",
                (
                    ae_id,
                    coerced["trial_id"],
                    coerced["site_id"],
                    coerced["patient_id"],
                    coerced["clinician_id"],
                    coerced["event_date"],
                    coerced["ae_term_code"],
                    coerced["ae_term_name"],
                    coerced["ctcae_grade"],
                    coerced["serious"],
                    coerced["outcome"],
                    coerced["action_taken"],
                    coerced["narrative"],
                    coerced["related_drug_id"],
                    coerced["reported_by"],
                ),
            )
            if cursor.fetchone() is None:
                raise RuntimeError("Insert failed for adverse_events")

            _log("INFO", "db_insert", step="DB_WRITE", outcome="SUCCESS", table="ae_notifications", operation="INSERT")
            cursor.execute(
                "INSERT INTO ae_notifications (notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, outcome, priority, acknowledged, created_at) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, FALSE, NOW()) RETURNING id",
                (
                    notification_id,
                    ae_id,
                    coerced["trial_id"],
                    coerced["site_id"],
                    coerced["patient_id"],
                    coerced["ae_term_name"],
                    coerced["ctcae_grade"],
                    coerced["serious"],
                    coerced["outcome"],
                    priority,
                ),
            )
            if cursor.fetchone() is None:
                raise RuntimeError("Insert failed for ae_notifications")

            _log("INFO", "db_insert", step="DB_WRITE", outcome="SUCCESS", table="ae_audit_log", operation="INSERT")
            cursor.execute(
                "INSERT INTO ae_audit_log (ae_id, action, performed_by, sae, notes, performed_at) VALUES (%s, %s, %s, %s, %s, NOW()) RETURNING id",
                (
                    ae_id,
                    "CREATED",
                    coerced["reported_by"],
                    coerced["serious"],
                    "Adverse event created and notification stored.",
                ),
            )
            if cursor.fetchone() is None:
                raise RuntimeError("Insert failed for ae_audit_log")

            conn.commit()
            received_at = _utc_now().isoformat().replace("+00:00", "Z")
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
    except Psycopg2Error as exc:
        if conn is not None:
            conn.rollback()
        _log("ERROR", "database_error", step="DB_WRITE", outcome="FAILURE", error=str(exc))
        raise HTTPException(status_code=503, detail="Database Error")
    except Exception as exc:
        if conn is not None:
            conn.rollback()
        _log("ERROR", "unexpected_error", step="DB_WRITE", outcome="FAILURE", error=str(exc))
        raise HTTPException(status_code=500, detail="Internal Error")
    finally:
        if conn is not None:
            release_conn(conn)


def get_notifications(
    trial_id: str | None,
    site_id: str | None,
    ctcae_grade: int | None,
    serious: bool | None,
    acknowledged: bool | None,
    priority: str | None,
    date_from: str | None,
    date_to: str | None,
    page: int,
    page_size: int,
    request_id: str,
) -> NotificationListResponse:
    _log("INFO", "service_start", step="VALIDATION", outcome="SUCCESS", request_id=request_id)
    if page_size > 100:
        _raise_validation("page_size_exceeded")
    filters: list[str] = []
    params: list[Any] = []
    if trial_id is not None:
        filters.append("trial_id = %s")
        params.append(trial_id)
    if site_id is not None:
        filters.append("site_id = %s")
        params.append(site_id)
    if ctcae_grade is not None:
        filters.append("ctcae_grade = %s")
        params.append(ctcae_grade)
    if serious is not None:
        filters.append("serious = %s")
        params.append(serious)
    if acknowledged is not None:
        filters.append("acknowledged = %s")
        params.append(acknowledged)
    if priority is not None:
        if priority not in ALLOWED_PRIORITIES:
            _raise_validation("invalid_priority")
        filters.append("priority = %s")
        params.append(priority)
    if date_from is not None:
        filters.append("created_at >= %s")
        params.append(date_from)
    if date_to is not None:
        filters.append("created_at <= %s")
        params.append(date_to)

    where_clause = " WHERE " + " AND ".join(filters) if filters else ""
    offset = (page - 1) * page_size
    conn = None
    try:
        conn = get_conn()
        conn.rollback()
        with conn.cursor() as cursor:
            _log("INFO", "db_select", step="DB_WRITE", outcome="SUCCESS", table="ae_notifications", operation="SELECT")
            cursor.execute(f"SELECT COUNT(*) FROM ae_notifications{where_clause}", tuple(params))
            total_row = cursor.fetchone()
            total = int(total_row[0]) if total_row is not None else 0

            _log("INFO", "db_select", step="DB_WRITE", outcome="SUCCESS", table="ae_notifications", operation="SELECT")
            cursor.execute(
                f"SELECT notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, priority, outcome, acknowledged, acknowledged_by, acknowledged_at, created_at FROM ae_notifications{where_clause} ORDER BY created_at DESC LIMIT %s OFFSET %s",
                tuple(params) + (int(page_size), int(offset)),
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
                    acknowledgedAt=row[12].isoformat().replace("+00:00", "Z") if row[12] is not None else None,
                    createdAt=row[13].isoformat().replace("+00:00", "Z") if row[13] is not None else None,
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
    except Psycopg2Error as exc:
        _log("ERROR", "database_error", step="DB_WRITE", outcome="FAILURE", error=str(exc))
        raise HTTPException(status_code=503, detail="Database Error")
    except Exception as exc:
        _log("ERROR", "unexpected_error", step="DB_WRITE", outcome="FAILURE", error=str(exc))
        raise HTTPException(status_code=500, detail="Internal Error")
    finally:
        if conn is not None:
            release_conn(conn)

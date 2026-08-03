import json
import logging
import secrets
from datetime import datetime, timezone
from typing import Any

import psycopg2
from fastapi import HTTPException, Request, status

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


def _now_utc() -> datetime:
    return datetime.now(timezone.utc)


def _log(step: str, outcome: str, request_id: str, trial_id: str | None, patient_id: str | None, ctcae_grade: int | None, serious: bool | None, ae_id: str | None = None, notification_id: str | None = None, error: str | None = None, duration_ms: int | None = None, level: str = "INFO") -> None:
    payload = {
        "timestamp": _now_utc().isoformat(),
        "level": level,
        "request_id": request_id,
        "ae_id": ae_id,
        "notification_id": notification_id,
        "trial_id": trial_id,
        "patient_id": patient_id,
        "ctcae_grade": ctcae_grade,
        "serious": serious,
        "step": step,
        "outcome": outcome,
        "error": error,
        "duration_ms": duration_ms,
    }
    logger.info(json.dumps(payload, default=str))


def _validate_create_payload(payload: AdverseEventCreateRequest) -> tuple[bool, str]:
    if payload.ctcaeGrade < 1 or payload.ctcaeGrade > 5:
        return False, "INVALID_CTCAE_GRADE"
    if payload.outcome not in ALLOWED_OUTCOMES:
        return False, "INVALID_OUTCOME"
    if payload.actionTaken not in ALLOWED_ACTIONS:
        return False, "INVALID_ACTION_TAKEN"
    if len(payload.narrative) > 2000:
        return False, "NARRATIVE_TOO_LONG"
    return True, ""


def _coerce_values(payload: AdverseEventCreateRequest) -> tuple[bool, str, str]:
    serious = payload.serious or payload.ctcaeGrade >= 3
    outcome = "FATAL" if payload.ctcaeGrade == 5 else payload.outcome
    priority = "HIGH" if payload.ctcaeGrade >= 3 else "NORMAL"
    return serious, outcome, priority


def _generate_identifier(prefix: str, year: int, seq: int) -> str:
    return f"{prefix}-{year}-{seq:06d}"


def create_adverse_event(request: Request, payload: AdverseEventCreateRequest) -> AdverseEventCreateResponse:
    request_id = request.headers.get("x-request-id") or secrets.token_hex(8)
    start = datetime.now(timezone.utc)
    trial_id = payload.trialId
    patient_id = payload.patientId
    ctcae_grade = payload.ctcaeGrade
    serious, outcome, priority = _coerce_values(payload)

    _log("VALIDATION", "SUCCESS", request_id, trial_id, patient_id, ctcae_grade, serious)

    valid, error_code = _validate_create_payload(payload)
    if not valid:
        _log("VALIDATION", "FAILURE", request_id, trial_id, patient_id, ctcae_grade, serious, error=error_code, level="WARNING")
        raise HTTPException(status_code=status.HTTP_422_UNPROCESSABLE_ENTITY, detail="Validation Error")

    conn = None
    try:
        conn = get_conn()
        conn.rollback()
        with conn.cursor() as cursor:
            logger.info(json.dumps({"timestamp": _now_utc().isoformat(), "level": "INFO", "request_id": request_id, "step": "DB_WRITE", "outcome": "SUCCESS", "table": "trials", "operation": "SELECT"}))
            cursor.execute("SELECT id FROM trials WHERE trial_id = %s AND status = 'ACTIVE'", (payload.trialId,))
            if cursor.fetchone() is None:
                raise HTTPException(status_code=status.HTTP_400_BAD_REQUEST, detail="Validation Error")

            logger.info(json.dumps({"timestamp": _now_utc().isoformat(), "level": "INFO", "request_id": request_id, "step": "DB_WRITE", "outcome": "SUCCESS", "table": "trial_enrolments", "operation": "SELECT"}))
            cursor.execute("SELECT id FROM trial_enrolments WHERE trial_id = %s AND patient_id = %s AND status = 'ENROLLED'", (payload.trialId, payload.patientId))
            if cursor.fetchone() is None:
                raise HTTPException(status_code=status.HTTP_400_BAD_REQUEST, detail="Validation Error")

            logger.info(json.dumps({"timestamp": _now_utc().isoformat(), "level": "INFO", "request_id": request_id, "step": "DB_WRITE", "outcome": "SUCCESS", "table": "adverse_events", "operation": "SELECT"}))
            cursor.execute(
                "SELECT ae_id FROM adverse_events WHERE trial_id = %s AND patient_id = %s AND ae_term_code = %s AND submitted_at >= NOW() - INTERVAL '60 seconds' ORDER BY submitted_at DESC LIMIT 1",
                (payload.trialId, payload.patientId, payload.aeTermCode),
            )
            duplicate = cursor.fetchone()
            if duplicate is not None:
                ae_id = duplicate[0]
                _log("DB_WRITE", "FAILURE", request_id, trial_id, patient_id, ctcae_grade, serious, ae_id=ae_id, error="DUPLICATE_AE", level="WARNING")
                raise HTTPException(status_code=status.HTTP_409_CONFLICT, detail="Validation Error")

            year = _now_utc().year
            logger.info(json.dumps({"timestamp": _now_utc().isoformat(), "level": "INFO", "request_id": request_id, "step": "DB_WRITE", "outcome": "SUCCESS", "table": "ae_id_seq", "operation": "SELECT"}))
            cursor.execute("SELECT nextval('ae_id_seq')")
            ae_seq_row = cursor.fetchone()
            if ae_seq_row is None:
                raise RuntimeError("Sequence fetch failed")
            ae_id = _generate_identifier("AE", year, int(ae_seq_row[0]))

            logger.info(json.dumps({"timestamp": _now_utc().isoformat(), "level": "INFO", "request_id": request_id, "step": "DB_WRITE", "outcome": "SUCCESS", "table": "notif_id_seq", "operation": "SELECT"}))
            cursor.execute("SELECT nextval('notif_id_seq')")
            notif_seq_row = cursor.fetchone()
            if notif_seq_row is None:
                raise RuntimeError("Sequence fetch failed")
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
                serious=serious,
                outcome=outcome,
                action_taken=payload.actionTaken,
                narrative=payload.narrative,
                related_drug_id=payload.relatedDrugId,
                reported_by=payload.reportedBy,
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
            )

            logger.info(json.dumps({"timestamp": _now_utc().isoformat(), "level": "INFO", "request_id": request_id, "step": "DB_WRITE", "outcome": "SUCCESS", "table": "adverse_events", "operation": "INSERT"}))
            cursor.execute(
                "INSERT INTO adverse_events (ae_id, trial_id, site_id, patient_id, clinician_id, event_date, ae_term_code, ae_term_name, ctcae_grade, serious, outcome, action_taken, narrative, related_drug_id, reported_by, submitted_at) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, NOW()) RETURNING id",
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
                ),
            )
            if cursor.fetchone() is None:
                raise RuntimeError("Insert failed")

            logger.info(json.dumps({"timestamp": _now_utc().isoformat(), "level": "INFO", "request_id": request_id, "step": "DB_WRITE", "outcome": "SUCCESS", "table": "ae_notifications", "operation": "INSERT"}))
            cursor.execute(
                "INSERT INTO ae_notifications (notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, outcome, priority, acknowledged, created_at) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, FALSE, NOW()) RETURNING id",
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
                ),
            )
            if cursor.fetchone() is None:
                raise RuntimeError("Insert failed")

            logger.info(json.dumps({"timestamp": _now_utc().isoformat(), "level": "INFO", "request_id": request_id, "step": "DB_WRITE", "outcome": "SUCCESS", "table": "ae_audit_log", "operation": "INSERT"}))
            cursor.execute(
                "INSERT INTO ae_audit_log (ae_id, action, performed_by, sae, notes, performed_at) VALUES (%s, %s, %s, %s, %s, NOW()) RETURNING id",
                (
                    ae_record.ae_id,
                    "CREATED",
                    ae_record.reported_by,
                    ae_record.serious,
                    "Adverse event created and notification stored.",
                ),
            )
            if cursor.fetchone() is None:
                raise RuntimeError("Insert failed")

        conn.commit()
        received_at = _now_utc().isoformat().replace("+00:00", "Z")
        duration_ms = int((datetime.now(timezone.utc) - start).total_seconds() * 1000)
        _log("DB_WRITE", "SUCCESS", request_id, trial_id, patient_id, ctcae_grade, serious, ae_id=ae_id, notification_id=notification_id, duration_ms=duration_ms)
        return AdverseEventCreateResponse(status="success", aeId=ae_id, notificationId=notification_id, message="Adverse event recorded and notification stored.", receivedAt=received_at)
    except HTTPException:
        if conn is not None:
            conn.rollback()
        raise
    except psycopg2.Error as exc:
        if conn is not None:
            conn.rollback()
        logger.error(json.dumps({"error": str(exc), "request_id": request_id}), exc_info=True)
        raise HTTPException(status_code=status.HTTP_503_SERVICE_UNAVAILABLE, detail="Database Error")
    except Exception as exc:
        if conn is not None:
            conn.rollback()
        logger.error(json.dumps({"error": str(exc), "request_id": request_id}), exc_info=True)
        raise HTTPException(status_code=status.HTTP_500_INTERNAL_SERVER_ERROR, detail="Internal Error")
    finally:
        if conn is not None:
            release_conn(conn)


def list_notifications(request: Request) -> NotificationListResponse:
    request_id = request.headers.get("x-request-id") or secrets.token_hex(8)
    params = dict(request.query_params)
    page = int(params.get("page", 1))
    page_size = int(params.get("pageSize", 20))
    if page_size > 100:
        raise HTTPException(status_code=status.HTTP_422_UNPROCESSABLE_ENTITY, detail="Validation Error")

    filters: list[str] = []
    values: list[Any] = []

    if params.get("trialId"):
        filters.append("trial_id = %s")
        values.append(params["trialId"])
    if params.get("siteId"):
        filters.append("site_id = %s")
        values.append(params["siteId"])
    if params.get("ctcaeGrade"):
        filters.append("ctcae_grade = %s")
        values.append(int(params["ctcaeGrade"]))
    if params.get("serious") is not None:
        filters.append("serious = %s")
        values.append(params["serious"].lower() == "true")
    if params.get("acknowledged") is not None:
        filters.append("acknowledged = %s")
        values.append(params["acknowledged"].lower() == "true")
    if params.get("priority"):
        filters.append("priority = %s")
        values.append(params["priority"])
    if params.get("dateFrom"):
        filters.append("created_at >= %s")
        values.append(params["dateFrom"])
    if params.get("dateTo"):
        filters.append("created_at <= %s")
        values.append(params["dateTo"])

    where_clause = " WHERE " + " AND ".join(filters) if filters else ""
    conn = None
    try:
        conn = get_conn()
        conn.rollback()
        with conn.cursor() as cursor:
            logger.info(json.dumps({"timestamp": _now_utc().isoformat(), "level": "INFO", "request_id": request_id, "step": "DB_WRITE", "outcome": "SUCCESS", "table": "ae_notifications", "operation": "SELECT"}))
            cursor.execute(f"SELECT COUNT(*) FROM ae_notifications{where_clause}", tuple(values))
            total_row = cursor.fetchone()
            total = int(total_row[0]) if total_row is not None else 0

            offset = (page - 1) * page_size
            cursor.execute(
                f"SELECT notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, priority, outcome, acknowledged, acknowledged_by, acknowledged_at, created_at FROM ae_notifications{where_clause} ORDER BY created_at DESC LIMIT %s OFFSET %s",
                tuple(values) + (page_size, offset),
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
                    acknowledgedAt=row[12].isoformat().replace("+00:00", "Z") if row[12] else None,
                    createdAt=row[13].isoformat().replace("+00:00", "Z") if row[13] else None,
                )
                for row in rows
            ]
        return NotificationListResponse(status="success", total=total, page=page, pageSize=page_size, notifications=notifications)
    except psycopg2.Error as exc:
        logger.error(json.dumps({"error": str(exc), "request_id": request_id}), exc_info=True)
        raise HTTPException(status_code=status.HTTP_503_SERVICE_UNAVAILABLE, detail="Database Error")
    except Exception as exc:
        logger.error(json.dumps({"error": str(exc), "request_id": request_id}), exc_info=True)
        raise HTTPException(status_code=status.HTTP_500_INTERNAL_SERVER_ERROR, detail="Internal Error")
    finally:
        if conn is not None:
            release_conn(conn)

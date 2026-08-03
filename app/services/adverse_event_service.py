import json
import logging
import secrets
from dataclasses import asdict
from datetime import datetime, timezone
from typing import Any

import psycopg2
from fastapi import HTTPException, Request

from app.db.connection import get_conn, release_conn
from app.models.adverse_event_model import NotificationRecord
from app.schemas.adverse_event_schema import (
    AdverseEventCreateRequest,
    AdverseEventCreateResponse,
    NotificationListResponse,
)

logger = logging.getLogger(__name__)

ALLOWED_OUTCOMES = {"ONGOING", "RESOLVED", "FATAL", "UNKNOWN"}
ALLOWED_ACTIONS = {"NONE", "DOSE_REDUCED", "DRUG_WITHDRAWN", "HOSPITALISED"}
ALLOWED_PRIORITIES = {"HIGH", "NORMAL"}


def _log(level: str, message: str) -> None:
    getattr(logger, level.lower())(json.dumps({"message": message}))


def _now_utc() -> datetime:
    return datetime.now(timezone.utc)


def _request_id(request: Request) -> str:
    return request.headers.get("x-request-id", secrets.token_hex(8))


def _validate_required(payload: AdverseEventCreateRequest) -> None:
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
    data = payload.model_dump()
    missing = [field for field in required_fields if data.get(field) in (None, "")]
    if missing:
        raise HTTPException(status_code=400, detail="Validation Error")


def _coerce_payload(payload: AdverseEventCreateRequest) -> dict[str, Any]:
    data = payload.model_dump()
    grade = data["ctcaeGrade"]
    if not isinstance(grade, int) or grade < 1 or grade > 5:
        raise HTTPException(status_code=400, detail="Validation Error")
    if data["outcome"] not in ALLOWED_OUTCOMES:
        raise HTTPException(status_code=400, detail="Validation Error")
    if data["actionTaken"] not in ALLOWED_ACTIONS:
        raise HTTPException(status_code=400, detail="Validation Error")
    narrative = data["narrative"]
    if len(narrative) > 2000:
        raise HTTPException(status_code=400, detail="Validation Error")
    data["serious"] = True if grade >= 3 else bool(data["serious"])
    data["outcome"] = "FATAL" if grade == 5 else data["outcome"]
    data["priority"] = "HIGH" if grade >= 3 else "NORMAL"
    return data


def _generate_identifier(prefix: str, year: int, seq: int) -> str:
    return f"{prefix}-{year}-{seq:06d}"


def create_adverse_event(request: Request, payload: AdverseEventCreateRequest) -> AdverseEventCreateResponse:
    request_id = _request_id(request)
    start = _now_utc()
    logger.info(json.dumps({"step": "VALIDATION", "request_id": request_id, "message": "create adverse event"}))
    _validate_required(payload)
    data = _coerce_payload(payload)
    conn = None
    try:
        conn = get_conn()
        conn.rollback()
        with conn.cursor() as cursor:
            logger.info(json.dumps({"step": "DB_WRITE", "table": "trials", "operation": "SELECT"}))
            cursor.execute("SELECT id FROM trials WHERE trial_id = %s AND status = 'ACTIVE'", (data["trialId"],))
            if cursor.fetchone() is None:
                raise HTTPException(status_code=400, detail="Validation Error")
            logger.info(json.dumps({"step": "DB_WRITE", "table": "trial_enrolments", "operation": "SELECT"}))
            cursor.execute("SELECT id FROM trial_enrolments WHERE trial_id = %s AND patient_id = %s AND status = 'ENROLLED'", (data["trialId"], data["patientId"]))
            if cursor.fetchone() is None:
                raise HTTPException(status_code=400, detail="Validation Error")
            logger.info(json.dumps({"step": "DB_WRITE", "table": "adverse_events", "operation": "SELECT"}))
            cursor.execute(
                "SELECT ae_id FROM adverse_events WHERE trial_id = %s AND patient_id = %s AND ae_term_code = %s AND submitted_at >= NOW() - INTERVAL '60 seconds' ORDER BY submitted_at DESC LIMIT 1",
                (data["trialId"], data["patientId"], data["aeTermCode"]),
            )
            duplicate = cursor.fetchone()
            if duplicate is not None:
                return AdverseEventCreateResponse(status="success", aeId=duplicate[0], notificationId="", message="Adverse event recorded and notification stored.", receivedAt=start)
            logger.info(json.dumps({"step": "DB_WRITE", "table": "ae_id_seq", "operation": "SELECT"}))
            cursor.execute("SELECT nextval('ae_id_seq')")
            ae_seq_row = cursor.fetchone()
            if ae_seq_row is None:
                raise HTTPException(status_code=500, detail="Internal Error")
            cursor.execute("SELECT nextval('notif_id_seq')")
            notif_seq_row = cursor.fetchone()
            if notif_seq_row is None:
                raise HTTPException(status_code=500, detail="Internal Error")
            year = start.year
            ae_id = _generate_identifier("AE", year, int(ae_seq_row[0]))
            notification_id = _generate_identifier("NOTIF", year, int(notif_seq_row[0]))
            logger.info(json.dumps({"step": "DB_WRITE", "table": "adverse_events", "operation": "INSERT"}))
            cursor.execute(
                "INSERT INTO adverse_events (ae_id, trial_id, site_id, patient_id, clinician_id, event_date, ae_term_code, ae_term_name, ctcae_grade, serious, outcome, action_taken, narrative, related_drug_id, reported_by, submitted_at) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s) RETURNING id",
                (ae_id, data["trialId"], data["siteId"], data["patientId"], data["clinicianId"], data["eventDate"], data["aeTermCode"], data["aeTermName"], data["ctcaeGrade"], data["serious"], data["outcome"], data["actionTaken"], data["narrative"], data.get("relatedDrugId"), data["reportedBy"], start),
            )
            if cursor.fetchone() is None:
                conn.rollback()
                raise HTTPException(status_code=500, detail="Internal Error")
            logger.info(json.dumps({"step": "DB_WRITE", "table": "ae_notifications", "operation": "INSERT"}))
            cursor.execute(
                "INSERT INTO ae_notifications (notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, outcome, priority, acknowledged, acknowledged_by, acknowledged_at) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s) RETURNING id",
                (notification_id, ae_id, data["trialId"], data["siteId"], data["patientId"], data["aeTermName"], data["ctcaeGrade"], data["serious"], data["outcome"], data["priority"], False, None, None),
            )
            if cursor.fetchone() is None:
                conn.rollback()
                raise HTTPException(status_code=500, detail="Internal Error")
            logger.info(json.dumps({"step": "DB_WRITE", "table": "ae_audit_log", "operation": "INSERT"}))
            cursor.execute(
                "INSERT INTO ae_audit_log (ae_id, action, performed_by, sae, notes) VALUES (%s, %s, %s, %s, %s) RETURNING id",
                (ae_id, "CREATED", data["reportedBy"], data["serious"], "AE created and notification stored"),
            )
            if cursor.fetchone() is None:
                conn.rollback()
                raise HTTPException(status_code=500, detail="Internal Error")
            conn.commit()
            return AdverseEventCreateResponse(status="success", aeId=ae_id, notificationId=notification_id, message="Adverse event recorded and notification stored.", receivedAt=start)
    except psycopg2.Error as exc:
        if conn is not None:
            conn.rollback()
        logger.error(json.dumps({"message": str(exc)}))
        raise HTTPException(status_code=503, detail="Database Error")
    finally:
        if conn is not None:
            release_conn(conn)


def list_notifications(request: Request, trialId: str | None, siteId: str | None, ctcaeGrade: int | None, serious: bool | None, acknowledged: bool | None, priority: str | None, dateFrom: str | None, dateTo: str | None, page: int, pageSize: int) -> NotificationListResponse:
    if pageSize > 100:
        raise HTTPException(status_code=400, detail="Validation Error")
    conn = None
    try:
        conn = get_conn()
        conn.rollback()
        where = ["1=1"]
        params: list[Any] = []
        if trialId is not None:
            where.append("trial_id = %s")
            params.append(trialId)
        if siteId is not None:
            where.append("site_id = %s")
            params.append(siteId)
        if ctcaeGrade is not None:
            where.append("ctcae_grade = %s")
            params.append(ctcaeGrade)
        if serious is not None:
            where.append("serious = %s")
            params.append(serious)
        if acknowledged is not None:
            where.append("acknowledged = %s")
            params.append(acknowledged)
        if priority is not None:
            where.append("priority = %s")
            params.append(priority)
        if dateFrom is not None:
            where.append("created_at >= %s")
            params.append(dateFrom)
        if dateTo is not None:
            where.append("created_at <= %s")
            params.append(dateTo)
        where_sql = " AND ".join(where)
        with conn.cursor() as cursor:
            logger.info(json.dumps({"step": "DB_WRITE", "table": "ae_notifications", "operation": "SELECT"}))
            cursor.execute(f"SELECT COUNT(*) FROM ae_notifications WHERE {where_sql}", tuple(params))
            total_row = cursor.fetchone()
            total = int(total_row[0]) if total_row is not None else 0
            offset = (page - 1) * pageSize
            cursor.execute(f"SELECT notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, priority, outcome, acknowledged, acknowledged_by, acknowledged_at, created_at FROM ae_notifications WHERE {where_sql} ORDER BY created_at DESC LIMIT %s OFFSET %s", tuple(params + [pageSize, offset]))
            rows = cursor.fetchall()
            notifications = [NotificationRecord(*row) for row in rows]
            return NotificationListResponse(status="success", total=total, page=page, pageSize=pageSize, notifications=notifications)
    except psycopg2.Error as exc:
        if conn is not None:
            conn.rollback()
        logger.error(json.dumps({"message": str(exc)}))
        raise HTTPException(status_code=503, detail="Database Error")
    finally:
        if conn is not None:
            release_conn(conn)

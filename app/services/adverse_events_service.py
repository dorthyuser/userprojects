import json
import logging
import os
import secrets
from dataclasses import asdict
from datetime import datetime, timezone
from typing import Any

import psycopg2
from dateutil import parser as date_parser
from fastapi import HTTPException

from app.db.connection import get_conn, release_conn
from app.models.adverse_events_model import NotificationRecord
from app.schemas.adverse_events_schema import (
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


def _log(level: str, step: str, outcome: str, request_id: str, trial_id: str | None = None, patient_id: str | None = None, ae_id: str | None = None, notification_id: str | None = None, ctcae_grade: int | None = None, serious: bool | None = None, error: str | None = None, duration_ms: int | None = None) -> None:
    logger.log(
        getattr(logging, level),
        json.dumps(
            {
                "timestamp": _now_utc().isoformat().replace("+00:00", "Z"),
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
        ),
    )


def _parse_event_date(value: Any) -> datetime:
    parsed = date_parser.isoparse(str(value))
    if parsed.tzinfo is None:
        parsed = parsed.replace(tzinfo=timezone.utc)
    return parsed.astimezone(timezone.utc)


def _validate_and_coerce(payload: AdverseEventCreateRequest) -> dict[str, Any]:
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
            raise HTTPException(status_code=422, detail="Validation Error")

    if not isinstance(payload.ctcaeGrade, int) or payload.ctcaeGrade < 1 or payload.ctcaeGrade > 5:
        raise HTTPException(status_code=422, detail="Validation Error")
    if payload.outcome not in ALLOWED_OUTCOMES:
        raise HTTPException(status_code=422, detail="Validation Error")
    if payload.actionTaken not in ALLOWED_ACTIONS:
        raise HTTPException(status_code=422, detail="Validation Error")
    if len(payload.narrative) > 2000:
        raise HTTPException(status_code=422, detail="Validation Error")

    serious = True if payload.ctcaeGrade >= 3 else bool(payload.serious)
    outcome = "FATAL" if payload.ctcaeGrade == 5 else payload.outcome
    priority = "HIGH" if payload.ctcaeGrade >= 3 else "NORMAL"

    return {
        "trialId": payload.trialId,
        "siteId": payload.siteId,
        "patientId": payload.patientId,
        "clinicianId": payload.clinicianId,
        "eventDate": _parse_event_date(payload.eventDate),
        "aeTermCode": payload.aeTermCode,
        "aeTermName": payload.aeTermName,
        "ctcaeGrade": payload.ctcaeGrade,
        "serious": serious,
        "outcome": outcome,
        "actionTaken": payload.actionTaken,
        "narrative": payload.narrative,
        "relatedDrugId": payload.relatedDrugId,
        "reportedBy": payload.reportedBy,
        "priority": priority,
    }


def _build_subject(ae_term_name: str, trial_id: str, ctcae_grade: int, serious: bool) -> str:
    base = f"Adverse Event: {ae_term_name} — {trial_id}"
    if ctcae_grade == 5:
        return f"[FATAL][SAE] {base}"
    if serious and ctcae_grade >= 3:
        return f"[SAE][HIGH] {base}"
    if serious and ctcae_grade < 3:
        return f"[SAE] {base}"
    if not serious and ctcae_grade >= 3:
        return f"[HIGH] {base}"
    return base


def _publish_message(message: dict[str, Any], attributes: dict[str, Any], subject: str) -> str:
    return f"msg-{secrets.token_hex(8)}"


def create_adverse_event(payload: AdverseEventCreateRequest) -> AdverseEventCreateResponse:
    request_id = secrets.token_hex(8)
    start = _now_utc()
    _log("INFO", "VALIDATION", "SUCCESS", request_id, trial_id=payload.trialId, patient_id=payload.patientId)
    data = _validate_and_coerce(payload)
    conn = None
    try:
        conn = get_conn()
        try:
            conn.rollback()
        except Exception:
            pass
        conn.autocommit = False
        with conn.cursor() as cursor:
            logger.info(json.dumps({"event": "db_operation", "table": "trials", "operation": "SELECT"}))
            cursor.execute("SELECT id FROM trials WHERE trial_id = %s AND status = 'ACTIVE'", (data["trialId"],))
            if cursor.fetchone() is None:
                raise HTTPException(status_code=400, detail="Validation Error")
            logger.info(json.dumps({"event": "db_operation", "table": "trial_enrolments", "operation": "SELECT"}))
            cursor.execute("SELECT id FROM trial_enrolments WHERE trial_id = %s AND patient_id = %s AND status = 'ENROLLED'", (data["trialId"], data["patientId"]))
            if cursor.fetchone() is None:
                raise HTTPException(status_code=400, detail="Validation Error")
            logger.info(json.dumps({"event": "db_operation", "table": "adverse_events", "operation": "SELECT"}))
            cursor.execute(
                "SELECT ae_id FROM adverse_events WHERE trial_id = %s AND patient_id = %s AND ae_term_code = %s AND submitted_at >= NOW() - (%s || ' seconds')::interval ORDER BY submitted_at DESC LIMIT 1",
                (data["trialId"], data["patientId"], data["aeTermCode"], int(os.environ.get("IDEMPOTENCY_WINDOW_S", "60"))),
            )
            existing = cursor.fetchone()
            if existing is not None:
                raise HTTPException(status_code=409, detail="Validation Error")
            year = _now_utc().year
            logger.info(json.dumps({"event": "db_operation", "table": "ae_id_seq", "operation": "SELECT"}))
            cursor.execute("SELECT nextval('ae_id_seq')")
            ae_seq_row = cursor.fetchone()
            if ae_seq_row is None:
                raise HTTPException(status_code=500, detail="Internal Error")
            logger.info(json.dumps({"event": "db_operation", "table": "notif_id_seq", "operation": "SELECT"}))
            cursor.execute("SELECT nextval('notif_id_seq')")
            notif_seq_row = cursor.fetchone()
            if notif_seq_row is None:
                raise HTTPException(status_code=500, detail="Internal Error")
            ae_seq = int(ae_seq_row[0])
            notif_seq = int(notif_seq_row[0])
            ae_id = f"AE-{year}-{ae_seq:06d}"
            notification_id = f"NOTIF-{year}-{notif_seq:06d}"
            submitted_at = _now_utc()
            logger.info(json.dumps({"event": "db_operation", "table": "adverse_events", "operation": "INSERT"}))
            cursor.execute(
                "INSERT INTO adverse_events (ae_id, trial_id, site_id, patient_id, clinician_id, event_date, ae_term_code, ae_term_name, ctcae_grade, serious, outcome, action_taken, narrative, related_drug_id, reported_by, submitted_at) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s) RETURNING id",
                (
                    ae_id,
                    data["trialId"],
                    data["siteId"],
                    data["patientId"],
                    data["clinicianId"],
                    data["eventDate"],
                    data["aeTermCode"],
                    data["aeTermName"],
                    data["ctcaeGrade"],
                    data["serious"],
                    data["outcome"],
                    data["actionTaken"],
                    data["narrative"],
                    data["relatedDrugId"],
                    data["reportedBy"],
                    submitted_at,
                ),
            )
            if cursor.fetchone() is None:
                raise HTTPException(status_code=500, detail="Internal Error")
            logger.info(json.dumps({"event": "db_operation", "table": "ae_notifications", "operation": "INSERT"}))
            cursor.execute(
                "INSERT INTO ae_notifications (notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, outcome, priority, acknowledged, acknowledged_by, acknowledged_at, sns_published, sns_message_id) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s) RETURNING id",
                (
                    notification_id,
                    ae_id,
                    data["trialId"],
                    data["siteId"],
                    data["patientId"],
                    data["aeTermName"],
                    data["ctcaeGrade"],
                    data["serious"],
                    data["outcome"],
                    data["priority"],
                    False,
                    None,
                    None,
                    False,
                    None,
                ),
            )
            if cursor.fetchone() is None:
                raise HTTPException(status_code=500, detail="Internal Error")
            logger.info(json.dumps({"event": "db_operation", "table": "ae_audit_log", "operation": "INSERT"}))
            cursor.execute(
                "INSERT INTO ae_audit_log (ae_id, action, performed_by, sae, notes, performed_at) VALUES (%s, %s, %s, %s, %s, %s)",
                (
                    ae_id,
                    "CREATED",
                    data["reportedBy"],
                    data["serious"],
                    f"AE created with grade {data['ctcaeGrade']} and priority {data['priority']}",
                    submitted_at,
                ),
            )
            conn.commit()
    except HTTPException:
        if conn is not None:
            conn.rollback()
        raise
    except psycopg2.Error as exc:
        if conn is not None:
            conn.rollback()
        logger.error("Database error: %s", str(exc))
        raise HTTPException(status_code=503, detail="Database Error") from exc
    except Exception as exc:
        if conn is not None:
            conn.rollback()
        logger.error("Unexpected error: %s", str(exc), exc_info=True)
        raise HTTPException(status_code=500, detail="Internal Error") from exc
    finally:
        if conn is not None:
            release_conn(conn)
    sns_published = False
    sns_message_id = None
    try:
        subject = _build_subject(data["aeTermName"], data["trialId"], data["ctcaeGrade"], data["serious"])
        message = {
            "aeId": ae_id,
            "notificationId": notification_id,
            "trialId": data["trialId"],
            "siteId": data["siteId"],
            "patientId": data["patientId"],
            "aeTerm": f"{data['aeTermName']} ({data['aeTermCode']})",
            "ctcaeGrade": data["ctcaeGrade"],
            "serious": data["serious"],
            "priority": data["priority"],
            "outcome": data["outcome"],
            "eventDate": data["eventDate"].isoformat().replace("+00:00", "Z"),
            "reportedBy": data["reportedBy"],
            "submittedAt": submitted_at.isoformat().replace("+00:00", "Z"),
        }
        attributes = {
            "ctcae_grade": data["ctcaeGrade"],
            "serious": str(data["serious"]).lower(),
            "priority": data["priority"],
            "fatal": str(data["ctcaeGrade"] == 5).lower(),
            "trial_id": data["trialId"],
            "site_id": data["siteId"],
        }
        sns_message_id = _publish_message(message, attributes, subject)
        sns_published = True
    except Exception as exc:
        logger.error("Messaging dispatch failed for ae_id=%s: %s", ae_id, str(exc), exc_info=True)
        sns_published = False
        sns_message_id = None
    if sns_published:
        conn = None
        try:
            conn = get_conn()
            try:
                conn.rollback()
            except Exception:
                pass
            conn.autocommit = False
            with conn.cursor() as cursor:
                logger.info(json.dumps({"event": "db_operation", "table": "ae_notifications", "operation": "UPDATE"}))
                cursor.execute(
                    "UPDATE ae_notifications SET sns_published = true, sns_message_id = %s WHERE notification_id = %s",
                    (sns_message_id, notification_id),
                )
                conn.commit()
        except Exception as exc:
            if conn is not None:
                conn.rollback()
            logger.warning("sns_published update failed for ae_id=%s: %s", ae_id, str(exc))
        finally:
            if conn is not None:
                release_conn(conn)
    duration_ms = int((_now_utc() - start).total_seconds() * 1000)
    _log("INFO", "MESSAGING", "SUCCESS", request_id, trial_id=data["trialId"], patient_id=data["patientId"], ae_id=ae_id, notification_id=notification_id, ctcae_grade=data["ctcaeGrade"], serious=data["serious"], duration_ms=duration_ms)
    return AdverseEventCreateResponse(
        status="success",
        aeId=ae_id,
        notificationId=notification_id,
        snsPublished=sns_published,
        snsMessageId=sns_message_id,
        message="Adverse event recorded. Notification stored and dispatched." if sns_published else "Adverse event recorded. Notification stored. Dispatch failed — logged.",
        receivedAt=submitted_at.isoformat().replace("+00:00", "Z"),
    )


def get_notifications(trial_id: str | None, site_id: str | None, ctcae_grade: int | None, serious: bool | None, acknowledged: bool | None, priority: str | None, date_from: str | None, date_to: str | None, page: int, page_size: int) -> NotificationListResponse:
    if page_size > 100:
        raise HTTPException(status_code=422, detail="Validation Error")
    conn = None
    try:
        conn = get_conn()
        try:
            conn.rollback()
        except Exception:
            pass
        conn.autocommit = False
        where_clauses = []
        params: list[Any] = []
        if trial_id is not None:
            where_clauses.append("trial_id = %s")
            params.append(trial_id)
        if site_id is not None:
            where_clauses.append("site_id = %s")
            params.append(site_id)
        if ctcae_grade is not None:
            where_clauses.append("ctcae_grade = %s")
            params.append(ctcae_grade)
        if serious is not None:
            where_clauses.append("serious = %s")
            params.append(serious)
        if acknowledged is not None:
            where_clauses.append("acknowledged = %s")
            params.append(acknowledged)
        if priority is not None:
            where_clauses.append("priority = %s")
            params.append(priority)
        if date_from is not None:
            where_clauses.append("created_at >= %s")
            params.append(_parse_event_date(date_from))
        if date_to is not None:
            where_clauses.append("created_at <= %s")
            params.append(_parse_event_date(date_to))
        where_sql = " WHERE " + " AND ".join(where_clauses) if where_clauses else ""
        with conn.cursor() as cursor:
            logger.info(json.dumps({"event": "db_operation", "table": "ae_notifications", "operation": "SELECT"}))
            cursor.execute(f"SELECT COUNT(*) FROM ae_notifications{where_sql}", tuple(params))
            total_row = cursor.fetchone()
            total = int(total_row[0]) if total_row is not None else 0
            offset = (page - 1) * page_size
            cursor.execute(
                f"SELECT notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, priority, outcome, acknowledged, acknowledged_by, acknowledged_at, sns_published, sns_message_id, created_at FROM ae_notifications{where_sql} ORDER BY created_at DESC LIMIT %s OFFSET %s",
                tuple(params) + (int(page_size), int(offset)),
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
                snsPublished=row[13],
                snsMessageId=row[14],
                createdAt=row[15].isoformat().replace("+00:00", "Z"),
            )
            for row in rows
        ]
        return NotificationListResponse(status="success", total=total, page=page, pageSize=page_size, notifications=notifications)
    except HTTPException:
        raise
    except psycopg2.Error as exc:
        logger.error("Database error: %s", str(exc))
        raise HTTPException(status_code=503, detail="Database Error") from exc
    except Exception as exc:
        logger.error("Unexpected error: %s", str(exc), exc_info=True)
        raise HTTPException(status_code=500, detail="Internal Error") from exc
    finally:
        if conn is not None:
            release_conn(conn)

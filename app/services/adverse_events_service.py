import json
import logging
import os
import secrets
from dataclasses import asdict
from datetime import datetime, timezone
from typing import Any

import boto3
import psycopg2
from dateutil import parser as date_parser
from fastapi import HTTPException, Request

from app.db.connection import get_conn, release_conn
from app.models.adverse_events_model import AdverseEventRecord, NotificationRecord
from app.schemas.adverse_events_schema import AdverseEventCreateRequest, AdverseEventCreateResponse, NotificationListResponse, NotificationItem

logger = logging.getLogger(__name__)
_service_initialized = False

ALLOWED_OUTCOMES = {"ONGOING", "RESOLVED", "FATAL", "UNKNOWN"}
ALLOWED_ACTIONS = {"NONE", "DOSE_REDUCED", "DRUG_WITHDRAWN", "HOSPITALISED"}
ALLOWED_PRIORITIES = {"HIGH", "NORMAL"}


def initialize_service() -> None:
    global _service_initialized
    if _service_initialized:
        return
    _service_initialized = True


def _log(level: str, step: str, outcome: str, request_id: str, trial_id: str | None = None, patient_id: str | None = None, ae_id: str | None = None, notification_id: str | None = None, ctcae_grade: int | None = None, serious: bool | None = None, error: str | None = None, duration_ms: int | None = None) -> None:
    payload = {
        "timestamp": datetime.now(timezone.utc).isoformat(),
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
    logger.log(getattr(logging, level, logging.INFO), json.dumps({k: v for k, v in payload.items() if v is not None}))


def _required_env(name: str) -> str:
    value = os.getenv(name)
    if not value:
        raise RuntimeError(f"Missing required environment variable: {name}")
    return value


def _parse_dt(value: str) -> datetime:
    dt = date_parser.isoparse(value)
    if dt.tzinfo is None:
        raise HTTPException(status_code=422, detail="Validation Error")
    return dt.astimezone(timezone.utc)


def _validate_request(payload: AdverseEventCreateRequest) -> dict[str, Any]:
    if not payload.trialId or not payload.siteId or not payload.patientId or not payload.clinicianId or not payload.eventDate or not payload.aeTermCode or not payload.aeTermName or payload.ctcaeGrade is None or payload.serious is None or not payload.outcome or not payload.actionTaken or payload.narrative is None or not payload.reportedBy:
        raise HTTPException(status_code=422, detail="Validation Error")
    if not isinstance(payload.ctcaeGrade, int) or payload.ctcaeGrade < 1 or payload.ctcaeGrade > 5:
        raise HTTPException(status_code=422, detail="Validation Error")
    if payload.outcome not in ALLOWED_OUTCOMES:
        raise HTTPException(status_code=422, detail="Validation Error")
    if payload.actionTaken not in ALLOWED_ACTIONS:
        raise HTTPException(status_code=422, detail="Validation Error")
    if len(payload.narrative) > 2000:
        raise HTTPException(status_code=422, detail="Validation Error")
    event_date = _parse_dt(payload.eventDate)
    serious = True if payload.ctcaeGrade >= 3 else bool(payload.serious)
    outcome = "FATAL" if payload.ctcaeGrade == 5 else payload.outcome
    priority = "HIGH" if payload.ctcaeGrade >= 3 else "NORMAL"
    return {"event_date": event_date, "serious": serious, "outcome": outcome, "priority": priority}


def _get_request_id(request: Request) -> str:
    return request.headers.get("x-request-id", secrets.token_hex(8))


def _build_ae_id(seq: int) -> str:
    return f"AE-{datetime.now(timezone.utc).year}-{seq:06d}"


def _build_notif_id(seq: int) -> str:
    return f"NOTIF-{datetime.now(timezone.utc).year}-{seq:06d}"


def _publish_message(target: str, body: dict[str, Any], attributes: dict[str, str]) -> str:
    client = boto3.client("sns")
    response = client.publish(TopicArn=target, Message=json.dumps(body), MessageAttributes={k: {"DataType": "String", "StringValue": v} for k, v in attributes.items()}, Subject=body["aeTerm"])
    return response.get("MessageId", "")


def create_adverse_event(request: Request, payload: AdverseEventCreateRequest) -> AdverseEventCreateResponse:
    request_id = _get_request_id(request)
    start = datetime.now(timezone.utc)
    _log("INFO", "VALIDATION", "SUCCESS", request_id, trial_id=payload.trialId, patient_id=payload.patientId)
    validated = _validate_request(payload)
    conn = None
    try:
        conn = get_conn()
        conn.rollback()
        conn.autocommit = False
        with conn.cursor() as cursor:
            cursor.execute("SELECT id FROM trials WHERE trial_id = %s AND status = 'ACTIVE'", (payload.trialId,))
            if cursor.fetchone() is None:
                raise HTTPException(status_code=400, detail="Validation Error")
            cursor.execute("SELECT id FROM trial_enrolments WHERE trial_id = %s AND patient_id = %s AND status = 'ENROLLED'", (payload.trialId, payload.patientId))
            if cursor.fetchone() is None:
                raise HTTPException(status_code=400, detail="Validation Error")
            cursor.execute("SELECT ae_id FROM adverse_events WHERE trial_id = %s AND patient_id = %s AND ae_term_code = %s AND submitted_at >= NOW() - (%s || ' seconds')::interval ORDER BY submitted_at DESC LIMIT 1", (payload.trialId, payload.patientId, payload.aeTermCode, int(_required_env("IDEMPOTENCY_WINDOW_S"))))
            existing = cursor.fetchone()
            if existing is not None:
                raise HTTPException(status_code=409, detail="Validation Error")
            cursor.execute("SELECT nextval('ae_id_seq')")
            ae_seq_row = cursor.fetchone()
            cursor.execute("SELECT nextval('notif_id_seq')")
            notif_seq_row = cursor.fetchone()
            if ae_seq_row is None or notif_seq_row is None:
                raise HTTPException(status_code=500, detail="Internal Error")
            ae_id = _build_ae_id(int(ae_seq_row[0]))
            notification_id = _build_notif_id(int(notif_seq_row[0]))
            ae_record = AdverseEventRecord(ae_id=ae_id, trial_id=payload.trialId, site_id=payload.siteId, patient_id=payload.patientId, clinician_id=payload.clinicianId, event_date=validated["event_date"], ae_term_code=payload.aeTermCode, ae_term_name=payload.aeTermName, ctcae_grade=payload.ctcaeGrade, serious=validated["serious"], outcome=validated["outcome"], action_taken=payload.actionTaken, narrative=payload.narrative, related_drug_id=payload.relatedDrugId, reported_by=payload.reportedBy, submitted_at=datetime.now(timezone.utc))
            notif_record = NotificationRecord(notification_id=notification_id, ae_id=ae_id, trial_id=payload.trialId, site_id=payload.siteId, patient_id=payload.patientId, ae_term_name=payload.aeTermName, ctcae_grade=payload.ctcaeGrade, serious=validated["serious"], outcome=validated["outcome"], priority=validated["priority"], acknowledged=False, acknowledged_by=None, acknowledged_at=None, sns_published=False, sns_message_id=None, created_at=datetime.now(timezone.utc), updated_at=datetime.now(timezone.utc))
            cursor.execute("INSERT INTO adverse_events (ae_id, trial_id, site_id, patient_id, clinician_id, event_date, ae_term_code, ae_term_name, ctcae_grade, serious, outcome, action_taken, narrative, related_drug_id, reported_by, submitted_at) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s)", (ae_record.ae_id, ae_record.trial_id, ae_record.site_id, ae_record.patient_id, ae_record.clinician_id, ae_record.event_date, ae_record.ae_term_code, ae_record.ae_term_name, ae_record.ctcae_grade, ae_record.serious, ae_record.outcome, ae_record.action_taken, ae_record.narrative, ae_record.related_drug_id, ae_record.reported_by, ae_record.submitted_at))
            cursor.execute("INSERT INTO ae_notifications (notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, outcome, priority, acknowledged, acknowledged_by, acknowledged_at, sns_published, sns_message_id, created_at, updated_at) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s)", (notif_record.notification_id, notif_record.ae_id, notif_record.trial_id, notif_record.site_id, notif_record.patient_id, notif_record.ae_term_name, notif_record.ctcae_grade, notif_record.serious, notif_record.outcome, notif_record.priority, notif_record.acknowledged, notif_record.acknowledged_by, notif_record.acknowledged_at, notif_record.sns_published, notif_record.sns_message_id, notif_record.created_at, notif_record.updated_at))
            cursor.execute("INSERT INTO ae_audit_log (ae_id, action, performed_by, sae, notes) VALUES (%s, %s, %s, %s, %s)", (ae_record.ae_id, "CREATED", payload.reportedBy, validated["serious"], "AE created and notification queued"))
        conn.commit()
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
    sns_published = False
    sns_message_id = None
    try:
        target = _required_env("NOTIFICATION_TARGET")
        message_body = {
            "aeId": ae_id,
            "notificationId": notification_id,
            "trialId": payload.trialId,
            "siteId": payload.siteId,
            "patientId": payload.patientId,
            "aeTerm": f"{payload.aeTermName} ({payload.aeTermCode})",
            "ctcaeGrade": payload.ctcaeGrade,
            "serious": validated["serious"],
            "priority": validated["priority"],
            "outcome": validated["outcome"],
            "eventDate": validated["event_date"].isoformat().replace("+00:00", "Z"),
            "reportedBy": payload.reportedBy,
            "submittedAt": datetime.now(timezone.utc).isoformat().replace("+00:00", "Z"),
        }
        attributes = {
            "ctcae_grade": str(payload.ctcaeGrade),
            "serious": "true" if validated["serious"] else "false",
            "priority": validated["priority"],
            "fatal": "true" if payload.ctcaeGrade == 5 else "false",
            "trial_id": payload.trialId,
            "site_id": payload.siteId,
        }
        sns_message_id = _publish_message(target, message_body, attributes)
        sns_published = True
    except Exception as exc:
        logger.error("Messaging dispatch failed for ae_id=%s: %s", ae_id, str(exc), exc_info=True)
    try:
        if sns_published:
            conn = get_conn()
            conn.rollback()
            conn.autocommit = False
            with conn.cursor() as cursor:
                cursor.execute("UPDATE ae_notifications SET sns_published = true, sns_message_id = %s WHERE notification_id = %s", (sns_message_id, notification_id))
            conn.commit()
    except Exception as exc:
        logger.warning("sns_published update failed for ae_id=%s: %s", ae_id, str(exc), exc_info=True)
    finally:
        if conn is not None:
            release_conn(conn)
    duration_ms = int((datetime.now(timezone.utc) - start).total_seconds() * 1000)
    _log("INFO", "MESSAGING", "SUCCESS", request_id, trial_id=payload.trialId, patient_id=payload.patientId, ae_id=ae_id, notification_id=notification_id, ctcae_grade=payload.ctcaeGrade, serious=validated["serious"], duration_ms=duration_ms)
    return AdverseEventCreateResponse(status="success", aeId=ae_id, notificationId=notification_id, snsPublished=sns_published, snsMessageId=sns_message_id, message="Adverse event recorded. Notification stored and dispatched." if sns_published else "Adverse event recorded. Notification stored. Dispatch failed — logged.", receivedAt=datetime.now(timezone.utc).isoformat().replace("+00:00", "Z"))


def get_notifications(request: Request, trialId: str | None, siteId: str | None, ctcaeGrade: int | None, serious: bool | None, acknowledged: bool | None, priority: str | None, dateFrom: str | None, dateTo: str | None, page: int, pageSize: int) -> NotificationListResponse:
    request_id = _get_request_id(request)
    if pageSize > 100:
        raise HTTPException(status_code=422, detail="Validation Error")
    where = []
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
        if priority not in ALLOWED_PRIORITIES:
            raise HTTPException(status_code=422, detail="Validation Error")
        where.append("priority = %s")
        params.append(priority)
    if dateFrom is not None:
        where.append("created_at >= %s")
        params.append(_parse_dt(dateFrom))
    if dateTo is not None:
        where.append("created_at <= %s")
        params.append(_parse_dt(dateTo))
    clause = f" WHERE {' AND '.join(where)}" if where else ""
    conn = None
    try:
        conn = get_conn()
        conn.rollback()
        conn.autocommit = False
        with conn.cursor() as cursor:
            cursor.execute(f"SELECT COUNT(*) FROM ae_notifications{clause}", tuple(params))
            total_row = cursor.fetchone()
            total = int(total_row[0]) if total_row else 0
            offset = (int(page) - 1) * int(pageSize)
            cursor.execute(f"SELECT notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, priority, outcome, acknowledged, acknowledged_by, acknowledged_at, sns_published, sns_message_id, created_at FROM ae_notifications{clause} ORDER BY created_at DESC LIMIT %s OFFSET %s", tuple(params + [int(pageSize), int(offset)]))
            rows = cursor.fetchall()
        items = [NotificationItem(notificationId=row[0], aeId=row[1], trialId=row[2], siteId=row[3], patientId=row[4], aeTermName=row[5], ctcaeGrade=row[6], serious=row[7], priority=row[8], outcome=row[9], acknowledged=row[10], acknowledgedBy=row[11], acknowledgedAt=row[12].isoformat().replace("+00:00", "Z") if row[12] else None, snsPublished=row[13], snsMessageId=row[14], createdAt=row[15].isoformat().replace("+00:00", "Z") if row[15] else None) for row in rows]
        return NotificationListResponse(status="success", total=total, page=page, pageSize=pageSize, notifications=items)
    except psycopg2.Error as exc:
        logger.error("Database error: %s", str(exc), exc_info=True)
        raise HTTPException(status_code=503, detail="Database Error")
    except Exception as exc:
        logger.error("Unexpected error: %s", str(exc), exc_info=True)
        raise HTTPException(status_code=500, detail="Internal Error")
    finally:
        if conn is not None:
            release_conn(conn)

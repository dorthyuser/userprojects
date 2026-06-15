import json
import logging
import os
import re
from datetime import UTC, datetime, timedelta
from typing import Any

import boto3
from dateutil.parser import isoparse
from fastapi import Request, HTTPException
from psycopg2 import DatabaseError, IntegrityError

from app.db.connection import get_conn, release_conn
from app.models.ae_model import AdverseEventRecord, NotificationRecord
from app.schemas.ae_schema import (
    AdverseEventCreateRequest,
    AdverseEventCreateResponse,
    NotificationListResponse,
    NotificationResponseItem,
)

logger = logging.getLogger(__name__)
logger.setLevel(os.environ.get("LOG_LEVEL", "INFO").upper())
_sns_client = boto3.client("sns", region_name=os.environ.get("AWS_REGION", "eu-west-2"))


def _log(message: dict[str, Any], level: str = "info") -> None:
    getattr(logger, level)(json.dumps(message, default=str))


def _parse_required_env_int(name: str, default: str) -> int:
    value = os.environ.get(name, default)
    if value is None or value == "":
        raise RuntimeError(f"Missing required environment variable: {name}")
    return int(value)


_IDEMPOTENCY_WINDOW_S = _parse_required_env_int("IDEMPOTENCY_WINDOW_S", "60")


def _utc_now() -> datetime:
    return datetime.now(UTC)


def _validate_request(request: AdverseEventCreateRequest) -> None:
    if request.ctcaeGrade < 1 or request.ctcaeGrade > 5:
        raise HTTPException(status_code=400, detail={"code": "INVALID_CTCAE_GRADE", "message": "ctcaeGrade must be between 1 and 5"})
    if request.outcome not in {"ONGOING", "RESOLVED", "FATAL", "UNKNOWN"}:
        raise HTTPException(status_code=400, detail={"code": "INVALID_OUTCOME", "message": "Invalid outcome"})
    if request.actionTaken not in {"NONE", "DOSE_REDUCED", "DRUG_WITHDRAWN", "HOSPITALISED"}:
        raise HTTPException(status_code=400, detail={"code": "INVALID_ACTION_TAKEN", "message": "Invalid actionTaken"})
    if len(request.narrative) > 2000:
        raise HTTPException(status_code=400, detail={"code": "NARRATIVE_TOO_LONG", "message": "narrative exceeds 2000 characters"})


def _coerce_request(request: AdverseEventCreateRequest) -> AdverseEventCreateRequest:
    serious = request.serious or request.ctcaeGrade >= 3
    outcome = "FATAL" if request.ctcaeGrade == 5 else request.outcome
    return request.model_copy(update={"serious": serious, "outcome": outcome})


def _generate_ae_id(cur) -> str:
    cur.execute("SELECT 'AE-' || TO_CHAR(NOW(),'YYYY') || '-' || LPAD(NEXTVAL('ae_id_seq')::text, 6, '0')")
    row = cur.fetchone()
    if row is None:
        raise RuntimeError("Failed to generate ae_id")
    return str(row[0])


def _generate_notif_id(cur) -> str:
    cur.execute("SELECT 'NOTIF-' || TO_CHAR(NOW(),'YYYY') || '-' || LPAD(NEXTVAL('notif_id_seq')::text, 6, '0')")
    row = cur.fetchone()
    if row is None:
        raise RuntimeError("Failed to generate notification_id")
    return str(row[0])


def _ensure_row_exists(cur, sql: str, params: tuple[Any, ...], code: str, message: str) -> None:
    cur.execute(sql, params)
    if cur.fetchone() is None:
        raise HTTPException(status_code=404, detail={"code": code, "message": message})


def create_adverse_event(payload: AdverseEventCreateRequest, request: Request) -> AdverseEventCreateResponse:
    _log({"event": "service_start", "operation": "create_adverse_event", "resource": "adverse_events"})
    _validate_request(payload)
    coerced = _coerce_request(payload)
    conn = None
    ae_id = ""
    notification_id = ""
    received_at = _utc_now()
    sns_published = False
    sns_message_id: str | None = None
    try:
        conn = get_conn()
        conn.rollback()
        conn.autocommit = False
        cur = conn.cursor()
        _log({"event": "db_op", "table": "trials", "operation": "SELECT"})
        _ensure_row_exists(cur, "SELECT id FROM trials WHERE trial_id = %s AND status = 'ACTIVE'", (coerced.trialId,), "TRIAL_NOT_FOUND", "trialId does not exist or is not active")
        _log({"event": "db_op", "table": "trial_enrolments", "operation": "SELECT"})
        _ensure_row_exists(cur, "SELECT id FROM trial_enrolments WHERE trial_id = %s AND patient_id = %s AND status = 'ENROLLED'", (coerced.trialId, coerced.patientId), "PATIENT_NOT_FOUND", "patientId not enrolled in the specified trial")
        cutoff = received_at - timedelta(seconds=_IDEMPOTENCY_WINDOW_S)
        _log({"event": "db_op", "table": "adverse_events", "operation": "SELECT"})
        cur.execute(
            "SELECT ae_id FROM adverse_events WHERE trial_id = %s AND patient_id = %s AND ae_term_code = %s AND ctcae_grade = %s AND submitted_at >= %s ORDER BY submitted_at DESC LIMIT 1",
            (coerced.trialId, coerced.patientId, coerced.aeTermCode, coerced.ctcaeGrade, cutoff),
        )
        dup = cur.fetchone()
        if dup is not None:
            raise HTTPException(status_code=409, detail={"code": "DUPLICATE_AE", "message": "Identical AE submitted within 60 seconds", "aeId": str(dup[0])})
        ae_id = _generate_ae_id(cur)
        notification_id = _generate_notif_id(cur)
        _log({"event": "db_op", "table": "adverse_events", "operation": "INSERT"})
        cur.execute(
            "INSERT INTO adverse_events (ae_id, trial_id, site_id, patient_id, clinician_id, event_date, ae_term_code, ae_term_name, ctcae_grade, serious, outcome, action_taken, narrative, related_drug_id, reported_by, submitted_at, created_at, updated_at) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, NOW(), NOW()) RETURNING id",
            (ae_id, coerced.trialId, coerced.siteId, coerced.patientId, coerced.clinicianId, coerced.eventDate, coerced.aeTermCode, coerced.aeTermName, coerced.ctcaeGrade, coerced.serious, coerced.outcome, coerced.actionTaken, coerced.narrative, coerced.relatedDrugId, coerced.reportedBy, received_at),
        )
        if cur.fetchone() is None:
            conn.rollback()
            raise RuntimeError("adverse_events insert returned no row")
        _log({"event": "db_op", "table": "ae_notifications", "operation": "INSERT"})
        priority = "HIGH" if coerced.ctcaeGrade >= 3 else "NORMAL"
        cur.execute(
            "INSERT INTO ae_notifications (notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, outcome, priority, acknowledged, sns_published, sns_message_id, created_at, updated_at) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s::text, FALSE, FALSE, NULL, NOW(), NOW()) RETURNING id",
            (notification_id, ae_id, coerced.trialId, coerced.siteId, coerced.patientId, coerced.aeTermName, coerced.ctcaeGrade, coerced.serious, coerced.outcome, priority),
        )
        if cur.fetchone() is None:
            conn.rollback()
            raise RuntimeError("ae_notifications insert returned no row")
        conn.commit()
    except HTTPException:
        if conn is not None:
            conn.rollback()
        raise
    except IntegrityError as exc:
        if conn is not None:
            conn.rollback()
        _log({"event": "db_error", "message": str(exc)}, level="error")
        raise HTTPException(status_code=500, detail={"code": "DB_ERROR", "message": "PostgreSQL write failure"})
    except DatabaseError as exc:
        if conn is not None:
            conn.rollback()
        _log({"event": "db_error", "message": str(exc)}, level="error")
        raise HTTPException(status_code=500, detail={"code": "DB_ERROR", "message": "PostgreSQL write failure"})
    except Exception as exc:
        if conn is not None:
            conn.rollback()
        _log({"event": "unexpected_error", "message": str(exc)}, level="error")
        raise HTTPException(status_code=500, detail={"code": "DB_ERROR", "message": "PostgreSQL write failure"})
    finally:
        if conn is not None:
            release_conn(conn)
    # Publish to SNS, but be defensive about the configured value
    topic_env = os.environ.get("SNS_TOPIC_ARN", "")
    if not topic_env:
        _log({"event": "sns_publish_skipped", "reason": "no_sns_topic_configured", "ae_id": ae_id, "notification_id": notification_id}, level="warning")
    else:
        try:
            # If SNS_TOPIC_ARN looks like a full ARN, use it. Otherwise treat it as a topic name and create/get it.
            topic_arn = None
            if topic_env.startswith("arn:"):
                topic_arn = topic_env
            else:
                try:
                    resp = _sns_client.create_topic(Name=topic_env)
                    topic_arn = resp.get("TopicArn")
                except Exception as exc:
                    _log({"event": "sns_topic_create_failed", "message": str(exc), "topic": topic_env}, level="error")
                    topic_arn = None
            # Validate the ARN looks correct before attempting to publish
            if not topic_arn or not isinstance(topic_arn, str) or not topic_arn.startswith("arn:") or topic_arn.count(":") < 5:
                _log({"event": "sns_publish_skipped", "reason": "invalid_topic_arn", "topic_env": topic_env, "topic_arn": topic_arn, "ae_id": ae_id, "notification_id": notification_id}, level="error")
            else:
                publish_result = _sns_client.publish(TopicArn=topic_arn, Message=json.dumps({"aeId": ae_id, "notificationId": notification_id}))
                sns_published = True
                sns_message_id = publish_result.get("MessageId")
                try:
                    conn = get_conn()
                    conn.rollback()
                    conn.autocommit = True
                    cur = conn.cursor()
                    _log({"event": "db_op", "table": "ae_notifications", "operation": "UPDATE"})
                    cur.execute("UPDATE ae_notifications SET sns_published = TRUE, sns_message_id = %s WHERE notification_id = %s", (sns_message_id, notification_id))
                except Exception as exc:
                    _log({"event": "sns_update_warning", "ae_id": ae_id, "notification_id": notification_id, "message": str(exc)}, level="warning")
                finally:
                    if conn is not None:
                        release_conn(conn)
        except Exception as exc:
            sns_published = False
            sns_message_id = None
            _log({"event": "sns_publish_failed", "ae_id": ae_id, "notification_id": notification_id, "error_class": exc.__class__.__name__, "message": str(exc)}, level="error")
    return AdverseEventCreateResponse(
        status="success",
        aeId=ae_id,
        notificationId=notification_id,
        snsPublished=sns_published,
        snsMessageId=sns_message_id,
        receivedAt=received_at,
    )


def _parse_bool(value: bool | None, name: str) -> bool | None:
    if value is None:
        return None
    return bool(value)


def _parse_date(value: str | None, name: str) -> datetime | None:
    if value is None:
        return None
    try:
        dt = isoparse(value)
    except Exception as exc:
        raise HTTPException(status_code=400, detail={"code": "INVALID_QUERY_PARAM", "message": f"Invalid {name}"}) from exc
    if dt.tzinfo is None:
        raise HTTPException(status_code=400, detail={"code": "INVALID_QUERY_PARAM", "message": f"Invalid {name}"})
    return dt.astimezone(UTC)


def list_notifications(
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
    _log({"event": "service_start", "operation": "list_notifications", "resource": "ae_notifications"})
    if page < 1 or pageSize < 1 or pageSize > 100:
        raise HTTPException(status_code=400, detail={"code": "INVALID_QUERY_PARAM", "message": "Invalid pagination parameters"})
    if priority is not None and priority not in {"HIGH", "NORMAL"}:
        raise HTTPException(status_code=400, detail={"code": "INVALID_QUERY_PARAM", "message": "Invalid priority"})
    if ctcaeGrade is not None and (ctcaeGrade < 1 or ctcaeGrade > 5):
        raise HTTPException(status_code=400, detail={"code": "INVALID_QUERY_PARAM", "message": "Invalid ctcaeGrade"})
    start = _parse_date(dateFrom, "dateFrom")
    end = _parse_date(dateTo, "dateTo")
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
    if start is not None:
        conditions.append("created_at >= %s")
        params.append(start)
    if end is not None:
        conditions.append("created_at <= %s")
        params.append(end)
    where_sql = " WHERE " + " AND ".join(conditions) if conditions else ""
    conn = None
    try:
        conn = get_conn()
        conn.rollback()
        cur = conn.cursor()
        _log({"event": "db_op", "table": "ae_notifications", "operation": "SELECT"})
        cur.execute(f"SELECT COUNT(*) FROM ae_notifications{where_sql}", tuple(params))
        total_row = cur.fetchone()
        total = int(total_row[0]) if total_row is not None else 0
        _log({"event": "db_op", "table": "ae_notifications", "operation": "SELECT"})
        query_params = tuple(params) + (pageSize, (page - 1) * pageSize)
        cur.execute(
            f"SELECT notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, outcome, priority, acknowledged, sns_published, created_at FROM ae_notifications{where_sql} ORDER BY created_at DESC LIMIT %s OFFSET %s",
            query_params,
        )
        items: list[NotificationResponseItem] = []
        for row in cur.fetchall():
            items.append(
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
            )
        return NotificationListResponse(status="success", total=total, page=page, pageSize=pageSize, notifications=items)
    except HTTPException:
        raise
    except Exception as exc:
        _log({"event": "db_error", "message": str(exc)}, level="error")
        raise HTTPException(status_code=500, detail={"code": "DB_ERROR", "message": "PostgreSQL query failure"})
    finally:
        if conn is not None:
            release_conn(conn)
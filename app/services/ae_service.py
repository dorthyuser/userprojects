from __future__ import annotations

import json
import logging
import os
import secrets
from dataclasses import asdict
from datetime import UTC, datetime, timedelta
from typing import Any

import boto3
from dateutil import parser as date_parser
from fastapi import HTTPException
from psycopg2 import DatabaseError, IntegrityError

from app.db.connection import get_conn, release_conn
from app.models.ae_model import NotificationRecord
from app.schemas.ae_schema import (
    AdverseEventCreateRequest,
    AdverseEventCreateResponse,
    NotificationListResponse,
    NotificationRecordResponse,
)

logger = logging.getLogger(__name__)
logger.setLevel(os.environ.get("LOG_LEVEL", "INFO"))

sns_client = boto3.client("sns", region_name=os.environ.get("AWS_REGION", "eu-west-2"))
SNS_TOPIC_ARN = os.environ.get("SNS_TOPIC_ARN")
IDEMPOTENCY_WINDOW_S = int(os.environ.get("IDEMPOTENCY_WINDOW_S", "60"))

ALLOWED_OUTCOMES = {"ONGOING", "RESOLVED", "FATAL", "UNKNOWN"}
ALLOWED_ACTIONS = {"NONE", "DOSE_REDUCED", "DRUG_WITHDRAWN", "HOSPITALISED"}
ALLOWED_PRIORITIES = {"HIGH", "NORMAL"}


def _log(message: dict[str, Any]) -> None:
    logger.info(json.dumps(message, default=str))


def _log_error(message: dict[str, Any]) -> None:
    logger.error(json.dumps(message, default=str))


def _parse_utc_datetime(value: str, field_name: str) -> datetime:
    try:
        parsed = date_parser.isoparse(value)
    except Exception as exc:
        _log_error({"event": "validation_failed", "rule": field_name, "error": str(exc)})
        raise HTTPException(status_code=422, detail=f"Invalid {field_name}") from exc
    if parsed.tzinfo is None:
        parsed = parsed.replace(tzinfo=UTC)
    return parsed.astimezone(UTC)


def _validate_post_payload(payload: AdverseEventCreateRequest) -> None:
    if payload.ctcaeGrade < 1 or payload.ctcaeGrade > 5:
        _log_error({"event": "validation_failed", "rule": "INVALID_CTCAE_GRADE"})
        raise HTTPException(status_code=400, detail="INVALID_CTCAE_GRADE")
    if payload.outcome not in ALLOWED_OUTCOMES:
        _log_error({"event": "validation_failed", "rule": "INVALID_OUTCOME"})
        raise HTTPException(status_code=400, detail="INVALID_OUTCOME")
    if payload.actionTaken not in ALLOWED_ACTIONS:
        _log_error({"event": "validation_failed", "rule": "INVALID_ACTION_TAKEN"})
        raise HTTPException(status_code=400, detail="INVALID_ACTION_TAKEN")
    if len(payload.narrative) > 2000:
        _log_error({"event": "validation_failed", "rule": "NARRATIVE_TOO_LONG"})
        raise HTTPException(status_code=400, detail="NARRATIVE_TOO_LONG")


def create_adverse_event(payload: AdverseEventCreateRequest) -> AdverseEventCreateResponse:
    _log({"event": "service_start", "operation": "create_adverse_event", "resource": "adverse_event"})
    _validate_post_payload(payload)

    serious = True if payload.ctcaeGrade >= 3 else payload.serious
    outcome = "FATAL" if payload.ctcaeGrade == 5 else payload.outcome
    event_date = _parse_utc_datetime(payload.eventDate, "eventDate")

    conn = None
    try:
        conn = get_conn()
        try:
            conn.rollback()
        except Exception:
            pass
        conn.autocommit = False
        with conn.cursor() as cursor:
            _log({"event": "db_operation", "table": "trials", "operation": "SELECT"})
            cursor.execute(
                "SELECT id FROM trials WHERE trial_id = %s AND status = 'ACTIVE'",
                (payload.trialId,),
            )
            if cursor.fetchone() is None:
                conn.rollback()
                raise HTTPException(status_code=404, detail="TRIAL_NOT_FOUND")

            _log({"event": "db_operation", "table": "trial_enrolments", "operation": "SELECT"})
            cursor.execute(
                "SELECT id FROM trial_enrolments WHERE trial_id = %s AND patient_id = %s AND status = 'ENROLLED'",
                (payload.trialId, payload.patientId),
            )
            if cursor.fetchone() is None:
                conn.rollback()
                raise HTTPException(status_code=404, detail="PATIENT_NOT_FOUND")

            window_start = datetime.now(UTC) - timedelta(seconds=IDEMPOTENCY_WINDOW_S)
            _log({"event": "db_operation", "table": "adverse_events", "operation": "SELECT"})
            cursor.execute(
                "SELECT ae_id FROM adverse_events WHERE trial_id = %s AND patient_id = %s AND ae_term_code = %s AND ctcae_grade = %s AND submitted_at >= %s ORDER BY submitted_at DESC LIMIT 1",
                (payload.trialId, payload.patientId, payload.aeTermCode, payload.ctcaeGrade, window_start),
            )
            duplicate_row = cursor.fetchone()
            if duplicate_row is not None:
                conn.rollback()
                raise HTTPException(status_code=409, detail={"code": "DUPLICATE_AE", "aeId": duplicate_row[0]})

            _log({"event": "db_operation", "table": "adverse_events", "operation": "SELECT"})
            cursor.execute("SELECT 'AE-' || TO_CHAR(NOW(),'YYYY') || '-' || LPAD(NEXTVAL('ae_id_seq')::text, 6, '0')")
            ae_row = cursor.fetchone()
            if ae_row is None:
                conn.rollback()
                raise HTTPException(status_code=500, detail="DB_ERROR")
            ae_id = ae_row[0]

            _log({"event": "db_operation", "table": "ae_notifications", "operation": "SELECT"})
            cursor.execute("SELECT 'NOTIF-' || TO_CHAR(NOW(),'YYYY') || '-' || LPAD(NEXTVAL('notif_id_seq')::text, 6, '0')")
            notif_row = cursor.fetchone()
            if notif_row is None:
                conn.rollback()
                raise HTTPException(status_code=500, detail="DB_ERROR")
            notification_id = notif_row[0]

            submitted_at = datetime.now(UTC)
            priority = "HIGH" if payload.ctcaeGrade >= 3 else "NORMAL"

            _log({"event": "db_operation", "table": "adverse_events", "operation": "INSERT"})
            cursor.execute(
                "INSERT INTO adverse_events (ae_id, trial_id, site_id, patient_id, clinician_id, event_date, ae_term_code, ae_term_name, ctcae_grade, serious, outcome, action_taken, narrative, related_drug_id, reported_by, submitted_at, created_at, updated_at) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, NOW(), NOW())",
                (
                    ae_id,
                    payload.trialId,
                    payload.siteId,
                    payload.patientId,
                    payload.clinicianId,
                    event_date,
                    payload.aeTermCode,
                    payload.aeTermName,
                    payload.ctcaeGrade,
                    serious,
                    outcome,
                    payload.actionTaken,
                    payload.narrative,
                    payload.relatedDrugId,
                    payload.reportedBy,
                    submitted_at,
                ),
            )

            _log({"event": "db_operation", "table": "ae_notifications", "operation": "INSERT"})
            cursor.execute(
                "INSERT INTO ae_notifications (notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, outcome, priority, acknowledged, sns_published, created_at, updated_at) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, NOW(), NOW())",
                (
                    notification_id,
                    ae_id,
                    payload.trialId,
                    payload.siteId,
                    payload.patientId,
                    payload.aeTermName,
                    payload.ctcaeGrade,
                    serious,
                    outcome,
                    priority,
                    False,
                    False,
                ),
            )

            conn.commit()
    except HTTPException:
        raise
    except (IntegrityError, DatabaseError) as exc:
        if conn is not None:
            conn.rollback()
        _log_error({"event": "db_error", "error": str(exc)})
        raise HTTPException(status_code=500, detail="DB_ERROR") from exc
    except Exception as exc:
        if conn is not None:
            conn.rollback()
        _log_error({"event": "unexpected_error", "error": str(exc)})
        raise HTTPException(status_code=500, detail="DB_ERROR") from exc
    finally:
        if conn is not None:
            release_conn(conn)

    sns_published = False
    sns_message_id: str | None = None
    if SNS_TOPIC_ARN:
        try:
            publish_response = sns_client.publish(
                TopicArn=SNS_TOPIC_ARN,
                Message=json.dumps(
                    {
                        "aeId": ae_id,
                        "notificationId": notification_id,
                        "trialId": payload.trialId,
                        "siteId": payload.siteId,
                        "ctcaeGrade": payload.ctcaeGrade,
                        "serious": serious,
                        "outcome": outcome,
                    },
                    default=str,
                ),
            )
            sns_message_id = publish_response.get("MessageId")
            sns_published = True
            conn = None
            try:
                conn = get_conn()
                try:
                    conn.rollback()
                except Exception:
                    pass
                conn.autocommit = False
                with conn.cursor() as cursor:
                    _log({"event": "db_operation", "table": "ae_notifications", "operation": "UPDATE"})
                    cursor.execute(
                        "UPDATE ae_notifications SET sns_published = %s, sns_message_id = %s, updated_at = NOW() WHERE notification_id = %s",
                        (True, sns_message_id, notification_id),
                    )
                conn.commit()
            except Exception as exc:
                if conn is not None:
                    conn.rollback()
                _log_error({"event": "sns_message_id_update_failed", "ae_id": ae_id, "notification_id": notification_id, "error": str(exc)})
            finally:
                if conn is not None:
                    release_conn(conn)
        except Exception as exc:
            _log_error({"event": "sns_publish_failed", "ae_id": ae_id, "notification_id": notification_id, "error": str(exc)})
            sns_published = False
            sns_message_id = None

    received_at = datetime.now(UTC)
    return AdverseEventCreateResponse(
        status="success",
        aeId=ae_id,
        notificationId=notification_id,
        snsPublished=sns_published,
        snsMessageId=sns_message_id,
        receivedAt=received_at,
    )


def _validate_query_params(
    ctcaeGrade: int | None,
    priority: str | None,
    page: int,
    pageSize: int,
    dateFrom: str | None,
    dateTo: str | None,
) -> tuple[datetime | None, datetime | None]:
    if ctcaeGrade is not None and (ctcaeGrade < 1 or ctcaeGrade > 5):
        raise HTTPException(status_code=400, detail="INVALID_QUERY_PARAM")
    if priority is not None and priority not in ALLOWED_PRIORITIES:
        raise HTTPException(status_code=400, detail="INVALID_QUERY_PARAM")
    if page < 1 or pageSize < 1 or pageSize > 100:
        raise HTTPException(status_code=400, detail="INVALID_QUERY_PARAM")
    parsed_from = _parse_utc_datetime(dateFrom, "dateFrom") if dateFrom is not None else None
    parsed_to = _parse_utc_datetime(dateTo, "dateTo") if dateTo is not None else None
    return parsed_from, parsed_to


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
    _log({"event": "service_start", "operation": "get_notifications", "resource": "notification"})
    parsed_from, parsed_to = _validate_query_params(ctcaeGrade, priority, page, pageSize, dateFrom, dateTo)

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
    if parsed_from is not None:
        conditions.append("created_at >= %s")
        params.append(parsed_from)
    if parsed_to is not None:
        conditions.append("created_at <= %s")
        params.append(parsed_to)

    where_clause = " WHERE " + " AND ".join(conditions) if conditions else ""
    offset = (page - 1) * pageSize

    conn = None
    try:
        conn = get_conn()
        try:
            conn.rollback()
        except Exception:
            pass
        with conn.cursor() as cursor:
            _log({"event": "db_operation", "table": "ae_notifications", "operation": "SELECT"})
            cursor.execute(f"SELECT COUNT(*) FROM ae_notifications{where_clause}", tuple(params))
            total_row = cursor.fetchone()
            total = int(total_row[0]) if total_row is not None else 0

            _log({"event": "db_operation", "table": "ae_notifications", "operation": "SELECT"})
            cursor.execute(
                f"SELECT notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, outcome, priority, acknowledged, sns_published, created_at FROM ae_notifications{where_clause} ORDER BY created_at DESC LIMIT %s OFFSET %s",
                tuple(params + [pageSize, offset]),
            )
            rows = cursor.fetchall()
        notifications = [
            NotificationRecordResponse(
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
        return NotificationListResponse(status="success", total=total, page=page, pageSize=pageSize, notifications=notifications)
    except HTTPException:
        raise
    except Exception as exc:
        _log_error({"event": "db_error", "error": str(exc)})
        raise HTTPException(status_code=500, detail="DB_ERROR") from exc
    finally:
        if conn is not None:
            release_conn(conn)

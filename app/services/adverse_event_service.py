import json
import logging
import os
import secrets
from datetime import UTC, datetime
from time import sleep
from typing import Any

import boto3
import psycopg2
from dateutil import parser as date_parser
from fastapi import HTTPException

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


def _log(event: str, **kwargs: Any) -> None:
    logger.info(json.dumps({"event": event, **kwargs}))


def _error(event: str, **kwargs: Any) -> None:
    logger.error(json.dumps({"event": event, **kwargs}))


def _parse_utc(value: str | None, field_name: str) -> datetime | None:
    if value is None:
        return None
    try:
        parsed = date_parser.isoparse(value)
    except Exception as exc:
        _error("validation_failed", rule="invalid_datetime", field=field_name, error=str(exc))
        raise HTTPException(status_code=400, detail="Validation Error") from exc
    if parsed.tzinfo is None:
        _error("validation_failed", rule="naive_datetime", field=field_name)
        raise HTTPException(status_code=400, detail="Validation Error")
    return parsed.astimezone(UTC)


def _validate_create_request(payload: AdverseEventCreateRequest) -> None:
    if payload.ctcaeGrade < 1 or payload.ctcaeGrade > 5:
        _error("validation_failed", rule="invalid_ctcae_grade")
        raise HTTPException(status_code=400, detail="Validation Error")
    if payload.outcome not in ALLOWED_OUTCOMES:
        _error("validation_failed", rule="invalid_outcome")
        raise HTTPException(status_code=400, detail="Validation Error")
    if payload.actionTaken not in ALLOWED_ACTIONS:
        _error("validation_failed", rule="invalid_action_taken")
        raise HTTPException(status_code=400, detail="Validation Error")
    if len(payload.narrative) > 2000:
        _error("validation_failed", rule="narrative_too_long")
        raise HTTPException(status_code=400, detail="Validation Error")


def _coerce_payload(payload: AdverseEventCreateRequest) -> AdverseEventCreateRequest:
    serious = payload.serious or payload.ctcaeGrade >= 3
    outcome = "FATAL" if payload.ctcaeGrade == 5 else payload.outcome
    return payload.model_copy(update={"serious": serious, "outcome": outcome})


def _publish_sns(message: dict[str, Any]) -> tuple[bool, str | None]:
    """
    Safely publish to SNS if SNS_TOPIC_ARN is configured.
    Returns (published: bool, message_id: str|None).
    """
    topic = os.environ.get("SNS_TOPIC_ARN")
    if not topic:
        _log("sns_skipped", reason="no_topic_configured")
        return False, None

    try:
        region = os.environ.get("AWS_REGION")
        client = boto3.client("sns", region_name=region) if region else boto3.client("sns")
        resp = client.publish(TopicArn=topic, Message=json.dumps(message))
        message_id = resp.get("MessageId")
        _log("sns_published", notification_id=message.get("notificationId"), message_id=message_id)
        return True, message_id
    except Exception as exc:
        # Do not raise — just log failure and continue
        _error("sns_publish_failed", notification_id=message.get("notificationId"), error=str(exc))
        return False, None


def submit_adverse_event_service(payload: AdverseEventCreateRequest) -> AdverseEventCreateResponse:
    _log("service_start", operation="submit_adverse_event", resource="adverse-events")
    _validate_create_request(payload)
    payload = _coerce_payload(payload)

    conn = None
    try:
        conn = get_conn()
        conn.rollback()
        conn.autocommit = False
        with conn.cursor() as cursor:
            _log("db_operation", table="trials", operation="SELECT")
            cursor.execute(
                "SELECT id FROM trials WHERE trial_id = %s AND status = 'ACTIVE'",
                (payload.trialId,),
            )
            if cursor.fetchone() is None:
                raise HTTPException(status_code=404, detail="Resource Not Found")

            _log("db_operation", table="trial_enrolments", operation="SELECT")
            cursor.execute(
                "SELECT id FROM trial_enrolments WHERE trial_id = %s AND patient_id = %s AND status = 'ENROLLED'",
                (payload.trialId, payload.patientId),
            )
            if cursor.fetchone() is None:
                raise HTTPException(status_code=404, detail="Resource Not Found")

            window_s = int(os.environ.get("IDEMPOTENCY_WINDOW_S", "60"))
            _log("db_operation", table="adverse_events", operation="SELECT")
            cursor.execute(
                "SELECT ae_id FROM adverse_events WHERE trial_id = %s AND patient_id = %s AND ae_term_code = %s AND ctcae_grade = %s AND submitted_at >= NOW() - (%s || ' seconds')::interval ORDER BY submitted_at DESC LIMIT 1",
                (payload.trialId, payload.patientId, payload.aeTermCode, payload.ctcaeGrade, str(window_s)),
            )
            duplicate = cursor.fetchone()
            if duplicate is not None:
                raise HTTPException(status_code=409, detail="Resource Not Found")

            _log("db_operation", table="adverse_events", operation="INSERT")
            cursor.execute("SELECT 'AE-' || TO_CHAR(NOW(),'YYYY') || '-' || LPAD(NEXTVAL('ae_id_seq')::text, 6, '0')")
            ae_row = cursor.fetchone()
            if ae_row is None:
                conn.rollback()
                raise HTTPException(status_code=500, detail="Internal Error")
            ae_id = ae_row[0]

            _log("db_operation", table="ae_notifications", operation="INSERT")
            cursor.execute("SELECT 'NOTIF-' || TO_CHAR(NOW(),'YYYY') || '-' || LPAD(NEXTVAL('notif_id_seq')::text, 6, '0')")
            notif_row = cursor.fetchone()
            if notif_row is None:
                conn.rollback()
                raise HTTPException(status_code=500, detail="Internal Error")
            notification_id = notif_row[0]

            received_at = datetime.now(UTC)
            priority = "HIGH" if payload.ctcaeGrade >= 3 else "NORMAL"

            cursor.execute(
                "INSERT INTO adverse_events (ae_id, trial_id, site_id, patient_id, clinician_id, event_date, ae_term_code, ae_term_name, ctcae_grade, serious, outcome, action_taken, narrative, related_drug_id, reported_by, submitted_at, created_at, updated_at) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, NOW(), NOW())",
                (
                    ae_id,
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
                    payload.relatedDrugId,
                    payload.reportedBy,
                    received_at,
                ),
            )

            cursor.execute(
                "INSERT INTO ae_notifications (notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, outcome, priority, acknowledged, sns_published, created_at, updated_at) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, FALSE, FALSE, NOW(), NOW())",
                (
                    notification_id,
                    ae_id,
                    payload.trialId,
                    payload.siteId,
                    payload.patientId,
                    payload.aeTermName,
                    payload.ctcaeGrade,
                    payload.serious,
                    payload.outcome,
                    priority,
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
        _error("database_error", error=str(exc))
        raise HTTPException(status_code=503, detail="Database Error") from exc
    except Exception as exc:
        if conn is not None:
            conn.rollback()
        _error("unexpected_error", error=str(exc))
        raise HTTPException(status_code=500, detail="Internal Error") from exc
    finally:
        if conn is not None:
            release_conn(conn)

    # Attempt to publish to SNS only if configured. Prevent KeyError when SNS_TOPIC_ARN is missing.
    message_payload = {
        "notificationId": notification_id,
        "aeId": ae_id,
        "trialId": payload.trialId,
        "siteId": payload.siteId,
        "patientId": payload.patientId,
        "aeTermName": payload.aeTermName,
        "ctcaeGrade": payload.ctcaeGrade,
        "serious": payload.serious,
        "outcome": payload.outcome,
        "priority": priority,
        "receivedAt": received_at.isoformat(),
    }

    sns_published, sns_message_id = _publish_sns(message_payload)

    # Update ae_notifications.sns_published if we successfully published
    if sns_published:
        try:
            conn = get_conn()
            with conn.cursor() as cursor:
                cursor.execute(
                    "UPDATE ae_notifications SET sns_published = TRUE, updated_at = NOW() WHERE notification_id = %s",
                    (notification_id,),
                )
                conn.commit()
        except Exception:
            # Non-fatal: log and continue
            _error("sns_update_db_failed", notification_id=notification_id)
        finally:
            if conn is not None:
                release_conn(conn)

    return AdverseEventCreateResponse(
        status="success",
        aeId=ae_id,
        notificationId=notification_id,
        snsPublished=sns_published,
        snsMessageId=sns_message_id,
        receivedAt=received_at,
    )


def get_notifications_service(
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
    _log("service_start", operation="get_notifications", resource="adverse-events")
    if page < 1 or pageSize < 1 or pageSize > 100:
        _error("validation_failed", rule="invalid_pagination")
        raise HTTPException(status_code=400, detail="Validation Error")
    if priority is not None and priority not in ALLOWED_PRIORITIES:
        _error("validation_failed", rule="invalid_priority")
        raise HTTPException(status_code=400, detail="Validation Error")

    date_from = _parse_utc(dateFrom, "dateFrom")
    date_to = _parse_utc(dateTo, "dateTo")

    conditions: list[str] = []
    params: list[Any] = []
    if trialId is not None:
        conditions.append("trial_id = %s")
        params.append(trialId)
    if siteId is not None:
        conditions.append("site_id = %s")
        params.append(siteId)
    if ctcaeGrade is not None:
        if ctcaeGrade < 1 or ctcaeGrade > 5:
            raise HTTPException(status_code=400, detail="Validation Error")
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
    if date_from is not None:
        conditions.append("created_at >= %s")
        params.append(date_from)
    if date_to is not None:
        conditions.append("created_at <= %s")
        params.append(date_to)

    where_clause = " WHERE " + " AND ".join(conditions) if conditions else ""
    conn = None
    try:
        conn = get_conn()
        conn.rollback()
        with conn.cursor() as cursor:
            _log("db_operation", table="ae_notifications", operation="SELECT")
            cursor.execute(f"SELECT COUNT(*) FROM ae_notifications{where_clause}", tuple(params))
            total_row = cursor.fetchone()
            total = int(total_row[0]) if total_row is not None else 0

            offset = (page - 1) * pageSize
            _log("db_operation", table="ae_notifications", operation="SELECT")
            cursor.execute(
                f"SELECT notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, outcome, priority, acknowledged, sns_published, created_at FROM ae_notifications{where_clause} ORDER BY created_at DESC LIMIT %s OFFSET %s",
                tuple(params + [pageSize, offset]),
            )
            rows = cursor.fetchall()
            notifications = [
                NotificationRecord(
                    notificationId=row[0],
                    aeId=row[1],
                    trialId=row[2],
                    siteId=row[3],
                    patientId=row[4],
                    aeTermName=row[5],
                    ctcaeGrade=row[6],
                    serious=row[7],
                    outcome=row[8],
                    priority=row[9],
                    acknowledged=row[10],
                    snsPublished=row[11],
                    createdAt=row[12],
                )
                for row in rows
            ]
    except psycopg2.Error as exc:
        _error("database_error", error=str(exc))
        raise HTTPException(status_code=503, detail="Database Error") from exc
    except Exception as exc:
        _error("unexpected_error", error=str(exc))
        raise HTTPException(status_code=500, detail="Internal Error") from exc
    finally:
        if conn is not None:
            release_conn(conn)

    return NotificationListResponse(status="success", total=total, page=page, pageSize=pageSize, notifications=notifications)

import json
import logging
import os
import secrets
from datetime import UTC, datetime
from typing import Any

import boto3
from dateutil import parser as date_parser
from fastapi import HTTPException
from psycopg2 import DatabaseError, IntegrityError

from app.db.connection import get_conn, release_conn
from app.models.ae_model import NotificationRecord
from app.schemas.ae_schema import AdverseEventCreateRequest, AdverseEventCreateResponse, NotificationListResponse

logger = logging.getLogger(__name__)
logger.setLevel(os.environ.get("LOG_LEVEL", "INFO"))
_sns_client = boto3.client("sns", region_name=os.environ.get("AWS_REGION", "eu-west-2"))


def _log(event: str, **kwargs: Any) -> None:
    logger.info(json.dumps({"event": event, **kwargs}, default=str))


def _log_error(event: str, **kwargs: Any) -> None:
    logger.error(json.dumps({"event": event, **kwargs}, default=str))


def _parse_utc_datetime(value: str) -> datetime:
    parsed = date_parser.isoparse(value)
    if parsed.tzinfo is None:
        raise HTTPException(status_code=422, detail={"code": "INVALID_QUERY_PARAM", "message": "date value must be timezone-aware"})
    return parsed.astimezone(UTC)


def _validate_and_coerce_request(payload: AdverseEventCreateRequest) -> AdverseEventCreateRequest:
    if payload.ctcaeGrade >= 3:
        payload.serious = True
    if payload.ctcaeGrade == 5:
        payload.outcome = "FATAL"
    return payload


def create_adverse_event(payload: AdverseEventCreateRequest) -> AdverseEventCreateResponse:
    _log("service_start", resource="adverse_event", operation="create")
    payload = _validate_and_coerce_request(payload)
    conn = None
    try:
        conn = get_conn()
        try:
            conn.rollback()
        except Exception:
            pass
        conn.autocommit = False
        with conn.cursor() as cursor:
            _log("db_operation", table="trials", operation="SELECT")
            cursor.execute("SELECT id FROM trials WHERE trial_id = %s AND status = 'ACTIVE'", (payload.trialId,))
            if cursor.fetchone() is None:
                raise HTTPException(status_code=404, detail={"code": "TRIAL_NOT_FOUND", "message": "trial not found"})

            _log("db_operation", table="trial_enrolments", operation="SELECT")
            cursor.execute(
                "SELECT id FROM trial_enrolments WHERE trial_id = %s AND patient_id = %s AND status = 'ENROLLED'",
                (payload.trialId, payload.patientId),
            )
            if cursor.fetchone() is None:
                raise HTTPException(status_code=404, detail={"code": "PATIENT_NOT_FOUND", "message": "patient not found"})

            window_s = int(os.environ.get("IDEMPOTENCY_WINDOW_S", "60"))
            _log("db_operation", table="adverse_events", operation="SELECT")
            cursor.execute(
                "SELECT ae_id FROM adverse_events WHERE trial_id = %s AND patient_id = %s AND ae_term_code = %s AND ctcae_grade = %s AND submitted_at >= NOW() - (%s || ' seconds')::interval ORDER BY submitted_at DESC LIMIT 1",
                (payload.trialId, payload.patientId, payload.aeTermCode, payload.ctcaeGrade, str(window_s)),
            )
            duplicate = cursor.fetchone()
            if duplicate is not None:
                raise HTTPException(status_code=409, detail={"code": "DUPLICATE_AE", "message": "duplicate adverse event", "aeId": duplicate[0]})

            _log("db_operation", table="ae_id_seq", operation="SELECT")
            cursor.execute("SELECT 'AE-' || TO_CHAR(NOW(),'YYYY') || '-' || LPAD(NEXTVAL('ae_id_seq')::text, 6, '0')")
            ae_row = cursor.fetchone()
            if ae_row is None:
                raise HTTPException(status_code=500, detail={"code": "DB_ERROR", "message": "failed to generate ae id"})
            ae_id = ae_row[0]

            _log("db_operation", table="notif_id_seq", operation="SELECT")
            cursor.execute("SELECT 'NOTIF-' || TO_CHAR(NOW(),'YYYY') || '-' || LPAD(NEXTVAL('notif_id_seq')::text, 6, '0')")
            notif_row = cursor.fetchone()
            if notif_row is None:
                raise HTTPException(status_code=500, detail={"code": "DB_ERROR", "message": "failed to generate notification id"})
            notification_id = notif_row[0]

            received_at = datetime.now(UTC)
            _log("db_operation", table="adverse_events", operation="INSERT")
            cursor.execute(
                "INSERT INTO adverse_events (ae_id, trial_id, site_id, patient_id, clinician_id, event_date, ae_term_code, ae_term_name, ctcae_grade, serious, outcome, action_taken, narrative, related_drug_id, reported_by, submitted_at, created_at, updated_at) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, NOW(), NOW())",
                (
                    ae_id,
                    payload.trialId,
                    payload.siteId,
                    payload.patientId,
                    payload.clinicianId,
                    _parse_utc_datetime(payload.eventDate),
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

            _log("db_operation", table="ae_notifications", operation="INSERT")
            priority = "HIGH" if payload.ctcaeGrade >= 3 else "NORMAL"
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
    except (IntegrityError, DatabaseError) as exc:
        if conn is not None:
            conn.rollback()
        _log_error("db_error", error=str(exc))
        raise HTTPException(status_code=500, detail={"code": "DB_ERROR", "message": "database operation failed"})
    except Exception as exc:
        if conn is not None:
            conn.rollback()
        _log_error("unexpected_error", error=str(exc))
        raise HTTPException(status_code=500, detail={"code": "DB_ERROR", "message": "database operation failed"})
    finally:
        if conn is not None:
            release_conn(conn)

    sns_published = False
    sns_message_id: str | None = None
    try:
        topic_arn = os.environ["SNS_TOPIC_ARN"]
        response = _sns_client.publish(
            TopicArn=topic_arn,
            Message=json.dumps({"aeId": ae_id, "notificationId": notification_id, "trialId": payload.trialId}),
        )
        sns_message_id = response.get("MessageId")
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
                _log("db_operation", table="ae_notifications", operation="UPDATE")
                cursor.execute(
                    "UPDATE ae_notifications SET sns_published = TRUE, sns_message_id = %s, updated_at = NOW() WHERE notification_id = %s",
                    (sns_message_id, notification_id),
                )
            conn.commit()
        except Exception as exc:
            if conn is not None:
                conn.rollback()
            _log_error("sns_message_id_update_failed", ae_id=ae_id, notification_id=notification_id, error=str(exc))
        finally:
            if conn is not None:
                release_conn(conn)
    except Exception as exc:
        _log_error("sns_publish_failed", ae_id=ae_id, notification_id=notification_id, error_class=exc.__class__.__name__, error=str(exc))
        sns_published = False
        sns_message_id = None

    return AdverseEventCreateResponse(
        status="success",
        aeId=ae_id,
        notificationId=notification_id,
        snsPublished=sns_published,
        snsMessageId=sns_message_id,
        receivedAt=received_at,
    )


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
    _log("service_start", resource="notification", operation="retrieve")
    if page < 1 or pageSize < 1 or pageSize > 100:
        raise HTTPException(status_code=400, detail={"code": "INVALID_QUERY_PARAM", "message": "invalid pagination"})
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
            raise HTTPException(status_code=400, detail={"code": "INVALID_QUERY_PARAM", "message": "invalid ctcaeGrade"})
        conditions.append("ctcae_grade = %s")
        params.append(ctcaeGrade)
    if serious is not None:
        conditions.append("serious = %s")
        params.append(serious)
    if acknowledged is not None:
        conditions.append("acknowledged = %s")
        params.append(acknowledged)
    if priority is not None:
        if priority not in {"HIGH", "NORMAL"}:
            raise HTTPException(status_code=400, detail={"code": "INVALID_QUERY_PARAM", "message": "invalid priority"})
        conditions.append("priority = %s")
        params.append(priority)
    if dateFrom is not None:
        conditions.append("created_at >= %s")
        params.append(_parse_utc_datetime(dateFrom))
    if dateTo is not None:
        conditions.append("created_at <= %s")
        params.append(_parse_utc_datetime(dateTo))
    where_clause = " WHERE " + " AND ".join(conditions) if conditions else ""
    offset = (page - 1) * pageSize
    conn = None
    try:
        conn = get_conn()
        try:
            conn.rollback()
        except Exception:
            pass
        conn.autocommit = True
        with conn.cursor() as cursor:
            _log("db_operation", table="ae_notifications", operation="SELECT")
            cursor.execute(f"SELECT COUNT(*) FROM ae_notifications{where_clause}", tuple(params))
            total_row = cursor.fetchone()
            total = int(total_row[0]) if total_row is not None else 0
            cursor.execute(
                f"SELECT notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, outcome, priority, acknowledged, sns_published, created_at FROM ae_notifications{where_clause} ORDER BY created_at DESC LIMIT %s OFFSET %s",
                tuple(params + [pageSize, offset]),
            )
            rows = cursor.fetchall()
    except Exception as exc:
        _log_error("db_error", error=str(exc))
        raise HTTPException(status_code=500, detail={"code": "DB_ERROR", "message": "database query failed"})
    finally:
        if conn is not None:
            release_conn(conn)
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
    return NotificationListResponse(status="success", total=total, page=page, pageSize=pageSize, notifications=notifications)

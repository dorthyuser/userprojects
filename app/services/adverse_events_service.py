import json
import logging
import os
import time
from datetime import UTC, datetime
from typing import Any

import boto3
import psycopg2
from fastapi import HTTPException, status
from psycopg2 import Error as Psycopg2Error
from psycopg2.extras import RealDictCursor

from app.db.connection import get_conn, release_conn
from app.models.adverse_events_model import NotificationRecord
from app.schemas.adverse_events_schema import AdverseEventCreateRequest, AdverseEventCreateResponse, NotificationListResponse, NotificationResponseItem

logger = logging.getLogger(__name__)
sns_client = boto3.client("sns", region_name=os.environ.get("AWS_REGION", "eu-west-2"))


def _utc_now_iso() -> str:
    return datetime.now(UTC).isoformat().replace("+00:00", "Z")


def _validate_required(payload: AdverseEventCreateRequest) -> None:
    missing = []
    for field_name in ["trialId", "siteId", "patientId", "clinicianId", "eventDate", "aeTermCode", "aeTermName", "ctcaeGrade", "serious", "outcome", "actionTaken", "narrative", "reportedBy"]:
        if getattr(payload, field_name) is None:
            missing.append(field_name)
    if missing:
        logger.error(json.dumps({"event": "validation_failure", "rule": "MISSING_REQUIRED_FIELD"}))
        raise HTTPException(status_code=status.HTTP_422_UNPROCESSABLE_ENTITY, detail="Validation Error")


def _coerce_payload(payload: AdverseEventCreateRequest) -> AdverseEventCreateRequest:
    serious = True if payload.ctcaeGrade >= 3 else payload.serious
    outcome = "FATAL" if payload.ctcaeGrade == 5 else payload.outcome
    return payload.model_copy(update={"serious": serious, "outcome": outcome})


def _get_connection():
    retries = 2
    last_error: Exception | None = None
    for attempt in range(retries + 1):
        try:
            conn = get_conn()
            conn.rollback()
            return conn
        except Exception as exc:
            last_error = exc
            logger.error(json.dumps({"event": "db_connection_failed", "message": str(exc)}))
            if attempt < retries:
                time.sleep(0.2)
    raise HTTPException(status_code=status.HTTP_500_INTERNAL_SERVER_ERROR, detail="Internal Error") from last_error


def create_adverse_event(payload: AdverseEventCreateRequest) -> AdverseEventCreateResponse:
    logger.info(json.dumps({"event": "service_start", "operation": "create_adverse_event", "resource": "adverse_events"}))
    _validate_required(payload)
    payload = _coerce_payload(payload)
    conn = _get_connection()
    try:
        with conn.cursor(cursor_factory=RealDictCursor) as cursor:
            logger.info(json.dumps({"event": "db_operation", "table": "trials", "operation": "SELECT"}))
            cursor.execute("SELECT id FROM trials WHERE trial_id = %s AND status = 'ACTIVE'", (payload.trialId,))
            if cursor.fetchone() is None:
                raise HTTPException(status_code=status.HTTP_502_BAD_GATEWAY, detail="Service Unavailable")

            logger.info(json.dumps({"event": "db_operation", "table": "trial_enrolments", "operation": "SELECT"}))
            cursor.execute("SELECT id FROM trial_enrolments WHERE trial_id = %s AND patient_id = %s AND status = 'ENROLLED'", (payload.trialId, payload.patientId))
            if cursor.fetchone() is None:
                raise HTTPException(status_code=status.HTTP_502_BAD_GATEWAY, detail="Service Unavailable")

            logger.info(json.dumps({"event": "db_operation", "table": "adverse_events", "operation": "SELECT"}))
            window_s = int(os.environ.get("IDEMPOTENCY_WINDOW_S", "60"))
            cursor.execute("SELECT ae_id FROM adverse_events WHERE trial_id = %s AND patient_id = %s AND ae_term_code = %s AND ctcae_grade = %s AND submitted_at >= NOW() - (%s || ' seconds')::interval ORDER BY submitted_at DESC LIMIT 1", (payload.trialId, payload.patientId, payload.aeTermCode, payload.ctcaeGrade, str(window_s)))
            duplicate = cursor.fetchone()
            if duplicate is not None:
                raise HTTPException(status_code=status.HTTP_409_CONFLICT, detail="Validation Error")

            logger.info(json.dumps({"event": "db_operation", "table": "ae_id_seq", "operation": "SELECT"}))
            cursor.execute("SELECT 'AE-' || TO_CHAR(NOW(),'YYYY') || '-' || LPAD(NEXTVAL('ae_id_seq')::text, 6, '0')")
            ae_row = cursor.fetchone()
            if ae_row is None:
                conn.rollback()
                raise HTTPException(status_code=status.HTTP_500_INTERNAL_SERVER_ERROR, detail="Internal Error")
            ae_id = ae_row[0]

            logger.info(json.dumps({"event": "db_operation", "table": "notif_id_seq", "operation": "SELECT"}))
            cursor.execute("SELECT 'NOTIF-' || TO_CHAR(NOW(),'YYYY') || '-' || LPAD(NEXTVAL('notif_id_seq')::text, 6, '0')")
            notif_row = cursor.fetchone()
            if notif_row is None:
                conn.rollback()
                raise HTTPException(status_code=status.HTTP_500_INTERNAL_SERVER_ERROR, detail="Internal Error")
            notification_id = notif_row[0]

            conn.autocommit = False
            logger.info(json.dumps({"event": "db_operation", "table": "adverse_events", "operation": "INSERT"}))
            cursor.execute(
                "INSERT INTO adverse_events (ae_id, trial_id, site_id, patient_id, clinician_id, event_date, ae_term_code, ae_term_name, ctcae_grade, serious, outcome, action_taken, narrative, related_drug_id, reported_by, submitted_at, created_at, updated_at) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, NOW(), NOW(), NOW())",
                (ae_id, payload.trialId, payload.siteId, payload.patientId, payload.clinicianId, payload.eventDate, payload.aeTermCode, payload.aeTermName, payload.ctcaeGrade, payload.serious, payload.outcome, payload.actionTaken, payload.narrative, payload.relatedDrugId, payload.reportedBy),
            )
            logger.info(json.dumps({"event": "db_operation", "table": "ae_notifications", "operation": "INSERT"}))
            priority = "HIGH" if payload.ctcaeGrade >= 3 else "NORMAL"
            cursor.execute(
                "INSERT INTO ae_notifications (notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, outcome, priority, acknowledged, sns_published, created_at, updated_at) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, FALSE, FALSE, NOW(), NOW())",
                (notification_id, ae_id, payload.trialId, payload.siteId, payload.patientId, payload.aeTermName, payload.ctcaeGrade, payload.serious, payload.outcome, priority),
            )
            conn.commit()
    except HTTPException:
        conn.rollback()
        raise
    except Psycopg2Error as exc:
        conn.rollback()
        logger.error(json.dumps({"event": "db_error", "message": str(exc)}), exc_info=True)
        raise HTTPException(status_code=status.HTTP_503_SERVICE_UNAVAILABLE, detail="Database Error") from exc
    except Exception as exc:
        conn.rollback()
        logger.error(json.dumps({"event": "unexpected_error", "message": str(exc)}), exc_info=True)
        raise HTTPException(status_code=status.HTTP_500_INTERNAL_SERVER_ERROR, detail="Internal Error") from exc
    finally:
        release_conn(conn)

    sns_published = False
    sns_message_id = None
    try:
        response = sns_client.publish(TopicArn=os.environ["SNS_TOPIC_ARN"], Message=json.dumps({"aeId": ae_id, "notificationId": notification_id}))
        sns_message_id = response.get("MessageId")
        sns_published = True
        conn = _get_connection()
        try:
            with conn.cursor() as cursor:
                logger.info(json.dumps({"event": "db_operation", "table": "ae_notifications", "operation": "UPDATE"}))
                cursor.execute("UPDATE ae_notifications SET sns_published = TRUE, sns_message_id = %s WHERE notification_id = %s", (sns_message_id, notification_id))
                conn.commit()
        except Exception as exc:
            conn.rollback()
            logger.warning(json.dumps({"event": "sns_message_update_failed", "ae_id": ae_id, "notification_id": notification_id, "message": str(exc)}))
        finally:
            release_conn(conn)
    except Exception as exc:
        logger.error(json.dumps({"event": "sns_publish_failed", "ae_id": ae_id, "notification_id": notification_id, "exception": exc.__class__.__name__, "message": str(exc)}))
        sns_published = False
        sns_message_id = None

    return AdverseEventCreateResponse(status="success", aeId=ae_id, notificationId=notification_id, snsPublished=sns_published, snsMessageId=sns_message_id, receivedAt=_utc_now_iso())


def _parse_bool(value: str | None, field_name: str) -> bool | None:
    if value is None:
        return None
    lowered = value.lower()
    if lowered in {"true", "1", "yes"}:
        return True
    if lowered in {"false", "0", "no"}:
        return False
    logger.error(json.dumps({"event": "validation_failure", "rule": field_name}))
    raise HTTPException(status_code=status.HTTP_502_BAD_GATEWAY, detail="Validation Error")


def get_notifications(trialId: str | None, siteId: str | None, ctcaeGrade: int | None, serious: bool | None, acknowledged: bool | None, priority: str | None, dateFrom: str | None, dateTo: str | None, page: int, pageSize: int) -> NotificationListResponse:
    logger.info(json.dumps({"event": "service_start", "operation": "get_notifications", "resource": "ae_notifications"}))
    if page < 1 or pageSize < 1 or pageSize > 100:
        logger.error(json.dumps({"event": "validation_failure", "rule": "pagination"}))
        raise HTTPException(status_code=status.HTTP_502_BAD_GATEWAY, detail="Validation Error")
    if priority is not None and priority not in {"HIGH", "NORMAL"}:
        logger.error(json.dumps({"event": "validation_failure", "rule": "priority"}))
        raise HTTPException(status_code=status.HTTP_502_BAD_GATEWAY, detail="Validation Error")
    if ctcaeGrade is not None and (ctcaeGrade < 1 or ctcaeGrade > 5):
        logger.error(json.dumps({"event": "validation_failure", "rule": "ctcaeGrade"}))
        raise HTTPException(status_code=status.HTTP_502_BAD_GATEWAY, detail="Validation Error")
    conn = _get_connection()
    try:
        conditions = []
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
        if dateFrom is not None:
            conditions.append("created_at >= %s")
            params.append(dateFrom)
        if dateTo is not None:
            conditions.append("created_at <= %s")
            params.append(dateTo)
        where_clause = " WHERE " + " AND ".join(conditions) if conditions else ""
        with conn.cursor(cursor_factory=RealDictCursor) as cursor:
            logger.info(json.dumps({"event": "db_operation", "table": "ae_notifications", "operation": "SELECT"}))
            cursor.execute(f"SELECT COUNT(*) AS total FROM ae_notifications{where_clause}", tuple(params))
            total_row = cursor.fetchone()
            total = int(total_row["total"]) if total_row is not None else 0
            offset = (page - 1) * pageSize
            cursor.execute(f"SELECT notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, outcome, priority, acknowledged, sns_published, created_at FROM ae_notifications{where_clause} ORDER BY created_at DESC LIMIT %s OFFSET %s", tuple(params + [pageSize, offset]))
            rows = cursor.fetchall()
            notifications = [NotificationResponseItem(notificationId=row["notification_id"], aeId=row["ae_id"], trialId=row["trial_id"], siteId=row["site_id"], patientId=row["patient_id"], aeTermName=row["ae_term_name"], ctcaeGrade=row["ctcae_grade"], serious=row["serious"], priority=row["priority"], outcome=row["outcome"], acknowledged=row["acknowledged"], snsPublished=row["sns_published"], createdAt=row["created_at"].isoformat().replace("+00:00", "Z")) for row in rows]
            return NotificationListResponse(status="success", total=total, page=page, pageSize=pageSize, notifications=notifications)
    except Psycopg2Error as exc:
        logger.error(json.dumps({"event": "db_error", "message": str(exc)}), exc_info=True)
        raise HTTPException(status_code=status.HTTP_503_SERVICE_UNAVAILABLE, detail="Database Error") from exc
    except Exception as exc:
        logger.error(json.dumps({"event": "unexpected_error", "message": str(exc)}), exc_info=True)
        raise HTTPException(status_code=status.HTTP_500_INTERNAL_SERVER_ERROR, detail="Internal Error") from exc
    finally:
        release_conn(conn)

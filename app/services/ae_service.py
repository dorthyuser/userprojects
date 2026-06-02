import json
import logging
import os
import time
from datetime import datetime, timezone
from typing import Any

import boto3
from dateutil import parser as date_parser
from fastapi import HTTPException, status
from psycopg2 import DatabaseError, Error

from app.db.connection import get_conn, release_conn
from app.models.ae_model import NotificationRecord
from app.schemas.ae_schema import AdverseEventCreateRequest, AdverseEventCreateResponse, NotificationListResponse, NotificationResponse

logger = logging.getLogger(__name__)
sns_client = boto3.client("sns", region_name=os.environ.get("AWS_REGION", "eu-west-2"))
SNS_TOPIC_ARN = os.environ.get("SNS_TOPIC_ARN")
IDEMPOTENCY_WINDOW_S = int(os.environ.get("IDEMPOTENCY_WINDOW_S", "60"))


def _utc_now() -> datetime:
    return datetime.now(timezone.utc)


def _parse_dt(value: str | None, field_name: str) -> datetime | None:
    if value is None:
        return None
    try:
        dt = date_parser.isoparse(value)
        if dt.tzinfo is None:
            raise ValueError
        return dt.astimezone(timezone.utc)
    except Exception as exc:
        logger.error(json.dumps({"event": "validation_failure", "rule": field_name, "error": str(exc)}))
        raise HTTPException(status_code=status.HTTP_400_BAD_REQUEST, detail={"code": "INVALID_QUERY_PARAM", "message": f"Invalid {field_name}"})


def _coerce_payload(payload: AdverseEventCreateRequest) -> AdverseEventCreateRequest:
    serious = payload.serious or payload.ctcaeGrade >= 3
    outcome = "FATAL" if payload.ctcaeGrade == 5 else payload.outcome
    return payload.model_copy(update={"serious": serious, "outcome": outcome})


def _validate_required(payload: AdverseEventCreateRequest) -> None:
    missing = []
    for field_name in ["trialId", "siteId", "patientId", "clinicianId", "eventDate", "aeTermCode", "aeTermName", "ctcaeGrade", "serious", "outcome", "actionTaken", "narrative", "reportedBy"]:
        if getattr(payload, field_name) is None:
            missing.append(field_name)
    if missing:
        logger.error(json.dumps({"event": "validation_failure", "rule": "MISSING_REQUIRED_FIELD"}))
        raise HTTPException(status_code=status.HTTP_400_BAD_REQUEST, detail={"code": "MISSING_REQUIRED_FIELD", "message": "One or more required fields absent"})


def create_adverse_event(payload: AdverseEventCreateRequest) -> AdverseEventCreateResponse:
    logger.info(json.dumps({"event": "service_entry", "operation": "create", "resource": "adverse_event"}))
    _validate_required(payload)
    coerced = _coerce_payload(payload)
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
            cursor.execute("SELECT id FROM trials WHERE trial_id = %s AND status = 'ACTIVE'", (coerced.trialId,))
            if cursor.fetchone() is None:
                raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail={"code": "TRIAL_NOT_FOUND", "message": "trialId does not exist or is not active"})
            logger.info(json.dumps({"event": "db_operation", "table": "trial_enrolments", "operation": "SELECT"}))
            cursor.execute("SELECT id FROM trial_enrolments WHERE trial_id = %s AND patient_id = %s AND status = 'ENROLLED'", (coerced.trialId, coerced.patientId))
            if cursor.fetchone() is None:
                raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail={"code": "PATIENT_NOT_FOUND", "message": "patientId not enrolled in the specified trial"})
            logger.info(json.dumps({"event": "db_operation", "table": "adverse_events", "operation": "SELECT"}))
            cursor.execute("SELECT ae_id FROM adverse_events WHERE trial_id = %s AND patient_id = %s AND ae_term_code = %s AND ctcae_grade = %s AND submitted_at >= NOW() - (%s || ' seconds')::interval ORDER BY submitted_at DESC LIMIT 1", (coerced.trialId, coerced.patientId, coerced.aeTermCode, coerced.ctcaeGrade, IDEMPOTENCY_WINDOW_S))
            existing = cursor.fetchone()
            if existing is not None:
                raise HTTPException(status_code=status.HTTP_409_CONFLICT, detail={"code": "DUPLICATE_AE", "message": "Identical AE submitted within 60 seconds"})
            logger.info(json.dumps({"event": "db_operation", "table": "ae_id_seq", "operation": "SELECT"}))
            cursor.execute("SELECT 'AE-' || TO_CHAR(NOW(),'YYYY') || '-' || LPAD(NEXTVAL('ae_id_seq')::text, 6, '0')")
            ae_row = cursor.fetchone()
            if ae_row is None:
                raise HTTPException(status_code=status.HTTP_500_INTERNAL_SERVER_ERROR, detail={"code": "DB_ERROR", "message": "Database error"})
            ae_id = ae_row[0]
            logger.info(json.dumps({"event": "db_operation", "table": "notif_id_seq", "operation": "SELECT"}))
            cursor.execute("SELECT 'NOTIF-' || TO_CHAR(NOW(),'YYYY') || '-' || LPAD(NEXTVAL('notif_id_seq')::text, 6, '0')")
            notif_row = cursor.fetchone()
            if notif_row is None:
                raise HTTPException(status_code=status.HTTP_500_INTERNAL_SERVER_ERROR, detail={"code": "DB_ERROR", "message": "Database error"})
            notification_id = notif_row[0]
            received_at = _utc_now()
            logger.info(json.dumps({"event": "db_operation", "table": "adverse_events", "operation": "INSERT"}))
            cursor.execute("INSERT INTO adverse_events (ae_id, trial_id, site_id, patient_id, clinician_id, event_date, ae_term_code, ae_term_name, ctcae_grade, serious, outcome, action_taken, narrative, related_drug_id, reported_by, submitted_at, created_at, updated_at) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s)", (ae_id, coerced.trialId, coerced.siteId, coerced.patientId, coerced.clinicianId, coerced.eventDate, coerced.aeTermCode, coerced.aeTermName, coerced.ctcaeGrade, coerced.serious, coerced.outcome, coerced.actionTaken, coerced.narrative, coerced.relatedDrugId, coerced.reportedBy, received_at, received_at, received_at))
            logger.info(json.dumps({"event": "db_operation", "table": "ae_notifications", "operation": "INSERT"}))
            priority = "HIGH" if coerced.ctcaeGrade >= 3 else "NORMAL"
            cursor.execute("INSERT INTO ae_notifications (notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, outcome, priority, acknowledged, sns_published, created_at, updated_at) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s)", (notification_id, ae_id, coerced.trialId, coerced.siteId, coerced.patientId, coerced.aeTermName, coerced.ctcaeGrade, coerced.serious, coerced.outcome, priority, False, False, received_at, received_at))
            conn.commit()
    except HTTPException:
        if conn is not None:
            conn.rollback()
        raise
    except (DatabaseError, Error) as exc:
        if conn is not None:
            conn.rollback()
        logger.error(json.dumps({"event": "db_error", "error": str(exc)}))
        raise HTTPException(status_code=status.HTTP_500_INTERNAL_SERVER_ERROR, detail={"code": "DB_ERROR", "message": "Database error"})
    finally:
        if conn is not None:
            release_conn(conn)
    sns_published = False
    sns_message_id = None
    try:
        if SNS_TOPIC_ARN:
            response = sns_client.publish(TopicArn=SNS_TOPIC_ARN, Message=json.dumps({"aeId": ae_id, "notificationId": notification_id}))
            sns_message_id = response.get("MessageId")
            sns_published = True
            conn = get_conn()
            try:
                conn.rollback()
            except Exception:
                pass
            conn.autocommit = False
            with conn.cursor() as cursor:
                logger.info(json.dumps({"event": "db_operation", "table": "ae_notifications", "operation": "UPDATE"}))
                cursor.execute("UPDATE ae_notifications SET sns_published = %s, sns_message_id = %s WHERE notification_id = %s", (True, sns_message_id, notification_id))
                conn.commit()
    except Exception as exc:
        logger.error(json.dumps({"event": "sns_publish_failed", "ae_id": ae_id, "notification_id": notification_id, "error": str(exc), "exception_class": exc.__class__.__name__}))
        sns_published = False
        sns_message_id = None
    finally:
        if 'conn' in locals() and conn is not None:
            try:
                release_conn(conn)
            except Exception:
                pass
    return AdverseEventCreateResponse(status="success", aeId=ae_id, notificationId=notification_id, snsPublished=sns_published, snsMessageId=sns_message_id, receivedAt=received_at)


def get_notifications(trialId: str | None, siteId: str | None, ctcaeGrade: int | None, serious: bool | None, acknowledged: bool | None, priority: str | None, dateFrom: str | None, dateTo: str | None, page: int, pageSize: int) -> NotificationListResponse:
    logger.info(json.dumps({"event": "service_entry", "operation": "retrieve", "resource": "notifications"}))
    if page < 1 or pageSize < 1 or pageSize > 100:
        raise HTTPException(status_code=status.HTTP_400_BAD_REQUEST, detail={"code": "INVALID_QUERY_PARAM", "message": "Invalid pagination"})
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
            raise HTTPException(status_code=status.HTTP_400_BAD_REQUEST, detail={"code": "INVALID_QUERY_PARAM", "message": "Invalid ctcaeGrade"})
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
            raise HTTPException(status_code=status.HTTP_400_BAD_REQUEST, detail={"code": "INVALID_QUERY_PARAM", "message": "Invalid priority"})
        conditions.append("priority = %s")
        params.append(priority)
    date_from_dt = _parse_dt(dateFrom, "dateFrom")
    date_to_dt = _parse_dt(dateTo, "dateTo")
    if date_from_dt is not None:
        conditions.append("created_at >= %s")
        params.append(date_from_dt)
    if date_to_dt is not None:
        conditions.append("created_at <= %s")
        params.append(date_to_dt)
    where_clause = " WHERE " + " AND ".join(conditions) if conditions else ""
    conn = None
    try:
        conn = get_conn()
        try:
            conn.rollback()
        except Exception:
            pass
        with conn.cursor() as cursor:
            logger.info(json.dumps({"event": "db_operation", "table": "ae_notifications", "operation": "SELECT"}))
            cursor.execute(f"SELECT COUNT(*) FROM ae_notifications{where_clause}", tuple(params))
            total = cursor.fetchone()[0]
            offset = (page - 1) * pageSize
            cursor.execute(f"SELECT notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, outcome, priority, acknowledged, sns_published, created_at FROM ae_notifications{where_clause} ORDER BY created_at DESC LIMIT %s OFFSET %s", tuple(params + [pageSize, offset]))
            rows = cursor.fetchall()
            notifications = [NotificationResponse(notificationId=row[0], aeId=row[1], trialId=row[2], siteId=row[3], patientId=row[4], aeTermName=row[5], ctcaeGrade=row[6], serious=row[7], outcome=row[8], priority=row[9], acknowledged=row[10], snsPublished=row[11], createdAt=row[12]) for row in rows]
            return NotificationListResponse(status="success", total=total, page=page, pageSize=pageSize, notifications=notifications)
    except HTTPException:
        raise
    except Exception as exc:
        logger.error(json.dumps({"event": "db_error", "error": str(exc)}))
        raise HTTPException(status_code=status.HTTP_500_INTERNAL_SERVER_ERROR, detail={"code": "DB_ERROR", "message": "Database error"})
    finally:
        if conn is not None:
            release_conn(conn)

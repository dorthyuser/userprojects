import json
import logging
import os
from dataclasses import asdict
from typing import Any

import boto3
from dateutil.parser import isoparse
from fastapi import HTTPException, status

from app.db.connection import get_conn, release_conn
from app.models.ae_model import NotificationRow
from app.schemas.ae_schema import AECreateRequest, AECreateResponse, AENotificationsResponse, NotificationItem, NotificationQueryParams

logger = logging.getLogger(__name__)
logger.setLevel(os.environ.get("LOG_LEVEL", "INFO"))
_sns_client = boto3.client("sns", region_name=os.environ.get("AWS_REGION", "eu-west-2"))


def _runtime_error(message: str) -> RuntimeError:
    logger.error(message)
    return RuntimeError(message)


def _validate_env(name: str) -> str:
    value = os.environ.get(name)
    if not value:
        raise _runtime_error(f"Missing required environment variable: {name}")
    return value


def _parse_dt(value: str) -> datetime:
    dt = isoparse(value)
    if dt.tzinfo is None:
        raise HTTPException(status_code=422, detail={"code": "INVALID_QUERY_PARAM", "message": "dateTime must be timezone-aware"})
    return dt.astimezone(UTC)


def create_adverse_event(payload: AECreateRequest) -> AECreateResponse:
    logger.info(json.dumps({"event": "service_start", "operation": "create_adverse_event", "resource": "adverse_events"}))
    if payload.ctcaeGrade >= 3:
        payload.serious = True
    if payload.ctcaeGrade == 5:
        payload.outcome = "FATAL"
    window_s = int(_validate_env("IDEMPOTENCY_WINDOW_S"))
    conn = None
    try:
        conn = get_conn()
        conn.autocommit = False
    except Exception as exc:
        logger.error(json.dumps({"event": "db_connection_failed", "error": str(exc)}))
        raise RuntimeError("DB connection failed") from exc
    try:
        with conn.cursor() as cur:
            logger.info(json.dumps({"table": "trials", "operation": "SELECT"}))
            cur.execute("SELECT id FROM trials WHERE trial_id = %s AND status = 'ACTIVE'", (payload.trialId,))
            if cur.fetchone() is None:
                raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail={"code": "TRIAL_NOT_FOUND", "message": "trial not found"})
            logger.info(json.dumps({"table": "trial_enrolments", "operation": "SELECT"}))
            cur.execute("SELECT id FROM trial_enrolments WHERE trial_id = %s AND patient_id = %s AND status = 'ENROLLED'", (payload.trialId, payload.patientId))
            if cur.fetchone() is None:
                raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail={"code": "PATIENT_NOT_FOUND", "message": "patient not found"})
            logger.info(json.dumps({"table": "adverse_events", "operation": "SELECT"}))
            cur.execute("SELECT ae_id FROM adverse_events WHERE trial_id = %s AND patient_id = %s AND ae_term_code = %s AND ctcae_grade = %s AND submitted_at >= NOW() - (%s || ' seconds')::interval ORDER BY submitted_at DESC LIMIT 1", (payload.trialId, payload.patientId, payload.aeTermCode, payload.ctcaeGrade, str(window_s)))
            existing = cur.fetchone()
            if existing is not None:
                raise HTTPException(status_code=status.HTTP_409_CONFLICT, detail={"code": "DUPLICATE_AE", "message": "duplicate adverse event"})
            logger.info(json.dumps({"table": "adverse_events", "operation": "SELECT"}))
            cur.execute("SELECT 'AE-' || TO_CHAR(NOW(),'YYYY') || '-' || LPAD(NEXTVAL('ae_id_seq')::text, 6, '0')")
            ae_row = cur.fetchone()
            if ae_row is None:
                conn.rollback()
                raise HTTPException(status_code=500, detail={"code": "DB_ERROR", "message": "database error"})
            ae_id = ae_row[0]
            logger.info(json.dumps({"table": "ae_notifications", "operation": "SELECT"}))
            cur.execute("SELECT 'NOTIF-' || TO_CHAR(NOW(),'YYYY') || '-' || LPAD(NEXTVAL('notif_id_seq')::text, 6, '0')")
            notif_row = cur.fetchone()
            if notif_row is None:
                conn.rollback()
                raise HTTPException(status_code=500, detail={"code": "DB_ERROR", "message": "database error"})
            notification_id = notif_row[0]
            now = datetime.now(UTC)
            logger.info(json.dumps({"table": "adverse_events", "operation": "INSERT"}))
            cur.execute(
                "INSERT INTO adverse_events (ae_id, trial_id, site_id, patient_id, clinician_id, event_date, ae_term_code, ae_term_name, ctcae_grade, serious, outcome, action_taken, narrative, related_drug_id, reported_by, submitted_at, created_at, updated_at) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s)",
                (ae_id, payload.trialId, payload.siteId, payload.patientId, payload.clinicianId, payload.eventDate, payload.aeTermCode, payload.aeTermName, payload.ctcaeGrade, payload.serious, payload.outcome, payload.actionTaken, payload.narrative, payload.relatedDrugId, payload.reportedBy, now, now, now)
            )
            priority = "HIGH" if payload.ctcaeGrade >= 3 else "NORMAL"
            logger.info(json.dumps({"table": "ae_notifications", "operation": "INSERT"}))
            cur.execute(
                "INSERT INTO ae_notifications (notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, outcome, priority, acknowledged, sns_published, created_at, updated_at) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s)",
                (notification_id, ae_id, payload.trialId, payload.siteId, payload.patientId, payload.aeTermName, payload.ctcaeGrade, payload.serious, payload.outcome, priority, False, False, now, now)
            )
            conn.commit()
    except HTTPException:
        if conn is not None:
            conn.rollback()
        raise
    except Exception as exc:
        if conn is not None:
            conn.rollback()
        logger.error(json.dumps({"event": "db_error", "error": str(exc)}))
        raise RuntimeError("DB_ERROR") from exc
    finally:
        if conn is not None:
            release_conn(conn)
    sns_published = False
    sns_message_id = None
    try:
        topic_arn = _validate_env("SNS_TOPIC_ARN")
        response = _sns_client.publish(TopicArn=topic_arn, Message=json.dumps({"aeId": ae_id, "notificationId": notification_id}))
        sns_message_id = response.get("MessageId")
        sns_published = True
        conn = None
        try:
            conn = get_conn()
            conn.autocommit = False
            with conn.cursor() as cur:
                logger.info(json.dumps({"table": "ae_notifications", "operation": "UPDATE"}))
                cur.execute("UPDATE ae_notifications SET sns_published = %s, sns_message_id = %s WHERE notification_id = %s", (True, sns_message_id, notification_id))
                conn.commit()
        except Exception as exc:
            if conn is not None:
                conn.rollback()
            logger.warning(json.dumps({"event": "sns_message_update_failed", "ae_id": ae_id, "notification_id": notification_id, "error": str(exc)}))
        finally:
            if conn is not None:
                release_conn(conn)
    except Exception as exc:
        logger.error(json.dumps({"event": "sns_publish_failed", "ae_id": ae_id, "notification_id": notification_id, "error": str(exc)}))
        sns_published = False
    return AECreateResponse(status="success", aeId=ae_id, notificationId=notification_id, snsPublished=sns_published, snsMessageId=sns_message_id, receivedAt=now)


def list_notifications(params: NotificationQueryParams) -> AENotificationsResponse:
    logger.info(json.dumps({"event": "service_start", "operation": "list_notifications", "resource": "ae_notifications"}))
    clauses = []
    values: list[Any] = []
    if params.trialId is not None:
        clauses.append("trial_id = %s")
        values.append(params.trialId)
    if params.siteId is not None:
        clauses.append("site_id = %s")
        values.append(params.siteId)
    if params.ctcaeGrade is not None:
        if params.ctcaeGrade < 1 or params.ctcaeGrade > 5:
            raise HTTPException(status_code=400, detail={"code": "INVALID_QUERY_PARAM", "message": "invalid ctcaeGrade"})
        clauses.append("ctcae_grade = %s")
        values.append(params.ctcaeGrade)
    if params.serious is not None:
        clauses.append("serious = %s")
        values.append(params.serious)
    if params.acknowledged is not None:
        clauses.append("acknowledged = %s")
        values.append(params.acknowledged)
    if params.priority is not None:
        if params.priority not in {"HIGH", "NORMAL"}:
            raise HTTPException(status_code=400, detail={"code": "INVALID_QUERY_PARAM", "message": "invalid priority"})
        clauses.append("priority = %s")
        values.append(params.priority)
    if params.dateFrom is not None:
        clauses.append("created_at >= %s")
        values.append(_parse_dt(params.dateFrom))
    if params.dateTo is not None:
        clauses.append("created_at <= %s")
        values.append(_parse_dt(params.dateTo))
    where_sql = " WHERE " + " AND ".join(clauses) if clauses else ""
    limit = params.pageSize if params.pageSize <= 100 else 100
    offset = (params.page - 1) * limit
    conn = None
    try:
        conn = get_conn()
        conn.autocommit = False
        with conn.cursor() as cur:
            logger.info(json.dumps({"table": "ae_notifications", "operation": "SELECT"}))
            cur.execute(f"SELECT COUNT(*) FROM ae_notifications{where_sql}", tuple(values))
            total_row = cur.fetchone()
            total = int(total_row[0]) if total_row is not None else 0
            logger.info(json.dumps({"table": "ae_notifications", "operation": "SELECT"}))
            cur.execute(
                f"SELECT notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, outcome, priority, acknowledged, sns_published, created_at FROM ae_notifications{where_sql} ORDER BY created_at DESC LIMIT %s OFFSET %s",
                tuple(values + [limit, offset])
            )
            rows = cur.fetchall()
            notifications = [NotificationItem(notificationId=r[0], aeId=r[1], trialId=r[2], siteId=r[3], patientId=r[4], aeTermName=r[5], ctcaeGrade=r[6], serious=r[7], outcome=r[8], priority=r[9], acknowledged=r[10], snsPublished=r[11], createdAt=r[12]) for r in rows]
            return AENotificationsResponse(status="success", total=total, page=params.page, pageSize=limit, notifications=notifications)
    except HTTPException:
        raise
    except Exception as exc:
        logger.error(json.dumps({"event": "db_error", "error": str(exc)}))
        raise RuntimeError("DB_ERROR") from exc
    finally:
        if conn is not None:
            release_conn(conn)

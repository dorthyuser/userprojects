import json
import logging
import os
from datetime import UTC, datetime, timedelta
from typing import Any

import boto3
import psycopg2
from dateutil import parser as date_parser
from fastapi import HTTPException

from app.db.connection import get_conn, release_conn
from app.schemas.adverse_events_schema import (
    AdverseEventCreateRequest,
    AdverseEventCreateResponse,
    NotificationItem,
    NotificationListResponse,
)

logger = logging.getLogger(__name__)
sns_client = boto3.client("sns", region_name=os.environ.get("AWS_REGION", "eu-west-2"))
SNS_TOPIC_ARN = os.environ.get("SNS_TOPIC_ARN")
IDEMPOTENCY_WINDOW_S = int(os.environ.get("IDEMPOTENCY_WINDOW_S", "60"))


def _utc_now() -> datetime:
    return datetime.now(tz=UTC)


def _validate_query_params(
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
) -> tuple[datetime | None, datetime | None]:
    if ctcaeGrade is not None and ctcaeGrade not in {1, 2, 3, 4, 5}:
        logger.error(json.dumps({"event": "validation_failure", "rule": "INVALID_QUERY_PARAM"}))
        raise HTTPException(status_code=502, detail="Validation Error")
    if priority is not None and priority not in {"HIGH", "NORMAL"}:
        logger.error(json.dumps({"event": "validation_failure", "rule": "INVALID_QUERY_PARAM"}))
        raise HTTPException(status_code=502, detail="Validation Error")
    if page < 1 or pageSize < 1 or pageSize > 100:
        logger.error(json.dumps({"event": "validation_failure", "rule": "INVALID_QUERY_PARAM"}))
        raise HTTPException(status_code=502, detail="Validation Error")
    parsed_from = date_parser.isoparse(dateFrom) if dateFrom else None
    parsed_to = date_parser.isoparse(dateTo) if dateTo else None
    return parsed_from, parsed_to


def create_adverse_event(payload: AdverseEventCreateRequest) -> AdverseEventCreateResponse:
    logger.info(json.dumps({"event": "service_entry", "operation": "create", "resource": "adverse_events"}))
    conn = None
    try:
        payload = payload.coerce_business_rules()
        conn = get_conn()
        conn.rollback()
        conn.autocommit = False
        with conn.cursor() as cursor:
            logger.info(json.dumps({"event": "db_operation", "table": "trials", "operation": "SELECT"}))
            cursor.execute("SELECT id FROM trials WHERE trial_id = %s AND status = 'ACTIVE'", (payload.trialId,))
            if cursor.fetchone() is None:
                raise HTTPException(status_code=502, detail="Resource Not Found")
            logger.info(json.dumps({"event": "db_operation", "table": "trial_enrolments", "operation": "SELECT"}))
            cursor.execute(
                "SELECT id FROM trial_enrolments WHERE trial_id = %s AND patient_id = %s AND status = 'ENROLLED'",
                (payload.trialId, payload.patientId),
            )
            if cursor.fetchone() is None:
                raise HTTPException(status_code=502, detail="Resource Not Found")
            logger.info(json.dumps({"event": "db_operation", "table": "adverse_events", "operation": "SELECT"}))
            cursor.execute(
                "SELECT ae_id FROM adverse_events WHERE trial_id = %s AND patient_id = %s AND ae_term_code = %s AND ctcae_grade = %s AND submitted_at >= NOW() - (%s || ' seconds')::interval ORDER BY submitted_at DESC LIMIT 1",
                (payload.trialId, payload.patientId, payload.aeTermCode, payload.ctcaeGrade, IDEMPOTENCY_WINDOW_S),
            )
            duplicate = cursor.fetchone()
            if duplicate is not None:
                raise HTTPException(status_code=409, detail="Resource Not Found")
            logger.info(json.dumps({"event": "db_operation", "table": "ae_id_seq", "operation": "SELECT"}))
            cursor.execute("SELECT 'AE-' || TO_CHAR(NOW(),'YYYY') || '-' || LPAD(NEXTVAL('ae_id_seq')::text, 6, '0')")
            ae_row = cursor.fetchone()
            if ae_row is None:
                raise HTTPException(status_code=500, detail="Internal Error")
            ae_id = ae_row[0]
            logger.info(json.dumps({"event": "db_operation", "table": "notif_id_seq", "operation": "SELECT"}))
            cursor.execute("SELECT 'NOTIF-' || TO_CHAR(NOW(),'YYYY') || '-' || LPAD(NEXTVAL('notif_id_seq')::text, 6, '0')")
            notif_row = cursor.fetchone()
            if notif_row is None:
                raise HTTPException(status_code=500, detail="Internal Error")
            notification_id = notif_row[0]
            logger.info(json.dumps({"event": "db_operation", "table": "adverse_events", "operation": "INSERT"}))
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
                    _utc_now(),
                ),
            )
            logger.info(json.dumps({"event": "db_operation", "table": "ae_notifications", "operation": "INSERT"}))
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
                    payload.serious,
                    payload.outcome,
                    "HIGH" if payload.ctcaeGrade >= 3 else "NORMAL",
                    False,
                    False,
                ),
            )
        conn.commit()
        sns_published = False
        sns_message_id = None
        try:
            if SNS_TOPIC_ARN:
                response = sns_client.publish(
                    TopicArn=SNS_TOPIC_ARN,
                    Message=json.dumps({"aeId": ae_id, "notificationId": notification_id}),
                )
                sns_message_id = response.get("MessageId")
                sns_published = True
                try:
                    with conn.cursor() as cursor:
                        logger.info(json.dumps({"event": "db_operation", "table": "ae_notifications", "operation": "UPDATE"}))
                        cursor.execute(
                            "UPDATE ae_notifications SET sns_published = %s, sns_message_id = %s WHERE notification_id = %s",
                            (True, sns_message_id, notification_id),
                        )
                    conn.commit()
                except Exception as update_error:
                    logger.warning(json.dumps({"event": "sns_update_failed", "ae_id": ae_id, "notification_id": notification_id, "error": str(update_error)}))
        except Exception as sns_error:
            logger.error(json.dumps({"event": "sns_publish_failed", "ae_id": ae_id, "notification_id": notification_id, "error": str(sns_error), "exception": sns_error.__class__.__name__}))
            sns_published = False
            sns_message_id = None
        return AdverseEventCreateResponse(
            status="success",
            aeId=ae_id,
            notificationId=notification_id,
            snsPublished=sns_published,
            snsMessageId=sns_message_id,
            receivedAt=_utc_now(),
        )
    except HTTPException:
        if conn is not None:
            conn.rollback()
        raise
    except psycopg2.Error as exc:
        if conn is not None:
            conn.rollback()
        logger.error(json.dumps({"event": "db_error", "error": str(exc)}))
        raise HTTPException(status_code=500, detail="Database Error")
    except Exception as exc:
        if conn is not None:
            conn.rollback()
        logger.error(json.dumps({"event": "unexpected_error", "error": str(exc)}), exc_info=True)
        raise HTTPException(status_code=500, detail="Internal Error")
    finally:
        if conn is not None:
            release_conn(conn)


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
    logger.info(json.dumps({"event": "service_entry", "operation": "get", "resource": "ae_notifications"}))
    conn = None
    try:
        parsed_from, parsed_to = _validate_query_params(trialId, siteId, ctcaeGrade, serious, acknowledged, priority, dateFrom, dateTo, page, pageSize)
        conditions: list[str] = ["1=1"]
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
        where_clause = " AND ".join(conditions)
        conn = get_conn()
        conn.rollback()
        with conn.cursor() as cursor:
            logger.info(json.dumps({"event": "db_operation", "table": "ae_notifications", "operation": "SELECT"}))
            cursor.execute(f"SELECT COUNT(*) FROM ae_notifications WHERE {where_clause}", tuple(params))
            total_row = cursor.fetchone()
            total = int(total_row[0]) if total_row is not None else 0
            offset = (page - 1) * pageSize
            cursor.execute(
                f"SELECT notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, outcome, priority, acknowledged, sns_published, created_at FROM ae_notifications WHERE {where_clause} ORDER BY created_at DESC LIMIT %s OFFSET %s",
                tuple(params + [pageSize, offset]),
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
    except psycopg2.Error as exc:
        logger.error(json.dumps({"event": "db_error", "error": str(exc)}))
        raise HTTPException(status_code=500, detail="Database Error")
    except Exception as exc:
        logger.error(json.dumps({"event": "unexpected_error", "error": str(exc)}), exc_info=True)
        raise HTTPException(status_code=500, detail="Internal Error")
    finally:
        if conn is not None:
            release_conn(conn)

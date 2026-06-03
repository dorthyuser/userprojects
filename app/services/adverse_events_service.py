import json
import logging
import os
import time
from datetime import UTC, datetime
from typing import Any

import boto3
from dateutil.parser import isoparse
from fastapi import Request
from psycopg2 import DatabaseError, IntegrityError

from app.db.connection import get_conn, release_conn
from app.models.adverse_events_model import NotificationRecord
from app.schemas.adverse_events_schema import (
    AdverseEventCreateRequest,
    AdverseEventCreateResponse,
    NotificationListResponse,
    NotificationResponse,
)

logger = logging.getLogger(__name__)
sns_client = boto3.client("sns", region_name=os.environ.get("AWS_REGION", "eu-west-2"))
SNS_TOPIC_ARN = os.environ.get("SNS_TOPIC_ARN")
IDEMPOTENCY_WINDOW_S = int(os.environ.get("IDEMPOTENCY_WINDOW_S", "60"))


def _log(event: str, **kwargs: Any) -> None:
    logger.info(json.dumps({"event": event, **kwargs}))


def _log_error(event: str, **kwargs: Any) -> None:
    logger.error(json.dumps({"event": event, **kwargs}))


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
        raise ValueError("INVALID_QUERY_PARAM: ctcaeGrade")
    if priority is not None and priority not in {"HIGH", "NORMAL"}:
        raise ValueError("INVALID_QUERY_PARAM: priority")
    if page < 1:
        raise ValueError("INVALID_QUERY_PARAM: page")
    if pageSize < 1 or pageSize > 100:
        raise ValueError("INVALID_QUERY_PARAM: pageSize")
    parsed_from = isoparse(dateFrom) if dateFrom else None
    parsed_to = isoparse(dateTo) if dateTo else None
    return parsed_from, parsed_to


def create_adverse_event(request: Request, payload: AdverseEventCreateRequest) -> AdverseEventCreateResponse:
    _log("service_start", resource="adverse_event", method="POST", path=str(request.url.path))
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
                raise LookupError("TRIAL_NOT_FOUND")

            _log("db_operation", table="trial_enrolments", operation="SELECT")
            cursor.execute(
                "SELECT id FROM trial_enrolments WHERE trial_id = %s AND patient_id = %s AND status = 'ENROLLED'",
                (payload.trialId, payload.patientId),
            )
            if cursor.fetchone() is None:
                raise LookupError("PATIENT_NOT_FOUND")

            _log("db_operation", table="adverse_events", operation="SELECT")
            cursor.execute(
                "SELECT ae_id FROM adverse_events WHERE trial_id = %s AND patient_id = %s AND ae_term_code = %s AND ctcae_grade = %s AND submitted_at >= NOW() - (%s || ' seconds')::interval ORDER BY submitted_at DESC LIMIT 1",
                (payload.trialId, payload.patientId, payload.aeTermCode, payload.ctcaeGrade, IDEMPOTENCY_WINDOW_S),
            )
            duplicate = cursor.fetchone()
            if duplicate is not None:
                raise FileExistsError(f"DUPLICATE_AE:{duplicate[0]}")

            _log("db_operation", table="adverse_events", operation="INSERT")
            cursor.execute("SELECT 'AE-' || TO_CHAR(NOW(),'YYYY') || '-' || LPAD(NEXTVAL('ae_id_seq')::text, 6, '0')")
            ae_row = cursor.fetchone()
            if ae_row is None:
                conn.rollback()
                raise RuntimeError("AE_ID_GENERATION_FAILED")
            ae_id = ae_row[0]

            _log("db_operation", table="ae_notifications", operation="INSERT")
            cursor.execute("SELECT 'NOTIF-' || TO_CHAR(NOW(),'YYYY') || '-' || LPAD(NEXTVAL('notif_id_seq')::text, 6, '0')")
            notif_row = cursor.fetchone()
            if notif_row is None:
                conn.rollback()
                raise RuntimeError("NOTIFICATION_ID_GENERATION_FAILED")
            notification_id = notif_row[0]

            submitted_at = datetime.now(UTC)
            priority = "HIGH" if payload.ctcaeGrade >= 3 else "NORMAL"
            serious = payload.serious or payload.ctcaeGrade >= 3
            outcome = "FATAL" if payload.ctcaeGrade == 5 else payload.outcome

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
                    serious,
                    outcome,
                    payload.actionTaken,
                    payload.narrative,
                    payload.relatedDrugId,
                    payload.reportedBy,
                    submitted_at,
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
                    serious,
                    outcome,
                    priority,
                ),
            )
            conn.commit()
    except LookupError as exc:
        if conn is not None:
            conn.rollback()
        message = str(exc)
        raise ValueError(message) from None
    except FileExistsError as exc:
        if conn is not None:
            conn.rollback()
        existing_ae_id = str(exc).split(":", 1)[1]
        raise RuntimeError(f"DUPLICATE_AE:{existing_ae_id}") from None
    except (DatabaseError, IntegrityError) as exc:
        if conn is not None:
            conn.rollback()
        _log_error("db_error", error=str(exc))
        raise RuntimeError("DB_ERROR") from None
    finally:
        if conn is not None:
            release_conn(conn)

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
            conn = None
            try:
                conn = get_conn()
                conn.rollback()
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
                _log_error("sns_message_update_failed", ae_id=ae_id, notification_id=notification_id, error=str(exc))
            finally:
                if conn is not None:
                    release_conn(conn)
    except Exception as exc:
        _log_error("sns_publish_failed", ae_id=ae_id, notification_id=notification_id, error_class=exc.__class__.__name__, error=str(exc))
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


def get_notifications(
    request: Request,
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
    _log("service_start", resource="notification", method="GET", path=str(request.url.path))
    try:
        parsed_from, parsed_to = _validate_query_params(
            trialId,
            siteId,
            ctcaeGrade,
            serious,
            acknowledged,
            priority,
            dateFrom,
            dateTo,
            page,
            pageSize,
        )
    except ValueError as exc:
        _log_error("validation_failed", rule=str(exc))
        raise ValueError("INVALID_QUERY_PARAM") from None

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
    conn = None
    try:
        conn = get_conn()
        conn.rollback()
        with conn.cursor() as cursor:
            _log("db_operation", table="ae_notifications", operation="SELECT")
            cursor.execute(f"SELECT COUNT(*) FROM ae_notifications{where_clause}", tuple(params))
            total_row = cursor.fetchone()
            total = int(total_row[0]) if total_row is not None else 0

            data_params = list(params) + [pageSize, (page - 1) * pageSize]
            cursor.execute(
                f"SELECT notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, outcome, priority, acknowledged, sns_published, created_at FROM ae_notifications{where_clause} ORDER BY created_at DESC LIMIT %s OFFSET %s",
                tuple(data_params),
            )
            rows = cursor.fetchall()
    except (DatabaseError, Exception) as exc:
        _log_error("db_error", error=str(exc))
        raise RuntimeError("DB_ERROR") from None
    finally:
        if conn is not None:
            release_conn(conn)

    notifications = [
        NotificationResponse(
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

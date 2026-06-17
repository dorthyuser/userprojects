import json
import logging
import os
import re
import time
from dataclasses import asdict
from datetime import datetime, timezone
from typing import Any

import boto3
import psycopg2
from dateutil.parser import isoparse
from fastapi import HTTPException

from app.db.connection import get_conn, release_conn
from app.models.adverse_events_model import AdverseEventRecord, NotificationRecord
from app.schemas.adverse_events_schema import (
    AdverseEventCreateRequest,
    AdverseEventCreateResponse,
    NotificationListResponse,
    NotificationResponse,
)

logger = logging.getLogger(__name__)
logger.setLevel(os.environ.get("LOG_LEVEL", "INFO"))
_sns_client = boto3.client("sns", region_name=os.environ.get("AWS_REGION", "eu-west-2"))


def _utc_now() -> datetime:
    return datetime.now(timezone.utc)


def _log(message: dict[str, Any]) -> None:
    logger.info(json.dumps(message, default=str))


def _error(message: dict[str, Any]) -> None:
    logger.error(json.dumps(message, default=str))


def _validate_query_datetime(value: str | None, field_name: str) -> datetime | None:
    if value is None:
        return None
    try:
        parsed = isoparse(value)
    except Exception as exc:
        _error({"event": "validation_failure", "rule": field_name})
        raise HTTPException(status_code=502, detail="Validation Error") from exc
    if parsed.tzinfo is None:
        _error({"event": "validation_failure", "rule": field_name})
        raise HTTPException(status_code=502, detail="Validation Error")
    return parsed.astimezone(timezone.utc)


def _get_idempotency_window() -> int:
    raw = os.environ.get("IDEMPOTENCY_WINDOW_S", "60")
    try:
        return int(raw)
    except ValueError as exc:
        _error({"event": "validation_failure", "rule": "IDEMPOTENCY_WINDOW_S"})
        raise RuntimeError("Invalid IDEMPOTENCY_WINDOW_S") from exc


def _retry_get_conn() -> Any:
    last_exc: Exception | None = None
    for attempt in range(3):
        try:
            conn = get_conn()
            conn.rollback()
            return conn
        except Exception as exc:
            last_exc = exc
            _error({"event": "db_connection_failed", "message": str(exc)})
            if attempt < 2:
                time.sleep(0.2)
    raise RuntimeError("DB connection failed") from last_exc


def _generate_ae_id(cursor: Any) -> str:
    cursor.execute("SELECT 'AE-' || TO_CHAR(NOW(),'YYYY') || '-' || LPAD(NEXTVAL('ae_id_seq')::text, 6, '0')")
    row = cursor.fetchone()
    if row is None:
        raise RuntimeError("Failed to generate ae_id")
    return str(row[0])


def _generate_notification_id(cursor: Any) -> str:
    cursor.execute("SELECT 'NOTIF-' || TO_CHAR(NOW(),'YYYY') || '-' || LPAD(NEXTVAL('notif_id_seq')::text, 6, '0')")
    row = cursor.fetchone()
    if row is None:
        raise RuntimeError("Failed to generate notification_id")
    return str(row[0])


def create_adverse_event(payload: AdverseEventCreateRequest) -> AdverseEventCreateResponse:
    _log({"event": "service_start", "operation": "create_adverse_event", "resource": "adverse_events"})
    conn = _retry_get_conn()
    try:
        with conn.cursor() as cursor:
            _log({"event": "db_operation", "table": "trials", "operation": "SELECT"})
            cursor.execute("SELECT id FROM trials WHERE trial_id = %s AND status = 'ACTIVE'", (payload.trialId,))
            if cursor.fetchone() is None:
                raise HTTPException(status_code=502, detail="Service Unavailable")

            _log({"event": "db_operation", "table": "trial_enrolments", "operation": "SELECT"})
            cursor.execute("SELECT id FROM trial_enrolments WHERE trial_id = %s AND patient_id = %s AND status = 'ENROLLED'", (payload.trialId, payload.patientId))
            if cursor.fetchone() is None:
                raise HTTPException(status_code=502, detail="Service Unavailable")

            window_s = _get_idempotency_window()
            _log({"event": "db_operation", "table": "adverse_events", "operation": "SELECT"})
            cursor.execute(
                "SELECT ae_id FROM adverse_events WHERE trial_id = %s AND patient_id = %s AND ae_term_code = %s AND ctcae_grade = %s AND submitted_at >= NOW() - (%s || ' seconds')::interval ORDER BY submitted_at DESC LIMIT 1",
                (payload.trialId, payload.patientId, payload.aeTermCode, payload.ctcaeGrade, str(window_s)),
            )
            duplicate = cursor.fetchone()
            if duplicate is not None:
                existing_ae_id = str(duplicate[0])
                raise HTTPException(status_code=409, detail=f"DUPLICATE_AE:{existing_ae_id}")

            ae_id = _generate_ae_id(cursor)
            notification_id = _generate_notification_id(cursor)
            received_at = _utc_now()

            adverse_event = AdverseEventRecord(
                ae_id=ae_id,
                trial_id=payload.trialId,
                site_id=payload.siteId,
                patient_id=payload.patientId,
                clinician_id=payload.clinicianId,
                event_date=payload.eventDate,
                ae_term_code=payload.aeTermCode,
                ae_term_name=payload.aeTermName,
                ctcae_grade=payload.ctcaeGrade,
                serious=payload.serious,
                outcome=payload.outcome,
                action_taken=payload.actionTaken,
                narrative=payload.narrative,
                related_drug_id=payload.relatedDrugId,
                reported_by=payload.reportedBy,
                submitted_at=received_at,
                created_at=received_at,
                updated_at=received_at,
            )
            notification = NotificationRecord(
                notification_id=notification_id,
                ae_id=ae_id,
                trial_id=payload.trialId,
                site_id=payload.siteId,
                patient_id=payload.patientId,
                ae_term_name=payload.aeTermName,
                ctcae_grade=payload.ctcaeGrade,
                serious=payload.serious,
                outcome=payload.outcome,
                priority="HIGH" if payload.ctcaeGrade >= 3 else "NORMAL",
                acknowledged=False,
                acknowledged_by=None,
                acknowledged_at=None,
                sns_published=False,
                sns_message_id=None,
                created_at=received_at,
                updated_at=received_at,
            )

            conn.rollback()
            conn.autocommit = False
            try:
                _log({"event": "db_operation", "table": "adverse_events", "operation": "INSERT"})
                cursor.execute(
                    "INSERT INTO adverse_events (ae_id, trial_id, site_id, patient_id, clinician_id, event_date, ae_term_code, ae_term_name, ctcae_grade, serious, outcome, action_taken, narrative, related_drug_id, reported_by, submitted_at, created_at, updated_at) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s)",
                    (
                        adverse_event.ae_id,
                        adverse_event.trial_id,
                        adverse_event.site_id,
                        adverse_event.patient_id,
                        adverse_event.clinician_id,
                        adverse_event.event_date,
                        adverse_event.ae_term_code,
                        adverse_event.ae_term_name,
                        adverse_event.ctcae_grade,
                        adverse_event.serious,
                        adverse_event.outcome,
                        adverse_event.action_taken,
                        adverse_event.narrative,
                        adverse_event.related_drug_id,
                        adverse_event.reported_by,
                        adverse_event.submitted_at,
                        adverse_event.created_at,
                        adverse_event.updated_at,
                    ),
                )
                _log({"event": "db_operation", "table": "ae_notifications", "operation": "INSERT"})
                cursor.execute(
                    "INSERT INTO ae_notifications (notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, outcome, priority, acknowledged, acknowledged_by, acknowledged_at, sns_published, sns_message_id, created_at, updated_at) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s)",
                    (
                        notification.notification_id,
                        notification.ae_id,
                        notification.trial_id,
                        notification.site_id,
                        notification.patient_id,
                        notification.ae_term_name,
                        notification.ctcae_grade,
                        notification.serious,
                        notification.outcome,
                        notification.priority,
                        notification.acknowledged,
                        notification.acknowledged_by,
                        notification.acknowledged_at,
                        notification.sns_published,
                        notification.sns_message_id,
                        notification.created_at,
                        notification.updated_at,
                    ),
                )
                conn.commit()
            except Exception as exc:
                conn.rollback()
                _error({"event": "transaction_failure", "message": str(exc)})
                raise HTTPException(status_code=500, detail="Internal Error") from exc

            sns_published = False
            sns_message_id = None

            # Safely publish to SNS only if SNS_TOPIC_ARN is configured
            topic_arn = os.environ.get("SNS_TOPIC_ARN")
            if topic_arn:
                try:
                    response = _sns_client.publish(
                        TopicArn=topic_arn,
                        Message=json.dumps({"aeId": ae_id, "notificationId": notification_id}),
                    )
                    sns_message_id = response.get("MessageId")
                    sns_published = True
                    try:
                        conn2 = _retry_get_conn()
                        try:
                            with conn2.cursor() as cursor2:
                                _log({"event": "db_operation", "table": "ae_notifications", "operation": "UPDATE"})
                                cursor2.execute(
                                    "UPDATE ae_notifications SET sns_published = %s, sns_message_id = %s WHERE notification_id = %s",
                                    (True, sns_message_id, notification_id),
                                )
                                conn2.commit()
                        finally:
                            release_conn(conn2)
                    except Exception as exc:
                        _error({"event": "sns_update_failed", "ae_id": ae_id, "notification_id": notification_id, "message": str(exc)})
                except Exception as exc:
                    _error({"event": "sns_publish_failed", "ae_id": ae_id, "notification_id": notification_id, "exception": exc.__class__.__name__, "message": str(exc)})
                    sns_published = False
                    sns_message_id = None
            else:
                # Do not raise an exception if SNS isn't configured; log and continue
                _error({"event": "sns_publish_failed", "ae_id": ae_id, "notification_id": notification_id, "exception": "MissingEnvironment", "message": "SNS_TOPIC_ARN not configured"})
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
    except HTTPException:
        raise
    except psycopg2.Error as exc:
        _error({"event": "db_error", "message": str(exc)})
        raise HTTPException(status_code=503, detail="Database Error") from exc
    except Exception as exc:
        _error({"event": "unexpected_error", "message": str(exc)})
        raise HTTPException(status_code=500, detail="Internal Error") from exc
    finally:
        release_conn(conn)


def get_notifications(trialId: str | None, siteId: str | None, ctcaeGrade: int | None, serious: bool | None, acknowledged: bool | None, priority: str | None, dateFrom: str | None, dateTo: str | None, page: int, pageSize: int) -> NotificationListResponse:
    _log({"event": "service_start", "operation": "get_notifications", "resource": "ae_notifications"})
    if page < 1 or pageSize < 1 or pageSize > 100:
        raise HTTPException(status_code=502, detail="Validation Error")
    date_from = _validate_query_datetime(dateFrom, "dateFrom")
    date_to = _validate_query_datetime(dateTo, "dateTo")
    if priority is not None and priority not in {"HIGH", "NORMAL"}:
        raise HTTPException(status_code=502, detail="Validation Error")
    conn = _retry_get_conn()
    try:
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
                raise HTTPException(status_code=502, detail="Validation Error")
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
        count_sql = "SELECT COUNT(*) FROM ae_notifications" + where_clause
        data_sql = "SELECT notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, outcome, priority, acknowledged, sns_published, created_at FROM ae_notifications" + where_clause + " ORDER BY created_at DESC LIMIT %s OFFSET %s"
        offset = (page - 1) * pageSize
        with conn.cursor() as cursor:
            _log({"event": "db_operation", "table": "ae_notifications", "operation": "SELECT"})
            cursor.execute(count_sql, tuple(params))
            total_row = cursor.fetchone()
            total = int(total_row[0]) if total_row is not None else 0
            cursor.execute(data_sql, tuple(params + [pageSize, offset]))
            rows = cursor.fetchall()
        notifications = [
            NotificationResponse(
                notificationId=str(row[0]),
                aeId=str(row[1]),
                trialId=str(row[2]),
                siteId=str(row[3]),
                patientId=str(row[4]),
                aeTermName=str(row[5]),
                ctcaeGrade=int(row[6]),
                serious=bool(row[7]),
                priority=str(row[9]),
                outcome=str(row[8]),
                acknowledged=bool(row[10]),
                snsPublished=bool(row[11]),
                createdAt=row[12],
            )
            for row in rows
        ]
        return NotificationListResponse(status="success", total=total, page=page, pageSize=pageSize, notifications=notifications)
    except HTTPException:
        raise
    except psycopg2.Error as exc:
        _error({"event": "db_error", "message": str(exc)})
        raise HTTPException(status_code=503, detail="Database Error") from exc
    except Exception as exc:
        _error({"event": "unexpected_error", "message": str(exc)})
        raise HTTPException(status_code=500, detail="Internal Error") from exc
    finally:
        release_conn(conn)

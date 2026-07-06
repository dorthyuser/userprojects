import json
import logging
import os
import re
import time
from dataclasses import asdict
from datetime import datetime, timezone
from typing import Any

from fastapi import HTTPException
from psycopg2 import Error as Psycopg2Error
from psycopg2.extras import RealDictCursor

from app.db.connection import get_conn, release_conn
from app.models.adverse_event_model import AdverseEventRecord, NotificationRecord
from app.schemas.adverse_event_schema import (
    AdverseEventCreateRequest,
    AdverseEventCreateResponse,
    NotificationListResponse,
    NotificationResponseItem,
)

logger = logging.getLogger(__name__)


def _utc_now() -> datetime:
    return datetime.now(timezone.utc)


def _parse_iso_datetime(value: str | None, field_name: str) -> datetime | None:
    if value is None:
        return None
    try:
        parsed = datetime.fromisoformat(value.replace("Z", "+00:00"))
    except ValueError as exc:
        logger.error("Validation failed: %s", field_name)
        raise HTTPException(status_code=400, detail="Parsing Error") from exc
    if parsed.tzinfo is None:
        logger.error("Validation failed: %s", field_name)
        raise HTTPException(status_code=400, detail="Parsing Error")
    return parsed.astimezone(timezone.utc)


def _retry_get_conn() -> Any:
    last_error: Exception | None = None
    for attempt in range(3):
        try:
            conn = get_conn()
            conn.rollback()
            conn.autocommit = False
            return conn
        except Exception as exc:
            last_error = exc
            logger.error("DB connection failed: %s", str(exc))
            if attempt < 2:
                time.sleep(0.2)
    raise RuntimeError("DB connection failed") from last_error


def _validate_required_fields(payload: AdverseEventCreateRequest) -> None:
    required_fields = [
        "trialId",
        "siteId",
        "patientId",
        "clinicianId",
        "eventDate",
        "aeTermCode",
        "aeTermName",
        "ctcaeGrade",
        "serious",
        "outcome",
        "actionTaken",
        "narrative",
        "reportedBy",
    ]
    for field_name in required_fields:
        if getattr(payload, field_name) is None:
            logger.error("Validation failed: missing required field")
            raise HTTPException(status_code=400, detail="Validation Error")


def _coerce_payload(payload: AdverseEventCreateRequest) -> AdverseEventCreateRequest:
    serious = payload.serious or payload.ctcaeGrade >= 3
    outcome = payload.outcome
    if payload.ctcaeGrade == 5:
        outcome = "FATAL"
    return payload.model_copy(update={"serious": serious, "outcome": outcome})


def submit_adverse_event_service(payload: AdverseEventCreateRequest) -> AdverseEventCreateResponse:
    logger.info(json.dumps({"event": "service_entry", "operation": "submit_adverse_event", "resource": "adverse_events"}))
    _validate_required_fields(payload)
    coerced = _coerce_payload(payload)
    event_date = _parse_iso_datetime(coerced.eventDate, "eventDate")
    if event_date is None:
        raise HTTPException(status_code=400, detail="Validation Error")

    conn = _retry_get_conn()
    try:
        with conn.cursor() as cursor:
            logger.info(json.dumps({"event": "db_operation", "table": "trials", "operation": "SELECT"}))
            cursor.execute("SELECT id FROM trials WHERE trial_id = %s AND status = 'ACTIVE'", (coerced.trialId,))
            if cursor.fetchone() is None:
                raise HTTPException(status_code=404, detail="Resource Not Found")

            logger.info(json.dumps({"event": "db_operation", "table": "trial_enrolments", "operation": "SELECT"}))
            cursor.execute(
                "SELECT id FROM trial_enrolments WHERE trial_id = %s AND patient_id = %s AND status = 'ENROLLED'",
                (coerced.trialId, coerced.patientId),
            )
            if cursor.fetchone() is None:
                raise HTTPException(status_code=404, detail="Resource Not Found")

            logger.info(json.dumps({"event": "db_operation", "table": "adverse_events", "operation": "SELECT"}))
            cursor.execute(
                "SELECT ae_id FROM adverse_events WHERE trial_id = %s AND patient_id = %s AND ae_term_code = %s AND ctcae_grade = %s AND submitted_at >= NOW() - INTERVAL '60 seconds' ORDER BY submitted_at DESC LIMIT 1",
                (coerced.trialId, coerced.patientId, coerced.aeTermCode, coerced.ctcaeGrade),
            )
            duplicate_row = cursor.fetchone()
            if duplicate_row is not None:
                raise HTTPException(status_code=409, detail="Validation Error")

            logger.info(json.dumps({"event": "db_operation", "table": "ae_id_seq", "operation": "SELECT"}))
            cursor.execute("SELECT 'AE-' || TO_CHAR(NOW(),'YYYY') || '-' || LPAD(NEXTVAL('ae_id_seq')::text, 6, '0')")
            ae_id_row = cursor.fetchone()
            if ae_id_row is None:
                conn.rollback()
                raise HTTPException(status_code=500, detail="Internal Error")
            ae_id = ae_id_row[0]

            logger.info(json.dumps({"event": "db_operation", "table": "notif_id_seq", "operation": "SELECT"}))
            cursor.execute("SELECT 'NOTIF-' || TO_CHAR(NOW(),'YYYY') || '-' || LPAD(NEXTVAL('notif_id_seq')::text, 6, '0')")
            notif_id_row = cursor.fetchone()
            if notif_id_row is None:
                conn.rollback()
                raise HTTPException(status_code=500, detail="Internal Error")
            notification_id = notif_id_row[0]

            logger.info(json.dumps({"event": "db_operation", "table": "adverse_events", "operation": "INSERT"}))
            cursor.execute(
                "INSERT INTO adverse_events (ae_id, trial_id, site_id, patient_id, clinician_id, event_date, ae_term_code, ae_term_name, ctcae_grade, serious, outcome, action_taken, narrative, related_drug_id, reported_by, submitted_at, created_at, updated_at) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, NOW(), NOW()) RETURNING id",
                (
                    ae_id,
                    coerced.trialId,
                    coerced.siteId,
                    coerced.patientId,
                    coerced.clinicianId,
                    event_date,
                    coerced.aeTermCode,
                    coerced.aeTermName,
                    coerced.ctcaeGrade,
                    coerced.serious,
                    coerced.outcome,
                    coerced.actionTaken,
                    coerced.narrative,
                    coerced.relatedDrugId,
                    coerced.reportedBy,
                    _utc_now(),
                ),
            )
            inserted_ae = cursor.fetchone()
            if inserted_ae is None:
                conn.rollback()
                raise HTTPException(status_code=500, detail="Internal Error")

            logger.info(json.dumps({"event": "db_operation", "table": "ae_notifications", "operation": "INSERT"}))
            priority = "HIGH" if coerced.ctcaeGrade >= 3 else "NORMAL"
            cursor.execute(
                "INSERT INTO ae_notifications (notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, outcome, priority, acknowledged, sns_published, sns_message_id, created_at, updated_at) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, FALSE, FALSE, NULL, NOW(), NOW()) RETURNING id",
                (
                    notification_id,
                    ae_id,
                    coerced.trialId,
                    coerced.siteId,
                    coerced.patientId,
                    coerced.aeTermName,
                    coerced.ctcaeGrade,
                    coerced.serious,
                    coerced.outcome,
                    priority,
                ),
            )
            inserted_notif = cursor.fetchone()
            if inserted_notif is None:
                conn.rollback()
                raise HTTPException(status_code=500, detail="Internal Error")

        conn.commit()
        try:
            _ = False
        except Exception:
            pass
        return AdverseEventCreateResponse(
            status="success",
            aeId=ae_id,
            notificationId=notification_id,
            snsPublished=False,
            snsMessageId=None,
            receivedAt=_utc_now(),
        )
    except HTTPException:
        conn.rollback()
        raise
    except Psycopg2Error as exc:
        conn.rollback()
        logger.error("Database error: %s", str(exc))
        raise HTTPException(status_code=503, detail="Database Error") from exc
    except Exception as exc:
        conn.rollback()
        logger.error("Unexpected error: %s", str(exc), exc_info=True)
        raise HTTPException(status_code=500, detail="Internal Error") from exc
    finally:
        release_conn(conn)


def _validate_notification_query(
    ctcae_grade: int | None,
    priority: str | None,
    page: int,
    page_size: int,
    date_from: str | None,
    date_to: str | None,
) -> tuple[datetime | None, datetime | None]:
    if ctcae_grade is not None and ctcae_grade not in {1, 2, 3, 4, 5}:
        logger.error("Validation failed: ctcaeGrade")
        raise HTTPException(status_code=400, detail="Validation Error")
    if priority is not None and priority not in {"HIGH", "NORMAL"}:
        logger.error("Validation failed: priority")
        raise HTTPException(status_code=400, detail="Validation Error")
    if page < 1 or page_size < 1 or page_size > 100:
        logger.error("Validation failed: pagination")
        raise HTTPException(status_code=400, detail="Validation Error")
    return _parse_iso_datetime(date_from, "dateFrom"), _parse_iso_datetime(date_to, "dateTo")


def get_notifications_service(
    trial_id: str | None,
    site_id: str | None,
    ctcae_grade: int | None,
    serious: bool | None,
    acknowledged: bool | None,
    priority: str | None,
    date_from: str | None,
    date_to: str | None,
    page: int,
    page_size: int,
) -> NotificationListResponse:
    logger.info(json.dumps({"event": "service_entry", "operation": "get_notifications", "resource": "ae_notifications"}))
    parsed_date_from, parsed_date_to = _validate_notification_query(ctcae_grade, priority, page, page_size, date_from, date_to)
    conn = _retry_get_conn()
    try:
        conditions: list[str] = []
        params: list[Any] = []
        if trial_id is not None:
            conditions.append("trial_id = %s")
            params.append(trial_id)
        if site_id is not None:
            conditions.append("site_id = %s")
            params.append(site_id)
        if ctcae_grade is not None:
            conditions.append("ctcae_grade = %s")
            params.append(ctcae_grade)
        if serious is not None:
            conditions.append("serious = %s")
            params.append(serious)
        if acknowledged is not None:
            conditions.append("acknowledged = %s")
            params.append(acknowledged)
        if priority is not None:
            conditions.append("priority = %s")
            params.append(priority)
        if parsed_date_from is not None:
            conditions.append("created_at >= %s")
            params.append(parsed_date_from)
        if parsed_date_to is not None:
            conditions.append("created_at <= %s")
            params.append(parsed_date_to)

        where_clause = " WHERE " + " AND ".join(conditions) if conditions else ""
        count_sql = f"SELECT COUNT(*) FROM ae_notifications{where_clause}"
        data_sql = f"SELECT notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, priority, outcome, acknowledged, sns_published, created_at FROM ae_notifications{where_clause} ORDER BY created_at DESC LIMIT %s OFFSET %s"

        with conn.cursor(cursor_factory=RealDictCursor) as cursor:
            logger.info(json.dumps({"event": "db_operation", "table": "ae_notifications", "operation": "SELECT"}))
            cursor.execute(count_sql, tuple(params))
            total_row = cursor.fetchone()
            total = int(total_row["count"]) if total_row is not None else 0

            logger.info(json.dumps({"event": "db_operation", "table": "ae_notifications", "operation": "SELECT"}))
            cursor.execute(data_sql, tuple(params + [page_size, (page - 1) * page_size]))
            rows = cursor.fetchall()

        notifications = [
            NotificationResponseItem(
                notificationId=row["notification_id"],
                aeId=row["ae_id"],
                trialId=row["trial_id"],
                siteId=row["site_id"],
                patientId=row["patient_id"],
                aeTermName=row["ae_term_name"],
                ctcaeGrade=row["ctcae_grade"],
                serious=row["serious"],
                priority=row["priority"],
                outcome=row["outcome"],
                acknowledged=row["acknowledged"],
                snsPublished=row["sns_published"],
                createdAt=row["created_at"],
            )
            for row in rows
        ]
        return NotificationListResponse(status="success", total=total, page=page, pageSize=page_size, notifications=notifications)
    except HTTPException:
        raise
    except Psycopg2Error as exc:
        logger.error("Database error: %s", str(exc))
        raise HTTPException(status_code=503, detail="Database Error") from exc
    except Exception as exc:
        logger.error("Unexpected error: %s", str(exc), exc_info=True)
        raise HTTPException(status_code=500, detail="Internal Error") from exc
    finally:
        release_conn(conn)

import json
import logging
import os
from dataclasses import asdict
from datetime import UTC, datetime
from time import sleep
from typing import Any

import psycopg2
from fastapi import HTTPException
from psycopg2 import Error as Psycopg2Error
from psycopg2.extras import RealDictCursor
from pydantic import ValidationError
from dateutil.parser import isoparse

from app.db.connection import get_conn, release_conn
from app.models.adverse_events_model import AdverseEventRecord, NotificationRecord
from app.schemas.adverse_events_schema import (
    AdverseEventCreateRequest,
    AdverseEventCreateResponse,
    NotificationListResponse,
    NotificationResponseItem,
)

logger = logging.getLogger(__name__)

ALLOWED_OUTCOMES = {"ONGOING", "RESOLVED", "FATAL", "UNKNOWN"}
ALLOWED_ACTIONS = {"NONE", "DOSE_REDUCED", "DRUG_WITHDRAWN", "HOSPITALISED"}
ALLOWED_PRIORITIES = {"HIGH", "NORMAL"}


def _raise_validation_error(message: str) -> None:
    logger.error(json.dumps({"event": "validation_failure", "rule": message}))
    raise HTTPException(status_code=422, detail="Validation Error")


def _parse_utc_datetime(value: str, field_name: str) -> datetime:
    try:
        parsed = isoparse(value)
    except Exception:
        _raise_validation_error(f"invalid_{field_name}")
    if parsed.tzinfo is None:
        _raise_validation_error(f"naive_{field_name}")
    return parsed.astimezone(UTC)


def _validate_request(payload: AdverseEventCreateRequest) -> None:
    if payload.ctcaeGrade < 1 or payload.ctcaeGrade > 5:
        _raise_validation_error("invalid_ctcae_grade")
    if payload.outcome not in ALLOWED_OUTCOMES:
        _raise_validation_error("invalid_outcome")
    if payload.actionTaken not in ALLOWED_ACTIONS:
        _raise_validation_error("invalid_action_taken")
    if len(payload.narrative) > 2000:
        _raise_validation_error("narrative_too_long")


def _coerce_request(payload: AdverseEventCreateRequest) -> AdverseEventCreateRequest:
    serious = True if payload.ctcaeGrade >= 3 else payload.serious
    outcome = "FATAL" if payload.ctcaeGrade == 5 else payload.outcome
    return payload.model_copy(update={"serious": serious, "outcome": outcome})


def _get_required_env(name: str) -> str:
    value = os.getenv(name)
    if value is None or value == "":
        logger.error(json.dumps({"event": "missing_environment_variable", "variable": name}))
        raise RuntimeError(f"Missing required environment variable: {name}")
    return value


def _retryable_get_conn() -> Any:
    last_error: Exception | None = None
    for attempt in range(3):
        try:
            conn = get_conn()
            conn.rollback()
            conn.autocommit = False
            return conn
        except Exception as exc:
            last_error = exc
            logger.error(json.dumps({"event": "db_connection_failed", "message": str(exc)}))
            if attempt < 2:
                sleep(0.2)
    raise RuntimeError("DB connection failed") from last_error


def _build_notification_filters(
    trialId: str | None,
    siteId: str | None,
    ctcaeGrade: int | None,
    serious: bool | None,
    acknowledged: bool | None,
    priority: str | None,
    dateFrom: str | None,
    dateTo: str | None,
) -> tuple[list[str], list[Any]]:
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
            _raise_validation_error("invalid_ctcae_grade")
        conditions.append("ctcae_grade = %s")
        params.append(ctcaeGrade)
    if serious is not None:
        conditions.append("serious = %s")
        params.append(serious)
    if acknowledged is not None:
        conditions.append("acknowledged = %s")
        params.append(acknowledged)
    if priority is not None:
        if priority not in ALLOWED_PRIORITIES:
            _raise_validation_error("invalid_priority")
        conditions.append("priority = %s")
        params.append(priority)
    if dateFrom is not None:
        conditions.append("created_at >= %s")
        params.append(_parse_utc_datetime(dateFrom, "dateFrom"))
    if dateTo is not None:
        conditions.append("created_at <= %s")
        params.append(_parse_utc_datetime(dateTo, "dateTo"))

    return conditions, params


def submit_adverse_event_service(payload: AdverseEventCreateRequest) -> AdverseEventCreateResponse:
    logger.info(json.dumps({"event": "service_start", "operation": "submit_adverse_event", "resource": "adverse_events"}))
    _validate_request(payload)
    payload = _coerce_request(payload)

    conn = None
    try:
        conn = _retryable_get_conn()
        with conn.cursor(cursor_factory=RealDictCursor) as cursor:
            logger.info(json.dumps({"event": "db_operation", "table": "trials", "operation": "SELECT"}))
            cursor.execute(
                "SELECT id FROM trials WHERE trial_id = %s AND status = 'ACTIVE'",
                (payload.trialId,),
            )
            if cursor.fetchone() is None:
                raise HTTPException(status_code=404, detail="Resource Not Found")

            logger.info(json.dumps({"event": "db_operation", "table": "trial_enrolments", "operation": "SELECT"}))
            cursor.execute(
                "SELECT id FROM trial_enrolments WHERE trial_id = %s AND patient_id = %s AND status = 'ENROLLED'",
                (payload.trialId, payload.patientId),
            )
            if cursor.fetchone() is None:
                raise HTTPException(status_code=404, detail="Resource Not Found")

            logger.info(json.dumps({"event": "db_operation", "table": "adverse_events", "operation": "SELECT"}))
            cursor.execute(
                "SELECT ae_id FROM adverse_events WHERE trial_id = %s AND patient_id = %s AND ae_term_code = %s AND ctcae_grade = %s AND submitted_at >= NOW() - INTERVAL '60 seconds' ORDER BY submitted_at DESC LIMIT 1",
                (payload.trialId, payload.patientId, payload.aeTermCode, payload.ctcaeGrade),
            )
            duplicate_row = cursor.fetchone()
            if duplicate_row is not None:
                raise HTTPException(status_code=409, detail="Resource Not Found")

            logger.info(json.dumps({"event": "db_operation", "table": "ae_id_seq", "operation": "SELECT"}))
            cursor.execute("SELECT 'AE-' || TO_CHAR(NOW(),'YYYY') || '-' || LPAD(NEXTVAL('ae_id_seq')::text, 6, '0')")
            ae_id_row = cursor.fetchone()
            if ae_id_row is None:
                raise HTTPException(status_code=500, detail="Internal Error")
            ae_id = ae_id_row[0]

            logger.info(json.dumps({"event": "db_operation", "table": "notif_id_seq", "operation": "SELECT"}))
            cursor.execute("SELECT 'NOTIF-' || TO_CHAR(NOW(),'YYYY') || '-' || LPAD(NEXTVAL('notif_id_seq')::text, 6, '0')")
            notif_id_row = cursor.fetchone()
            if notif_id_row is None:
                raise HTTPException(status_code=500, detail="Internal Error")
            notification_id = notif_id_row[0]

            received_at = datetime.now(UTC)
            priority = "HIGH" if payload.ctcaeGrade >= 3 else "NORMAL"

            conn.autocommit = False
            logger.info(json.dumps({"event": "db_operation", "table": "adverse_events", "operation": "INSERT"}))
            cursor.execute(
                "INSERT INTO adverse_events (ae_id, trial_id, site_id, patient_id, clinician_id, event_date, ae_term_code, ae_term_name, ctcae_grade, serious, outcome, action_taken, narrative, related_drug_id, reported_by, submitted_at, created_at, updated_at) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, NOW(), NOW())",
                (
                    ae_id,
                    payload.trialId,
                    payload.siteId,
                    payload.patientId,
                    payload.clinicianId,
                    _parse_utc_datetime(payload.eventDate, "eventDate"),
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

            logger.info(json.dumps({"event": "db_operation", "table": "ae_notifications", "operation": "INSERT"}))
            cursor.execute(
                "INSERT INTO ae_notifications (notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, outcome, priority, acknowledged, sns_published, sns_message_id, created_at, updated_at) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, NOW(), NOW())",
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
                    False,
                    False,
                    None,
                ),
            )
            conn.commit()

        try:
            sns_published = False
            sns_message_id = None
            _ = (sns_published, sns_message_id)
        except Exception as exc:
            logger.error(json.dumps({"event": "notification_stub_error", "message": str(exc)}))

        return AdverseEventCreateResponse(
            status="success",
            aeId=ae_id,
            notificationId=notification_id,
            snsPublished=False,
            snsMessageId=None,
            receivedAt=received_at,
        )
    except HTTPException:
        if conn is not None:
            conn.rollback()
        raise
    except Psycopg2Error as exc:
        if conn is not None:
            conn.rollback()
        logger.error(json.dumps({"event": "database_error", "message": str(exc)}))
        raise HTTPException(status_code=503, detail="Database Error") from exc
    except ValidationError as exc:
        if conn is not None:
            conn.rollback()
        logger.error(json.dumps({"event": "validation_error", "message": str(exc)}))
        raise HTTPException(status_code=422, detail="Validation Error") from exc
    except Exception as exc:
        if conn is not None:
            conn.rollback()
        logger.error(json.dumps({"event": "unexpected_error", "message": str(exc)}), exc_info=True)
        raise HTTPException(status_code=500, detail="Internal Error") from exc
    finally:
        if conn is not None:
            release_conn(conn)


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
    logger.info(json.dumps({"event": "service_start", "operation": "get_notifications", "resource": "ae_notifications"}))
    if page < 1 or pageSize < 1 or pageSize > 100:
        _raise_validation_error("invalid_pagination")

    conditions, params = _build_notification_filters(
        trialId=trialId,
        siteId=siteId,
        ctcaeGrade=ctcaeGrade,
        serious=serious,
        acknowledged=acknowledged,
        priority=priority,
        dateFrom=dateFrom,
        dateTo=dateTo,
    )

    where_clause = ""
    if conditions:
        where_clause = " WHERE " + " AND ".join(conditions)

    conn = None
    try:
        conn = _retryable_get_conn()
        with conn.cursor(cursor_factory=RealDictCursor) as cursor:
            logger.info(json.dumps({"event": "db_operation", "table": "ae_notifications", "operation": "SELECT"}))
            cursor.execute(f"SELECT COUNT(*) AS total FROM ae_notifications{where_clause}", tuple(params))
            total_row = cursor.fetchone()
            total = int(total_row["total"]) if total_row is not None else 0

            offset = (page - 1) * pageSize
            cursor.execute(
                f"SELECT notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, outcome, priority, acknowledged, sns_published, created_at FROM ae_notifications{where_clause} ORDER BY created_at DESC LIMIT %s OFFSET %s",
                tuple(params + [pageSize, offset]),
            )
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
            return NotificationListResponse(status="success", total=total, page=page, pageSize=pageSize, notifications=notifications)
    except HTTPException:
        raise
    except Psycopg2Error as exc:
        logger.error(json.dumps({"event": "database_error", "message": str(exc)}))
        raise HTTPException(status_code=503, detail="Database Error") from exc
    except Exception as exc:
        logger.error(json.dumps({"event": "unexpected_error", "message": str(exc)}), exc_info=True)
        raise HTTPException(status_code=500, detail="Internal Error") from exc
    finally:
        if conn is not None:
            release_conn(conn)

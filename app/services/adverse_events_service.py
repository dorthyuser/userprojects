import json
import logging
import secrets
from datetime import datetime, timezone
from time import sleep
from typing import Any

import psycopg2
from fastapi import HTTPException, Request, status
from psycopg2 import Error as Psycopg2Error

from app.db.connection import get_conn, release_conn
from app.models.adverse_events_model import AdverseEventRecord, NotificationRecord
from app.schemas.adverse_events_schema import (
    AdverseEventCreateRequest,
    AdverseEventCreateResponse,
    NotificationListResponse,
    NotificationResponseItem,
)

logger = logging.getLogger(__name__)


def _utc_now() -> datetime:
    return datetime.now(timezone.utc)


def _log_json(message: str, **fields: Any) -> None:
    payload = {"message": message, **fields}
    logger.info(json.dumps(payload, default=str))


def _log_error(message: str, **fields: Any) -> None:
    payload = {"message": message, **fields}
    logger.error(json.dumps(payload, default=str))


def _raise_validation(rule: str) -> None:
    _log_error("validation_failed", rule=rule)
    raise HTTPException(status_code=status.HTTP_422_UNPROCESSABLE_ENTITY, detail="Validation Error")


def _raise_not_found() -> None:
    raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Resource Not Found")


def _raise_duplicate(existing_ae_id: str) -> None:
    raise HTTPException(status_code=status.HTTP_409_CONFLICT, detail=f"DUPLICATE_AE:{existing_ae_id}")


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
            _log_error("db_connection_failed", error=str(exc))
            if attempt < 2:
                sleep(0.2)
    raise RuntimeError("DB connection failed") from last_error


def _validate_create_payload(payload: AdverseEventCreateRequest) -> None:
    if payload.ctcaeGrade < 1 or payload.ctcaeGrade > 5:
        _raise_validation("INVALID_CTCAE_GRADE")
    if len(payload.narrative) > 2000:
        _raise_validation("NARRATIVE_TOO_LONG")


def _coerce_payload(payload: AdverseEventCreateRequest) -> AdverseEventCreateRequest:
    serious = True if payload.ctcaeGrade >= 3 else payload.serious
    outcome = "FATAL" if payload.ctcaeGrade == 5 else payload.outcome
    return payload.model_copy(update={"serious": serious, "outcome": outcome})


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


def _check_trial_exists(cursor: Any, trial_id: str) -> None:
    _log_json("db_select", table="trials", operation="SELECT")
    cursor.execute("SELECT id FROM trials WHERE trial_id = %s AND status = 'ACTIVE'", (trial_id,))
    if cursor.fetchone() is None:
        _raise_not_found()


def _check_patient_enrolled(cursor: Any, trial_id: str, patient_id: str) -> None:
    _log_json("db_select", table="trial_enrolments", operation="SELECT")
    cursor.execute(
        "SELECT id FROM trial_enrolments WHERE trial_id = %s AND patient_id = %s AND status = 'ENROLLED'",
        (trial_id, patient_id),
    )
    if cursor.fetchone() is None:
        _raise_not_found()


def _check_duplicate(cursor: Any, payload: AdverseEventCreateRequest) -> str | None:
    _log_json("db_select", table="adverse_events", operation="SELECT")
    cursor.execute(
        """
        SELECT ae_id
        FROM adverse_events
        WHERE trial_id = %s
          AND patient_id = %s
          AND ae_term_code = %s
          AND ctcae_grade = %s
          AND submitted_at >= NOW() - INTERVAL '60 seconds'
        ORDER BY submitted_at DESC
        LIMIT 1
        """,
        (payload.trialId, payload.patientId, payload.aeTermCode, payload.ctcaeGrade),
    )
    row = cursor.fetchone()
    return None if row is None else str(row[0])


def _insert_adverse_event(cursor: Any, ae_id: str, payload: AdverseEventCreateRequest) -> None:
    _log_json("db_insert", table="adverse_events", operation="INSERT")
    cursor.execute(
        """
        INSERT INTO adverse_events (
            ae_id, trial_id, site_id, patient_id, clinician_id, event_date,
            ae_term_code, ae_term_name, ctcae_grade, serious, outcome,
            action_taken, narrative, related_drug_id, reported_by, submitted_at,
            created_at, updated_at
        ) VALUES (
            %s, %s, %s, %s, %s, %s,
            %s, %s, %s, %s, %s,
            %s, %s, %s, %s, %s,
            NOW(), NOW()
        )
        """,
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
        ),
    )


def _insert_notification(cursor: Any, notification_id: str, ae_id: str, payload: AdverseEventCreateRequest) -> None:
    priority = "HIGH" if payload.ctcaeGrade >= 3 else "NORMAL"
    _log_json("db_insert", table="ae_notifications", operation="INSERT")
    cursor.execute(
        """
        INSERT INTO ae_notifications (
            notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name,
            ctcae_grade, serious, outcome, priority, acknowledged, sns_published,
            sns_message_id, created_at, updated_at
        ) VALUES (
            %s, %s, %s, %s, %s, %s,
            %s, %s, %s, %s::varchar, FALSE, FALSE,
            NULL, NOW(), NOW()
        )
        """,
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


def _notification_stub() -> tuple[bool, None]:
    try:
        return False, None
    except Exception as exc:
        _log_error("notification_stub_failed", error=str(exc))
        return False, None


def create_adverse_event(payload: AdverseEventCreateRequest, request: Request) -> AdverseEventCreateResponse:
    _log_json("route_entry", method=request.method, path=str(request.url.path), trialId=payload.trialId, siteId=payload.siteId)
    _validate_create_payload(payload)
    coerced = _coerce_payload(payload)
    conn = None
    try:
        conn = _retry_get_conn()
        with conn.cursor() as cursor:
            _check_trial_exists(cursor, coerced.trialId)
            _check_patient_enrolled(cursor, coerced.trialId, coerced.patientId)
            existing_ae_id = _check_duplicate(cursor, coerced)
            if existing_ae_id is not None:
                _raise_duplicate(existing_ae_id)
            ae_id = _generate_ae_id(cursor)
            notification_id = _generate_notification_id(cursor)
            _insert_adverse_event(cursor, ae_id, coerced)
            _insert_notification(cursor, notification_id, ae_id, coerced)
        conn.commit()
        sns_published, sns_message_id = _notification_stub()
        received_at = _utc_now()
        return AdverseEventCreateResponse(
            status="success",
            aeId=ae_id,
            notificationId=notification_id,
            snsPublished=sns_published,
            snsMessageId=sns_message_id,
            receivedAt=received_at,
        )
    except HTTPException:
        if conn is not None:
            conn.rollback()
        raise
    except Psycopg2Error as exc:
        if conn is not None:
            conn.rollback()
        _log_error("database_error", error=str(exc))
        raise HTTPException(status_code=status.HTTP_503_SERVICE_UNAVAILABLE, detail="Database Error")
    except Exception as exc:
        if conn is not None:
            conn.rollback()
        _log_error("unexpected_error", error=str(exc))
        raise HTTPException(status_code=status.HTTP_500_INTERNAL_SERVER_ERROR, detail="Internal Error")
    finally:
        if conn is not None:
            release_conn(conn)


def _parse_bool(value: str | None, field_name: str) -> bool | None:
    if value is None:
        return None
    lowered = value.lower()
    if lowered in {"true", "1", "yes"}:
        return True
    if lowered in {"false", "0", "no"}:
        return False
    _raise_validation(f"INVALID_{field_name.upper()}")
    return None


def _parse_int(value: str | None, field_name: str, minimum: int | None = None, maximum: int | None = None) -> int | None:
    if value is None:
        return None
    try:
        parsed = int(value)
    except ValueError:
        _raise_validation(f"INVALID_{field_name.upper()}")
        return None
    if minimum is not None and parsed < minimum:
        _raise_validation(f"INVALID_{field_name.upper()}")
    if maximum is not None and parsed > maximum:
        _raise_validation(f"INVALID_{field_name.upper()}")
    return parsed


def list_notifications(request: Request) -> NotificationListResponse:
    _log_json("route_entry", method=request.method, path=str(request.url.path))
    params = request.query_params
    trial_id = params.get("trialId")
    site_id = params.get("siteId")
    ctcae_grade = _parse_int(params.get("ctcaeGrade"), "ctcaeGrade", 1, 5)
    serious = _parse_bool(params.get("serious"), "serious")
    acknowledged = _parse_bool(params.get("acknowledged"), "acknowledged")
    priority = params.get("priority")
    if priority is not None and priority not in {"HIGH", "NORMAL"}:
        _raise_validation("INVALID_PRIORITY")
    date_from = params.get("dateFrom")
    date_to = params.get("dateTo")
    page = _parse_int(params.get("page"), "page", 1, None) or 1
    page_size = _parse_int(params.get("pageSize"), "pageSize", 1, 100) or 20
    conditions: list[str] = []
    values: list[Any] = []
    if trial_id is not None:
        conditions.append("trial_id = %s")
        values.append(trial_id)
    if site_id is not None:
        conditions.append("site_id = %s")
        values.append(site_id)
    if ctcae_grade is not None:
        conditions.append("ctcae_grade = %s")
        values.append(ctcae_grade)
    if serious is not None:
        conditions.append("serious = %s")
        values.append(serious)
    if acknowledged is not None:
        conditions.append("acknowledged = %s")
        values.append(acknowledged)
    if priority is not None:
        conditions.append("priority = %s")
        values.append(priority)
    if date_from is not None:
        conditions.append("created_at >= %s")
        values.append(date_from)
    if date_to is not None:
        conditions.append("created_at <= %s")
        values.append(date_to)
    where_clause = " WHERE " + " AND ".join(conditions) if conditions else ""
    conn = None
    try:
        conn = _retry_get_conn()
        with conn.cursor() as cursor:
            _log_json("db_select", table="ae_notifications", operation="SELECT")
            cursor.execute(f"SELECT COUNT(*) FROM ae_notifications{where_clause}", tuple(values))
            total_row = cursor.fetchone()
            total = int(total_row[0]) if total_row is not None else 0
            offset = (page - 1) * page_size
            cursor.execute(
                f"""
                SELECT notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name,
                       ctcae_grade, serious, priority, outcome, acknowledged, sns_published, created_at
                FROM ae_notifications
                {where_clause}
                ORDER BY created_at DESC
                LIMIT %s OFFSET %s
                """,
                tuple(values + [page_size, offset]),
            )
            rows = cursor.fetchall()
            notifications = [
                NotificationResponseItem(
                    notificationId=str(row[0]),
                    aeId=str(row[1]),
                    trialId=str(row[2]),
                    siteId=str(row[3]),
                    patientId=str(row[4]),
                    aeTermName=str(row[5]),
                    ctcaeGrade=int(row[6]),
                    serious=bool(row[7]),
                    priority=str(row[8]),
                    outcome=str(row[9]),
                    acknowledged=bool(row[10]),
                    snsPublished=bool(row[11]),
                    createdAt=row[12],
                )
                for row in rows
            ]
        return NotificationListResponse(status="success", total=total, page=page, pageSize=page_size, notifications=notifications)
    except HTTPException:
        raise
    except Psycopg2Error as exc:
        _log_error("database_error", error=str(exc))
        raise HTTPException(status_code=status.HTTP_503_SERVICE_UNAVAILABLE, detail="Database Error")
    except Exception as exc:
        _log_error("unexpected_error", error=str(exc))
        raise HTTPException(status_code=status.HTTP_500_INTERNAL_SERVER_ERROR, detail="Internal Error")
    finally:
        if conn is not None:
            release_conn(conn)

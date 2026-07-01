import json
import logging
import secrets
from datetime import datetime, timezone
from time import sleep
from typing import Any

import psycopg2
from fastapi import HTTPException, Request, status

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


def _log_json(message: str, **fields: Any) -> None:
    payload = {"message": message, **fields}
    logger.info(json.dumps(payload, default=str))


def _log_error(message: str, **fields: Any) -> None:
    payload = {"message": message, **fields}
    logger.error(json.dumps(payload, default=str))


def _utc_now() -> datetime:
    return datetime.now(timezone.utc)


def _retry_get_connection() -> Any:
    last_error: Exception | None = None
    for attempt in range(3):
        try:
            conn = get_conn()
            try:
                conn.rollback()
            except Exception:
                pass
            conn.autocommit = False
            return conn
        except Exception as exc:
            last_error = exc
            _log_error("DB connection failed", error=str(exc), attempt=attempt + 1)
            if attempt < 2:
                sleep(0.2)
    raise RuntimeError("DB connection failed") from last_error


def _validate_request(payload: AdverseEventCreateRequest) -> AdverseEventCreateRequest:
    if payload.ctcaeGrade < 1 or payload.ctcaeGrade > 5:
        _log_error("Validation failed", rule="INVALID_CTCAE_GRADE")
        raise HTTPException(status_code=status.HTTP_400_BAD_REQUEST, detail="Validation Error")
    if payload.outcome not in ALLOWED_OUTCOMES:
        _log_error("Validation failed", rule="INVALID_OUTCOME")
        raise HTTPException(status_code=status.HTTP_400_BAD_REQUEST, detail="Validation Error")
    if payload.actionTaken not in ALLOWED_ACTIONS:
        _log_error("Validation failed", rule="INVALID_ACTION_TAKEN")
        raise HTTPException(status_code=status.HTTP_400_BAD_REQUEST, detail="Validation Error")
    if len(payload.narrative) > 2000:
        _log_error("Validation failed", rule="NARRATIVE_TOO_LONG")
        raise HTTPException(status_code=status.HTTP_400_BAD_REQUEST, detail="Validation Error")
    return payload


def _coerce_payload(payload: AdverseEventCreateRequest) -> AdverseEventCreateRequest:
    serious = payload.serious or payload.ctcaeGrade >= 3
    outcome = "FATAL" if payload.ctcaeGrade == 5 else payload.outcome
    return payload.model_copy(update={"serious": serious, "outcome": outcome})


def _check_trial_and_patient(conn: Any, payload: AdverseEventCreateRequest) -> None:
    with conn.cursor() as cursor:
        _log_json("db operation", table="trials", operation="SELECT")
        cursor.execute("SELECT id FROM trials WHERE trial_id = %s AND status = 'ACTIVE'", (payload.trialId,))
        if cursor.fetchone() is None:
            raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Resource Not Found")

        _log_json("db operation", table="trial_enrolments", operation="SELECT")
        cursor.execute(
            "SELECT id FROM trial_enrolments WHERE trial_id = %s AND patient_id = %s AND status = 'ENROLLED'",
            (payload.trialId, payload.patientId),
        )
        if cursor.fetchone() is None:
            raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Resource Not Found")


def _check_duplicate(conn: Any, payload: AdverseEventCreateRequest) -> str | None:
    with conn.cursor() as cursor:
        _log_json("db operation", table="adverse_events", operation="SELECT")
        cursor.execute(
            "SELECT ae_id FROM adverse_events WHERE trial_id = %s AND patient_id = %s AND ae_term_code = %s AND ctcae_grade = %s AND submitted_at >= NOW() - INTERVAL '60 seconds' ORDER BY submitted_at DESC LIMIT 1",
            (payload.trialId, payload.patientId, payload.aeTermCode, payload.ctcaeGrade),
        )
        row = cursor.fetchone()
        if row is None:
            return None
        return str(row[0])


def _generate_ids(conn: Any) -> tuple[str, str]:
    with conn.cursor() as cursor:
        _log_json("db operation", table="ae_id_seq", operation="SELECT")
        cursor.execute("SELECT 'AE-' || TO_CHAR(NOW(),'YYYY') || '-' || LPAD(NEXTVAL('ae_id_seq')::text, 6, '0')")
        ae_row = cursor.fetchone()
        if ae_row is None:
            raise HTTPException(status_code=status.HTTP_500_INTERNAL_SERVER_ERROR, detail="Internal Error")
        ae_id = str(ae_row[0])

        _log_json("db operation", table="notif_id_seq", operation="SELECT")
        cursor.execute("SELECT 'NOTIF-' || TO_CHAR(NOW(),'YYYY') || '-' || LPAD(NEXTVAL('notif_id_seq')::text, 6, '0')")
        notif_row = cursor.fetchone()
        if notif_row is None:
            raise HTTPException(status_code=status.HTTP_500_INTERNAL_SERVER_ERROR, detail="Internal Error")
        notification_id = str(notif_row[0])
    return ae_id, notification_id


def _insert_records(conn: Any, payload: AdverseEventCreateRequest, ae_id: str, notification_id: str) -> None:
    submitted_at = _utc_now()
    priority = "HIGH" if payload.ctcaeGrade >= 3 else "NORMAL"
    with conn.cursor() as cursor:
        _log_json("db operation", table="adverse_events", operation="INSERT")
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
                submitted_at,
            ),
        )
        _log_json("db operation", table="ae_notifications", operation="INSERT")
        cursor.execute(
            "INSERT INTO ae_notifications (notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, outcome, priority, acknowledged, sns_published, sns_message_id, created_at, updated_at) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s::text, %s, %s, %s, NOW(), NOW())",
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


def _notification_stub() -> tuple[bool, None]:
    try:
        return False, None
    except Exception as exc:
        _log_error("Notification stub failed", error=str(exc))
        return False, None


def create_adverse_event(payload: AdverseEventCreateRequest, request: Request) -> AdverseEventCreateResponse:
    _log_json("route entry", method=request.method, path=str(request.url.path), trialId=payload.trialId, patientId=payload.patientId)
    payload = _validate_request(payload)
    payload = _coerce_payload(payload)
    conn = _retry_get_connection()
    try:
        _check_trial_and_patient(conn, payload)
        duplicate_ae_id = _check_duplicate(conn, payload)
        if duplicate_ae_id is not None:
            raise HTTPException(status_code=status.HTTP_409_CONFLICT, detail="Validation Error")
        ae_id, notification_id = _generate_ids(conn)
        try:
            _insert_records(conn, payload, ae_id, notification_id)
            conn.commit()
        except psycopg2.Error as exc:
            conn.rollback()
            _log_error("Database error", error=str(exc))
            raise HTTPException(status_code=status.HTTP_503_SERVICE_UNAVAILABLE, detail="Database Error") from exc
        except Exception as exc:
            conn.rollback()
            _log_error("Unexpected error", error=str(exc), exc_info=True)
            raise HTTPException(status_code=status.HTTP_500_INTERNAL_SERVER_ERROR, detail="Internal Error") from exc
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
    finally:
        release_conn(conn)


def _parse_bool(value: str) -> bool:
    lowered = value.lower()
    if lowered in {"true", "1", "yes"}:
        return True
    if lowered in {"false", "0", "no"}:
        return False
    raise HTTPException(status_code=status.HTTP_400_BAD_REQUEST, detail="Parsing Error")


def get_notifications(request: Request) -> NotificationListResponse:
    _log_json("route entry", method=request.method, path=str(request.url.path))
    params = request.query_params
    conditions: list[str] = []
    values: list[Any] = []
    try:
        trial_id = params.get("trialId")
        site_id = params.get("siteId")
        ctcae_grade = params.get("ctcaeGrade")
        serious = params.get("serious")
        acknowledged = params.get("acknowledged")
        priority = params.get("priority")
        date_from = params.get("dateFrom")
        date_to = params.get("dateTo")
        page = int(params.get("page", "1"))
        page_size = int(params.get("pageSize", "20"))
        if page < 1 or page_size < 1 or page_size > 100:
            raise HTTPException(status_code=status.HTTP_400_BAD_REQUEST, detail="Parsing Error")
        if trial_id:
            conditions.append("trial_id = %s")
            values.append(trial_id)
        if site_id:
            conditions.append("site_id = %s")
            values.append(site_id)
        if ctcae_grade is not None:
            grade_value = int(ctcae_grade)
            if grade_value < 1 or grade_value > 5:
                raise HTTPException(status_code=status.HTTP_400_BAD_REQUEST, detail="Parsing Error")
            conditions.append("ctcae_grade = %s")
            values.append(grade_value)
        if serious is not None:
            conditions.append("serious = %s")
            values.append(_parse_bool(serious))
        if acknowledged is not None:
            conditions.append("acknowledged = %s")
            values.append(_parse_bool(acknowledged))
        if priority is not None:
            if priority not in ALLOWED_PRIORITIES:
                raise HTTPException(status_code=status.HTTP_400_BAD_REQUEST, detail="Parsing Error")
            conditions.append("priority = %s")
            values.append(priority)
        if date_from is not None:
            conditions.append("created_at >= %s")
            values.append(date_from)
        if date_to is not None:
            conditions.append("created_at <= %s")
            values.append(date_to)
    except HTTPException:
        raise
    except Exception as exc:
        _log_error("Parsing failed", error=str(exc))
        raise HTTPException(status_code=status.HTTP_400_BAD_REQUEST, detail="Parsing Error") from exc

    where_clause = ""
    if conditions:
        where_clause = " WHERE " + " AND ".join(conditions)

    conn = _retry_get_connection()
    try:
        with conn.cursor() as cursor:
            count_sql = "SELECT COUNT(*) FROM ae_notifications" + where_clause
            _log_json("db operation", table="ae_notifications", operation="SELECT")
            cursor.execute(count_sql, tuple(values))
            total_row = cursor.fetchone()
            total = int(total_row[0]) if total_row is not None else 0

            data_sql = (
                "SELECT notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, outcome, priority, acknowledged, sns_published, created_at "
                "FROM ae_notifications"
                + where_clause
                + " ORDER BY created_at DESC LIMIT %s OFFSET %s"
            )
            _log_json("db operation", table="ae_notifications", operation="SELECT")
            cursor.execute(data_sql, tuple(values) + (page_size, (page - 1) * page_size))
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
                    priority=str(row[9]),
                    outcome=str(row[8]),
                    acknowledged=bool(row[10]),
                    snsPublished=bool(row[11]),
                    createdAt=row[12],
                )
                for row in rows
            ]
            return NotificationListResponse(status="success", total=total, page=page, pageSize=page_size, notifications=notifications)
    except psycopg2.Error as exc:
        _log_error("Database error", error=str(exc))
        raise HTTPException(status_code=status.HTTP_503_SERVICE_UNAVAILABLE, detail="Database Error") from exc
    except HTTPException:
        raise
    except Exception as exc:
        _log_error("Unexpected error", error=str(exc), exc_info=True)
        raise HTTPException(status_code=status.HTTP_500_INTERNAL_SERVER_ERROR, detail="Internal Error") from exc
    finally:
        release_conn(conn)

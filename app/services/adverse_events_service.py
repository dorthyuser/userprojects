import json
import os
import secrets
from datetime import UTC, datetime
from typing import Any

import psycopg2
from dateutil import parser as date_parser
from fastapi import HTTPException

from app.db.connection import get_conn, release_conn
from app.models.adverse_events_model import AdverseEventRecord, NotificationRecord
from app.schemas.adverse_events_schema import AdverseEventCreateRequest, AdverseEventCreateResponse, NotificationListResponse, NotificationResponse


def _required_env(name: str, default: str | None = None) -> str:
    value = os.environ.get(name, default)
    if value is None or value == "":
        raise RuntimeError(f"Missing required environment variable: {name}")
    return value


def _parse_utc_datetime(value: str) -> datetime:
    parsed = date_parser.isoparse(value)
    if parsed.tzinfo is None:
        raise HTTPException(status_code=422, detail="Validation Error")
    return parsed.astimezone(UTC)


def _validate_request(payload: AdverseEventCreateRequest) -> None:
    if payload.ctcaeGrade < 1 or payload.ctcaeGrade > 5:
        raise HTTPException(status_code=500, detail="Validation Error")
    if payload.outcome not in {"ONGOING", "RESOLVED", "FATAL", "UNKNOWN"}:
        raise HTTPException(status_code=500, detail="Validation Error")
    if payload.actionTaken not in {"NONE", "DOSE_REDUCED", "DRUG_WITHDRAWN", "HOSPITALISED"}:
        raise HTTPException(status_code=500, detail="Validation Error")
    if len(payload.narrative) > 2000:
        raise HTTPException(status_code=500, detail="Validation Error")


def _coerce_payload(payload: AdverseEventCreateRequest) -> AdverseEventCreateRequest:
    serious = True if payload.ctcaeGrade >= 3 else payload.serious
    outcome = "FATAL" if payload.ctcaeGrade == 5 else payload.outcome
    return payload.model_copy(update={"serious": serious, "outcome": outcome})


def _generate_identifier(prefix: str, year: str, sequence_value: int, width: int) -> str:
    return f"{prefix}-{year}-{str(sequence_value).zfill(width)}"


def create_adverse_event(payload: AdverseEventCreateRequest) -> AdverseEventCreateResponse:
    _validate_request(payload)
    payload = _coerce_payload(payload)
    event_date = _parse_utc_datetime(payload.eventDate)
    received_at = datetime.now(UTC)
    conn = None
    try:
        conn = get_conn()
        conn.rollback()
        conn.autocommit = False
        with conn.cursor() as cursor:
            cursor.execute("SELECT id FROM trials WHERE trial_id = %s AND status = 'ACTIVE'", (payload.trialId,))
            if cursor.fetchone() is None:
                raise HTTPException(status_code=502, detail="Service Unavailable")
            cursor.execute("SELECT id FROM trial_enrolments WHERE trial_id = %s AND patient_id = %s AND status = 'ENROLLED'", (payload.trialId, payload.patientId))
            if cursor.fetchone() is None:
                raise HTTPException(status_code=502, detail="Service Unavailable")
            window_s = int(_required_env("IDEMPOTENCY_WINDOW_S", "60"))
            cursor.execute("SELECT ae_id FROM adverse_events WHERE trial_id = %s AND patient_id = %s AND ae_term_code = %s AND ctcae_grade = %s AND submitted_at >= NOW() - (%s || ' seconds')::interval ORDER BY submitted_at DESC LIMIT 1", (payload.trialId, payload.patientId, payload.aeTermCode, payload.ctcaeGrade, str(window_s)))
            duplicate = cursor.fetchone()
            if duplicate is not None:
                raise HTTPException(status_code=409, detail="Service Unavailable")
            cursor.execute("SELECT 'AE-' || TO_CHAR(NOW(),'YYYY') || '-' || LPAD(NEXTVAL('ae_id_seq')::text, 6, '0')")
            ae_row = cursor.fetchone()
            if ae_row is None:
                raise HTTPException(status_code=500, detail="Internal Error")
            ae_id = ae_row[0]
            cursor.execute("SELECT 'NOTIF-' || TO_CHAR(NOW(),'YYYY') || '-' || LPAD(NEXTVAL('notif_id_seq')::text, 6, '0')")
            notif_row = cursor.fetchone()
            if notif_row is None:
                raise HTTPException(status_code=500, detail="Internal Error")
            notification_id = notif_row[0]
            adverse_event = AdverseEventRecord(ae_id=ae_id, trial_id=payload.trialId, site_id=payload.siteId, patient_id=payload.patientId, clinician_id=payload.clinicianId, event_date=event_date, ae_term_code=payload.aeTermCode, ae_term_name=payload.aeTermName, ctcae_grade=payload.ctcaeGrade, serious=payload.serious, outcome=payload.outcome, action_taken=payload.actionTaken, narrative=payload.narrative, related_drug_id=payload.relatedDrugId, reported_by=payload.reportedBy, submitted_at=received_at)
            notification = NotificationRecord(notification_id=notification_id, ae_id=ae_id, trial_id=payload.trialId, site_id=payload.siteId, patient_id=payload.patientId, ae_term_name=payload.aeTermName, ctcae_grade=payload.ctcaeGrade, serious=payload.serious, outcome=payload.outcome, priority="HIGH" if payload.ctcaeGrade >= 3 else "NORMAL", acknowledged=False, sns_published=False, created_at=received_at)
            cursor.execute("INSERT INTO adverse_events (ae_id, trial_id, site_id, patient_id, clinician_id, event_date, ae_term_code, ae_term_name, ctcae_grade, serious, outcome, action_taken, narrative, related_drug_id, reported_by, submitted_at, created_at, updated_at) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, NOW(), NOW()) RETURNING id", (adverse_event.ae_id, adverse_event.trial_id, adverse_event.site_id, adverse_event.patient_id, adverse_event.clinician_id, adverse_event.event_date, adverse_event.ae_term_code, adverse_event.ae_term_name, adverse_event.ctcae_grade, adverse_event.serious, adverse_event.outcome, adverse_event.action_taken, adverse_event.narrative, adverse_event.related_drug_id, adverse_event.reported_by, adverse_event.submitted_at))
            inserted = cursor.fetchone()
            if inserted is None:
                conn.rollback()
                raise HTTPException(status_code=500, detail="Internal Error")
            cursor.execute("INSERT INTO ae_notifications (notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, outcome, priority, acknowledged, sns_published, created_at, updated_at) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, NOW(), NOW()) RETURNING id", (notification.notification_id, notification.ae_id, notification.trial_id, notification.site_id, notification.patient_id, notification.ae_term_name, notification.ctcae_grade, notification.serious, notification.outcome, notification.priority, notification.acknowledged, notification.sns_published))
            notif_inserted = cursor.fetchone()
            if notif_inserted is None:
                conn.rollback()
                raise HTTPException(status_code=500, detail="Internal Error")
        conn.commit()
        # External notification publishing has been removed. The service stores notification records only.
        sns_published = False
        sns_message_id = None
        return AdverseEventCreateResponse(status="success", aeId=ae_id, notificationId=notification_id, snsPublished=sns_published, snsMessageId=sns_message_id, receivedAt=received_at)
    except HTTPException:
        if conn is not None:
            try:
                conn.rollback()
            except Exception:
                pass
        raise
    except psycopg2.Error as exc:
        if conn is not None:
            try:
                conn.rollback()
            except Exception:
                pass
        raise HTTPException(status_code=503, detail="Database Error") from exc
    except Exception as exc:
        if conn is not None:
            try:
                conn.rollback()
            except Exception:
                pass
        raise HTTPException(status_code=500, detail="Internal Error") from exc
    finally:
        if conn is not None:
            release_conn(conn)


def _parse_bool(value: bool | None) -> bool | None:
    return value


def get_notifications(trial_id: str | None, site_id: str | None, ctcae_grade: int | None, serious: bool | None, acknowledged: bool | None, priority: str | None, date_from: str | None, date_to: str | None, page: int, page_size: int) -> NotificationListResponse:
    if page < 1 or page_size < 1 or page_size > 100:
        raise HTTPException(status_code=502, detail="Service Unavailable")
    conditions: list[str] = []
    params: list[Any] = []
    if trial_id is not None:
        conditions.append("trial_id = %s")
        params.append(trial_id)
    if site_id is not None:
        conditions.append("site_id = %s")
        params.append(site_id)
    if ctcae_grade is not None:
        if ctcae_grade < 1 or ctcae_grade > 5:
            raise HTTPException(status_code=502, detail="Service Unavailable")
        conditions.append("ctcae_grade = %s")
        params.append(ctcae_grade)
    if serious is not None:
        conditions.append("serious = %s")
        params.append(serious)
    if acknowledged is not None:
        conditions.append("acknowledged = %s")
        params.append(acknowledged)
    if priority is not None:
        if priority not in {"HIGH", "NORMAL"}:
            raise HTTPException(status_code=502, detail="Service Unavailable")
        conditions.append("priority = %s")
        params.append(priority)
    if date_from is not None:
        conditions.append("created_at >= %s")
        params.append(_parse_utc_datetime(date_from))
    if date_to is not None:
        conditions.append("created_at <= %s")
        params.append(_parse_utc_datetime(date_to))
    where_clause = " WHERE " + " AND ".join(conditions) if conditions else ""
    offset = (page - 1) * page_size
    conn = None
    try:
        conn = get_conn()
        conn.rollback()
        with conn.cursor() as cursor:
            cursor.execute(f"SELECT COUNT(*) FROM ae_notifications{where_clause}", tuple(params))
            total_row = cursor.fetchone()
            total = int(total_row[0]) if total_row is not None else 0
            cursor.execute(f"SELECT notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, priority, outcome, acknowledged, sns_published, created_at FROM ae_notifications{where_clause} ORDER BY created_at DESC LIMIT %s OFFSET %s", tuple(params + [page_size, offset]))
            rows = cursor.fetchall()
        notifications = [NotificationResponse(notificationId=row[0], aeId=row[1], trialId=row[2], siteId=row[3], patientId=row[4], aeTermName=row[5], ctcaeGrade=row[6], serious=row[7], priority=row[8], outcome=row[9], acknowledged=row[10], snsPublished=row[11], createdAt=row[12]) for row in rows]
        return NotificationListResponse(status="success", total=total, page=page, pageSize=page_size, notifications=notifications)
    except HTTPException:
        raise
    except psycopg2.Error as exc:
        raise HTTPException(status_code=503, detail="Database Error") from exc
    except Exception as exc:
        raise HTTPException(status_code=500, detail="Internal Error") from exc
    finally:
        if conn is not None:
            release_conn(conn)

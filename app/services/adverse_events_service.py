from __future__ import annotations

import json
import logging
import secrets
from dataclasses import asdict
from datetime import datetime, timezone
from typing import Any

from fastapi import HTTPException, Request, status
from psycopg2 import Error as Psycopg2Error

from app.db.connection import get_conn, release_conn
from app.models.adverse_events_model import NotificationRecord
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


def _utc_now() -> datetime:
    return datetime.now(timezone.utc)


def _log(level: str, message: str, **fields: Any) -> None:
    payload = {"timestamp": _utc_now().isoformat(), "level": level, "message": message}
    payload.update(fields)
    logger.log(getattr(logging, level, logging.INFO), json.dumps(payload, default=str))


def _raise_validation(detail: str) -> None:
    _log("WARNING", "validation_failed", step="VALIDATION", outcome="FAILURE", error=detail)
    raise HTTPException(status_code=status.HTTP_422_UNPROCESSABLE_ENTITY, detail="Validation Error")


def _raise_parsing() -> None:
    _log("WARNING", "parsing_failed", step="VALIDATION", outcome="FAILURE", error="Parsing Error")
    raise HTTPException(status_code=status.HTTP_400_BAD_REQUEST, detail="Parsing Error")


def _coerce_payload(payload: AdverseEventCreateRequest) -> dict[str, Any]:
    if payload.ctcaeGrade >= 3:
        serious = True
        priority = "HIGH"
    else:
        serious = payload.serious
        priority = "NORMAL"
    outcome = "FATAL" if payload.ctcaeGrade == 5 else payload.outcome
    return {
        "trialId": payload.trialId,
        "siteId": payload.siteId,
        "patientId": payload.patientId,
        "clinicianId": payload.clinicianId,
        "eventDate": payload.eventDate,
        "aeTermCode": payload.aeTermCode,
        "aeTermName": payload.aeTermName,
        "ctcaeGrade": payload.ctcaeGrade,
        "serious": serious,
        "outcome": outcome,
        "actionTaken": payload.actionTaken,
        "narrative": payload.narrative,
        "relatedDrugId": payload.relatedDrugId,
        "reportedBy": payload.reportedBy,
        "priority": priority,
    }


def create_adverse_event(request: Request, payload: AdverseEventCreateRequest) -> AdverseEventCreateResponse:
    start = _utc_now()
    _log("INFO", "route_entry", step="VALIDATION", outcome="SUCCESS", trial_id=payload.trialId, patient_id=payload.patientId, ctcae_grade=payload.ctcaeGrade, serious=payload.serious)

    if payload.ctcaeGrade < 1 or payload.ctcaeGrade > 5:
        _raise_validation("INVALID_CTCAE_GRADE")
    if payload.outcome not in ALLOWED_OUTCOMES:
        _raise_validation("INVALID_OUTCOME")
    if payload.actionTaken not in ALLOWED_ACTIONS:
        _raise_validation("INVALID_ACTION_TAKEN")
    if len(payload.narrative) > 2000:
        _raise_validation("NARRATIVE_TOO_LONG")

    coerced = _coerce_payload(payload)
    conn = None
    try:
        conn = get_conn()
        conn.rollback()
        with conn.cursor() as cursor:
            _log("INFO", "db_select", step="DB_WRITE", outcome="SUCCESS", trial_id=coerced["trialId"], patient_id=coerced["patientId"])
            cursor.execute("SELECT id FROM trials WHERE trial_id = %s AND status = 'ACTIVE'", (coerced["trialId"],))
            trial_row = cursor.fetchone()
            if trial_row is None:
                raise HTTPException(status_code=status.HTTP_400_BAD_REQUEST, detail="Validation Error")

            cursor.execute(
                "SELECT id FROM trial_enrolments WHERE trial_id = %s AND patient_id = %s AND status = 'ENROLLED'",
                (coerced["trialId"], coerced["patientId"]),
            )
            patient_row = cursor.fetchone()
            if patient_row is None:
                raise HTTPException(status_code=status.HTTP_400_BAD_REQUEST, detail="Validation Error")

            cursor.execute(
                "SELECT ae_id FROM adverse_events WHERE trial_id = %s AND patient_id = %s AND ae_term_code = %s AND submitted_at >= NOW() - INTERVAL '60 seconds' ORDER BY submitted_at DESC LIMIT 1",
                (coerced["trialId"], coerced["patientId"], coerced["aeTermCode"]),
            )
            duplicate_row = cursor.fetchone()
            if duplicate_row is not None:
                existing_ae_id = duplicate_row[0]
                raise HTTPException(status_code=status.HTTP_409_CONFLICT, detail="Validation Error")

            year = _utc_now().year
            cursor.execute("SELECT nextval('ae_id_seq')")
            ae_seq_row = cursor.fetchone()
            if ae_seq_row is None:
                raise HTTPException(status_code=status.HTTP_500_INTERNAL_SERVER_ERROR, detail="Internal Error")
            ae_seq = int(ae_seq_row[0])
            ae_id = f"AE-{year}-{ae_seq:06d}"

            cursor.execute("SELECT nextval('notif_id_seq')")
            notif_seq_row = cursor.fetchone()
            if notif_seq_row is None:
                raise HTTPException(status_code=status.HTTP_500_INTERNAL_SERVER_ERROR, detail="Internal Error")
            notif_seq = int(notif_seq_row[0])
            notification_id = f"NOTIF-{year}-{notif_seq:06d}"

            submitted_at = _utc_now()
            _log("INFO", "db_insert", step="DB_WRITE", outcome="SUCCESS", ae_id=ae_id, notification_id=notification_id, trial_id=coerced["trialId"], patient_id=coerced["patientId"])
            cursor.execute(
                "INSERT INTO adverse_events (ae_id, trial_id, site_id, patient_id, clinician_id, event_date, ae_term_code, ae_term_name, ctcae_grade, serious, outcome, action_taken, narrative, related_drug_id, reported_by, submitted_at) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s) RETURNING id",
                (
                    ae_id,
                    coerced["trialId"],
                    coerced["siteId"],
                    coerced["patientId"],
                    coerced["clinicianId"],
                    coerced["eventDate"],
                    coerced["aeTermCode"],
                    coerced["aeTermName"],
                    coerced["ctcaeGrade"],
                    coerced["serious"],
                    coerced["outcome"],
                    coerced["actionTaken"],
                    coerced["narrative"],
                    coerced["relatedDrugId"],
                    coerced["reportedBy"],
                    submitted_at,
                ),
            )
            ae_insert_row = cursor.fetchone()
            if ae_insert_row is None:
                raise HTTPException(status_code=status.HTTP_500_INTERNAL_SERVER_ERROR, detail="Internal Error")

            cursor.execute(
                "INSERT INTO ae_notifications (notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, outcome, priority, acknowledged, acknowledged_by, acknowledged_at) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s) RETURNING id",
                (
                    notification_id,
                    ae_id,
                    coerced["trialId"],
                    coerced["siteId"],
                    coerced["patientId"],
                    coerced["aeTermName"],
                    coerced["ctcaeGrade"],
                    coerced["serious"],
                    coerced["outcome"],
                    coerced["priority"],
                    False,
                    None,
                    None,
                ),
            )
            notif_insert_row = cursor.fetchone()
            if notif_insert_row is None:
                raise HTTPException(status_code=status.HTTP_500_INTERNAL_SERVER_ERROR, detail="Internal Error")

            cursor.execute(
                "INSERT INTO ae_audit_log (ae_id, action, performed_by, sae, notes) VALUES (%s, %s, %s, %s, %s) RETURNING id",
                (
                    ae_id,
                    "CREATED",
                    coerced["reportedBy"],
                    coerced["serious"],
                    "AE created and notification stored",
                ),
            )
            audit_row = cursor.fetchone()
            if audit_row is None:
                raise HTTPException(status_code=status.HTTP_500_INTERNAL_SERVER_ERROR, detail="Internal Error")

            conn.commit()
            received_at = _utc_now().isoformat().replace("+00:00", "Z")
            duration_ms = int((_utc_now() - start).total_seconds() * 1000)
            _log("INFO", "request_complete", step="DB_WRITE", outcome="SUCCESS", ae_id=ae_id, notification_id=notification_id, trial_id=coerced["trialId"], patient_id=coerced["patientId"], duration_ms=duration_ms)
            return AdverseEventCreateResponse(
                status="success",
                aeId=ae_id,
                notificationId=notification_id,
                message="Adverse event recorded and notification stored.",
                receivedAt=received_at,
            )
    except HTTPException:
        if conn is not None:
            conn.rollback()
        raise
    except Psycopg2Error as exc:
        if conn is not None:
            conn.rollback()
        _log("ERROR", "database_error", step="DB_WRITE", outcome="FAILURE", error=str(exc), trial_id=coerced["trialId"], patient_id=coerced["patientId"])
        raise HTTPException(status_code=status.HTTP_503_SERVICE_UNAVAILABLE, detail="Database Error")
    except Exception as exc:
        if conn is not None:
            conn.rollback()
        _log("ERROR", "unexpected_error", step="DB_WRITE", outcome="FAILURE", error=str(exc), trial_id=coerced["trialId"], patient_id=coerced["patientId"], ae_id=None, notification_id=None)
        raise HTTPException(status_code=status.HTTP_500_INTERNAL_SERVER_ERROR, detail="Internal Error")
    finally:
        if conn is not None:
            release_conn(conn)


def list_notifications(request: Request) -> NotificationListResponse:
    params = dict(request.query_params)
    page = int(params.get("page", 1))
    page_size = int(params.get("pageSize", 20))
    if page_size > 100:
        raise HTTPException(status_code=status.HTTP_422_UNPROCESSABLE_ENTITY, detail="Validation Error")

    filters: list[str] = []
    values: list[Any] = []
    if params.get("trialId"):
        filters.append("trial_id = %s")
        values.append(params["trialId"])
    if params.get("siteId"):
        filters.append("site_id = %s")
        values.append(params["siteId"])
    if params.get("ctcaeGrade"):
        filters.append("ctcae_grade = %s")
        values.append(int(params["ctcaeGrade"]))
    if params.get("serious") is not None:
        filters.append("serious = %s")
        values.append(params["serious"].lower() == "true")
    if params.get("acknowledged") is not None:
        filters.append("acknowledged = %s")
        values.append(params["acknowledged"].lower() == "true")
    if params.get("priority"):
        filters.append("priority = %s")
        values.append(params["priority"])
    if params.get("dateFrom"):
        filters.append("created_at >= %s")
        values.append(params["dateFrom"])
    if params.get("dateTo"):
        filters.append("created_at <= %s")
        values.append(params["dateTo"])

    where_clause = " WHERE " + " AND ".join(filters) if filters else ""
    conn = None
    try:
        conn = get_conn()
        conn.rollback()
        with conn.cursor() as cursor:
            cursor.execute(f"SELECT COUNT(*) FROM ae_notifications{where_clause}", tuple(values))
            total_row = cursor.fetchone()
            total = int(total_row[0]) if total_row is not None else 0

            offset = (page - 1) * page_size
            cursor.execute(
                f"SELECT notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, priority, outcome, acknowledged, acknowledged_by, acknowledged_at, created_at FROM ae_notifications{where_clause} ORDER BY created_at DESC LIMIT %s OFFSET %s",
                tuple(values) + (page_size, offset),
            )
            rows = cursor.fetchall()
            notifications = [
                NotificationResponseItem(
                    notificationId=row[0],
                    aeId=row[1],
                    trialId=row[2],
                    siteId=row[3],
                    patientId=row[4],
                    aeTermName=row[5],
                    ctcaeGrade=row[6],
                    serious=row[7],
                    priority=row[8],
                    outcome=row[9],
                    acknowledged=row[10],
                    acknowledgedBy=row[11],
                    acknowledgedAt=row[12].isoformat().replace("+00:00", "Z") if row[12] else None,
                    createdAt=row[13].isoformat().replace("+00:00", "Z") if row[13] else None,
                )
                for row in rows
            ]
            return NotificationListResponse(status="success", total=total, page=page, pageSize=page_size, notifications=notifications)
    except Psycopg2Error as exc:
        _log("ERROR", "database_error", step="DB_READ", outcome="FAILURE", error=str(exc))
        raise HTTPException(status_code=status.HTTP_503_SERVICE_UNAVAILABLE, detail="Database Error")
    except Exception as exc:
        _log("ERROR", "unexpected_error", step="DB_READ", outcome="FAILURE", error=str(exc))
        raise HTTPException(status_code=status.HTTP_500_INTERNAL_SERVER_ERROR, detail="Internal Error")
    finally:
        if conn is not None:
            release_conn(conn)

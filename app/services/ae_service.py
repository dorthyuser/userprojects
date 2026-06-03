import json
import logging
import os
import secrets
from datetime import UTC, datetime
from typing import Any

import boto3
from dateutil import parser as date_parser
from fastapi import HTTPException
from psycopg2 import DatabaseError, IntegrityError

from app.db.connection import get_conn, release_conn
from app.models.ae_model import NotificationRecord
from app.schemas.ae_schema import AdverseEventCreateRequest, AdverseEventCreateResponse, NotificationListResponse

logger = logging.getLogger(__name__)
logger.setLevel(os.environ.get("LOG_LEVEL", "INFO"))

sns_client = boto3.client("sns", region_name=os.environ.get("AWS_REGION", "eu-west-2"))
SNS_TOPIC_ARN = os.environ.get("SNS_TOPIC_ARN", "")
IDEMPOTENCY_WINDOW_S = int(os.environ.get("IDEMPOTENCY_WINDOW_S", "60"))

ALLOWED_OUTCOMES = {"ONGOING", "RESOLVED", "FATAL", "UNKNOWN"}
ALLOWED_ACTIONS = {"NONE", "DOSE_REDUCED", "DRUG_WITHDRAWN", "HOSPITALISED"}
ALLOWED_PRIORITIES = {"HIGH", "NORMAL"}


def _log(event: str, **kwargs: Any) -> None:
    logger.info(json.dumps({"event": event, **kwargs}, default=str))


def _log_error(event: str, **kwargs: Any) -> None:
    logger.error(json.dumps({"event": event, **kwargs}, default=str))


def _parse_aware_datetime(value: str) -> datetime:
    parsed = date_parser.isoparse(value)
    if parsed.tzinfo is None:
        raise HTTPException(status_code=422, detail="date/time must be timezone-aware")
    return parsed.astimezone(UTC)


def _validate_post_payload(payload: AdverseEventCreateRequest) -> None:
    if payload.ctcaeGrade < 1 or payload.ctcaeGrade > 5:
        raise HTTPException(status_code=400, detail="INVALID_CTCAE_GRADE")
    if payload.outcome not in ALLOWED_OUTCOMES:
        raise HTTPException(status_code=400, detail="INVALID_OUTCOME")
    if payload.actionTaken not in ALLOWED_ACTIONS:
        raise HTTPException(status_code=400, detail="INVALID_ACTION_TAKEN")
    if len(payload.narrative) > 2000:
        raise HTTPException(status_code=400, detail="NARRATIVE_TOO_LONG")


def _coerce_payload(payload: AdverseEventCreateRequest) -> AdverseEventCreateRequest:
    serious = True if payload.ctcaeGrade >= 3 else payload.serious
    outcome = "FATAL" if payload.ctcaeGrade == 5 else payload.outcome
    return payload.model_copy(update={"serious": serious, "outcome": outcome})


def _get_existing_duplicate(cursor: Any, trial_id: str, patient_id: str, ae_term_code: str, ctcae_grade: int) -> str | None:
    _log("db_select", table="adverse_events", operation="SELECT")
    cursor.execute(
        """
        SELECT ae_id
        FROM adverse_events
        WHERE trial_id = %s
          AND patient_id = %s
          AND ae_term_code = %s
          AND ctcae_grade = %s
          AND submitted_at >= NOW() - (%s || ' seconds')::interval
        ORDER BY submitted_at DESC
        LIMIT 1
        """,
        (trial_id, patient_id, ae_term_code, ctcae_grade, IDEMPOTENCY_WINDOW_S),
    )
    row = cursor.fetchone()
    return row[0] if row else None


def create_adverse_event(payload: AdverseEventCreateRequest) -> AdverseEventCreateResponse:
    _log("service_start", operation="create_adverse_event", resource="adverse_event")
    _validate_post_payload(payload)
    coerced = _coerce_payload(payload)
    event_date = _parse_aware_datetime(coerced.eventDate)

    conn = None
    try:
        conn = get_conn()
        conn.rollback()
        conn.autocommit = False
        with conn.cursor() as cursor:
            _log("db_select", table="trials", operation="SELECT")
            cursor.execute(
                "SELECT id FROM trials WHERE trial_id = %s AND status = 'ACTIVE' LIMIT 1",
                (coerced.trialId,),
            )
            if cursor.fetchone() is None:
                raise HTTPException(status_code=404, detail="TRIAL_NOT_FOUND")

            _log("db_select", table="trial_enrolments", operation="SELECT")
            cursor.execute(
                "SELECT id FROM trial_enrolments WHERE trial_id = %s AND patient_id = %s AND status = 'ENROLLED' LIMIT 1",
                (coerced.trialId, coerced.patientId),
            )
            if cursor.fetchone() is None:
                raise HTTPException(status_code=404, detail="PATIENT_NOT_FOUND")

            duplicate_ae_id = _get_existing_duplicate(cursor, coerced.trialId, coerced.patientId, coerced.aeTermCode, coerced.ctcaeGrade)
            if duplicate_ae_id is not None:
                raise HTTPException(status_code=409, detail={"code": "DUPLICATE_AE", "aeId": duplicate_ae_id})

            _log("db_select", table="sequences", operation="SELECT")
            cursor.execute("SELECT 'AE-' || TO_CHAR(NOW(),'YYYY') || '-' || LPAD(NEXTVAL('ae_id_seq')::text, 6, '0')")
            ae_row = cursor.fetchone()
            if ae_row is None:
                raise RuntimeError("Failed to generate ae_id")
            ae_id = ae_row[0]

            _log("db_select", table="sequences", operation="SELECT")
            cursor.execute("SELECT 'NOTIF-' || TO_CHAR(NOW(),'YYYY') || '-' || LPAD(NEXTVAL('notif_id_seq')::text, 6, '0')")
            notif_row = cursor.fetchone()
            if notif_row is None:
                raise RuntimeError("Failed to generate notification_id")
            notification_id = notif_row[0]

            _log("db_insert", table="adverse_events", operation="INSERT")
            cursor.execute(
                """
                INSERT INTO adverse_events (
                    ae_id, trial_id, site_id, patient_id, clinician_id, event_date,
                    ae_term_code, ae_term_name, ctcae_grade, serious, outcome,
                    action_taken, narrative, related_drug_id, reported_by, submitted_at
                ) VALUES (
                    %s, %s, %s, %s, %s, %s,
                    %s, %s, %s, %s, %s,
                    %s, %s, %s, %s, NOW()
                )
                """,
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
                ),
            )

            priority = "HIGH" if coerced.ctcaeGrade >= 3 else "NORMAL"
            _log("db_insert", table="ae_notifications", operation="INSERT")
            cursor.execute(
                """
                INSERT INTO ae_notifications (
                    notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name,
                    ctcae_grade, serious, outcome, priority, acknowledged, sns_published, created_at
                ) VALUES (
                    %s, %s, %s, %s, %s, %s,
                    %s, %s, %s, %s::text, FALSE, FALSE, NOW()
                )
                """,
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
            conn.commit()

        sns_published = False
        sns_message_id: str | None = None
        try:
            if SNS_TOPIC_ARN:
                response = sns_client.publish(
                    TopicArn=SNS_TOPIC_ARN,
                    Message=json.dumps(
                        {
                            "aeId": ae_id,
                            "notificationId": notification_id,
                            "trialId": coerced.trialId,
                            "siteId": coerced.siteId,
                            "ctcaeGrade": coerced.ctcaeGrade,
                            "serious": coerced.serious,
                            "priority": "HIGH" if coerced.ctcaeGrade >= 3 else "NORMAL",
                        },
                        default=str,
                    ),
                )
                sns_message_id = response.get("MessageId")
                sns_published = True
                try:
                    conn = get_conn()
                    conn.rollback()
                    conn.autocommit = False
                    with conn.cursor() as cursor:
                        _log("db_update", table="ae_notifications", operation="UPDATE")
                        cursor.execute(
                            "UPDATE ae_notifications SET sns_published = TRUE, sns_message_id = %s WHERE notification_id = %s",
                            (sns_message_id, notification_id),
                        )
                    conn.commit()
                except Exception as update_exc:
                    _log_error("sns_message_id_update_failed", ae_id=ae_id, notification_id=notification_id, error=str(update_exc))
                    if conn is not None:
                        conn.rollback()
                finally:
                    if conn is not None:
                        release_conn(conn)
                        conn = None
            else:
                raise RuntimeError("SNS_TOPIC_ARN is not configured")
        except Exception as exc:
            sns_published = False
            sns_message_id = None
            _log_error("sns_publish_failed", ae_id=ae_id, notification_id=notification_id, error_class=exc.__class__.__name__, error=str(exc))

        received_at = datetime.now(UTC)
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
    except IntegrityError as exc:
        if conn is not None:
            conn.rollback()
        _log_error("db_integrity_error", error=str(exc))
        raise HTTPException(status_code=500, detail="DB_ERROR")
    except DatabaseError as exc:
        if conn is not None:
            conn.rollback()
        _log_error("db_error", error=str(exc))
        raise HTTPException(status_code=500, detail="DB_ERROR")
    except Exception as exc:
        if conn is not None:
            conn.rollback()
        _log_error("unexpected_error", error=str(exc))
        raise HTTPException(status_code=500, detail="DB_ERROR")
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
    _log("service_start", operation="get_notifications", resource="notification")
    if page < 1 or pageSize < 1 or pageSize > 100:
        raise HTTPException(status_code=400, detail="INVALID_QUERY_PARAM")
    if ctcaeGrade is not None and (ctcaeGrade < 1 or ctcaeGrade > 5):
        raise HTTPException(status_code=400, detail="INVALID_QUERY_PARAM")
    if priority is not None and priority not in ALLOWED_PRIORITIES:
        raise HTTPException(status_code=400, detail="INVALID_QUERY_PARAM")

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
    if dateFrom is not None:
        conditions.append("created_at >= %s")
        params.append(_parse_aware_datetime(dateFrom))
    if dateTo is not None:
        conditions.append("created_at <= %s")
        params.append(_parse_aware_datetime(dateTo))

    where_clause = " WHERE " + " AND ".join(conditions) if conditions else ""
    offset = (page - 1) * pageSize

    conn = None
    try:
        conn = get_conn()
        conn.rollback()
        conn.autocommit = True
        with conn.cursor() as cursor:
            _log("db_select", table="ae_notifications", operation="SELECT")
            cursor.execute(f"SELECT COUNT(*) FROM ae_notifications{where_clause}", tuple(params))
            total_row = cursor.fetchone()
            total = int(total_row[0]) if total_row else 0

            _log("db_select", table="ae_notifications", operation="SELECT")
            cursor.execute(
                f"""
                SELECT notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name,
                       ctcae_grade, serious, priority, outcome, acknowledged, sns_published, created_at
                FROM ae_notifications
                {where_clause}
                ORDER BY created_at DESC
                LIMIT %s OFFSET %s
                """,
                tuple(params + [pageSize, offset]),
            )
            rows = cursor.fetchall() or []
            notifications = [
                NotificationRecord(
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
                    snsPublished=row[11],
                    createdAt=row[12],
                )
                for row in rows
            ]
        return NotificationListResponse(status="success", total=total, page=page, pageSize=pageSize, notifications=notifications)
    except HTTPException:
        raise
    except Exception as exc:
        _log_error("db_query_error", error=str(exc))
        raise HTTPException(status_code=500, detail="DB_ERROR")
    finally:
        if conn is not None:
            release_conn(conn)

import json
import logging
import os
from datetime import datetime, timezone
from typing import Optional

import boto3
import psycopg2

from app.db.connection import get_conn, release_conn
from app.schemas.adverse_events_schema import (
    AdverseEventCreateRequest,
    AdverseEventCreateResponse,
    NotificationListResponse,
    NotificationResponse,
)

logger = logging.getLogger(__name__)

SNS_TOPIC_ARN         = os.environ.get("SNS_TOPIC_ARN", "")
IDEMPOTENCY_WINDOW_S  = int(os.environ.get("IDEMPOTENCY_WINDOW_S", "60"))

_sns_client = boto3.client("sns")


# ── Helpers ────────────────────────────────────────────────────────────────────

def _row_to_notification(row: tuple) -> NotificationResponse:
    return NotificationResponse(
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


# ── POST — Submit Adverse Event ────────────────────────────────────────────────

def create_adverse_event(payload: AdverseEventCreateRequest) -> AdverseEventCreateResponse:
    received_at = datetime.now(timezone.utc)

    # Step 2 — CTCAE coercion (BEFORE any DB operation)
    if payload.ctcaeGrade >= 3:
        payload.serious = True
    if payload.ctcaeGrade == 5:
        payload.outcome = "FATAL"

    conn = get_conn()
    try:
        conn.autocommit = False

        with conn.cursor() as cur:

            # Step 3 — Trial existence check
            cur.execute(
                "SELECT id FROM trials WHERE trial_id = %s AND status = 'ACTIVE'",
                (payload.trialId,),
            )
            if cur.fetchone() is None:
                raise LookupError("TRIAL_NOT_FOUND")

            # Step 4 — Patient enrolment check
            cur.execute(
                "SELECT id FROM trial_enrolments WHERE trial_id = %s AND patient_id = %s AND status = 'ENROLLED'",
                (payload.trialId, payload.patientId),
            )
            if cur.fetchone() is None:
                raise LookupError("PATIENT_NOT_FOUND")

            # Step 5 — Idempotency guard
            cur.execute(
                """
                SELECT ae_id FROM adverse_events
                WHERE trial_id     = %s
                  AND patient_id   = %s
                  AND ae_term_code = %s
                  AND ctcae_grade  = %s
                  AND submitted_at >= NOW() - (%s || ' seconds')::INTERVAL
                LIMIT 1
                """,
                (payload.trialId, payload.patientId, payload.aeTermCode,
                 payload.ctcaeGrade, IDEMPOTENCY_WINDOW_S),
            )
            dup = cur.fetchone()
            if dup:
                raise ValueError(f"DUPLICATE_AE:{dup[0]}")

            # Step 6 — Generate AE ID
            cur.execute(
                "SELECT 'AE-' || TO_CHAR(NOW(),'YYYY') || '-' || LPAD(NEXTVAL('ae_id_seq')::text, 6, '0')"
            )
            ae_id: str = cur.fetchone()[0]

            # Step 7 — Generate Notification ID
            cur.execute(
                "SELECT 'NOTIF-' || TO_CHAR(NOW(),'YYYY') || '-' || LPAD(NEXTVAL('notif_id_seq')::text, 6, '0')"
            )
            notification_id: str = cur.fetchone()[0]

            # Step 8-9 — INSERT adverse_events (inside transaction)
            cur.execute(
                """
                INSERT INTO adverse_events (
                    ae_id, trial_id, site_id, patient_id, clinician_id,
                    event_date, ae_term_code, ae_term_name, ctcae_grade,
                    serious, outcome, action_taken, narrative,
                    related_drug_id, reported_by, submitted_at
                ) VALUES (
                    %s,%s,%s,%s,%s,
                    %s,%s,%s,%s,
                    %s,%s,%s,%s,
                    %s,%s,%s
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
                    str(payload.reportedBy),
                    received_at,
                ),
            )

            # Step 10 — INSERT ae_notifications (inside same transaction)
            priority = "HIGH" if payload.ctcaeGrade >= 3 else "NORMAL"
            cur.execute(
                """
                INSERT INTO ae_notifications (
                    notification_id, ae_id, trial_id, site_id, patient_id,
                    ae_term_name, ctcae_grade, serious, outcome, priority,
                    acknowledged, sns_published
                ) VALUES (
                    %s,%s,%s,%s,%s,
                    %s,%s,%s,%s,%s,
                    %s,%s
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
                    False,
                    False,
                ),
            )

        # Step 11 — Commit both INSERTs
        conn.commit()
        logger.info(json.dumps({
            "event": "ae_created",
            "ae_id": ae_id,
            "notification_id": notification_id,
        }))

    except Exception:
        conn.rollback()
        raise
    finally:
        release_conn(conn)

    # Step 12 — SNS best-effort publish (OUTSIDE transaction)
    sns_published  = False
    sns_message_id: Optional[str] = None
    try:
        resp = _sns_client.publish(
            TopicArn=SNS_TOPIC_ARN,
            Message=json.dumps({
                "aeId":           ae_id,
                "notificationId": notification_id,
                "trialId":        payload.trialId,
                "patientId":      payload.patientId,
                "ctcaeGrade":     payload.ctcaeGrade,
                "serious":        payload.serious,
                "priority":       priority,
            }),
            Subject="AdverseEventAlert",
        )
        sns_message_id = resp.get("MessageId")
        sns_published  = True

        # Best-effort update of sns_published flag
        try:
            upd_conn = get_conn()
            try:
                with upd_conn.cursor() as cur:
                    cur.execute(
                        "UPDATE ae_notifications SET sns_published = TRUE, sns_message_id = %s WHERE notification_id = %s",
                        (sns_message_id, notification_id),
                    )
                upd_conn.commit()
            finally:
                release_conn(upd_conn)
        except Exception as upd_exc:
            logger.warning(json.dumps({
                "event":           "sns_flag_update_failed",
                "ae_id":           ae_id,
                "notification_id": notification_id,
                "error":           str(upd_exc),
            }))

    except Exception as sns_exc:
        logger.error(json.dumps({
            "event":           "sns_publish_failed",
            "ae_id":           ae_id,
            "notification_id": notification_id,
            "error_class":     type(sns_exc).__name__,
            "error":           str(sns_exc),
        }))
        # NEVER re-raise — SNS failure must not break the 201 response

    # Step 13 — Return HTTP 201 response
    return AdverseEventCreateResponse(
        aeId=ae_id,
        notificationId=notification_id,
        snsPublished=sns_published,
        snsMessageId=sns_message_id,
        receivedAt=received_at,
    )


# ── GET — Retrieve Notifications ───────────────────────────────────────────────

def get_notifications(
    trial_id:     Optional[str]  = None,
    site_id:      Optional[str]  = None,
    ctcae_grade:  Optional[int]  = None,
    serious:      Optional[bool] = None,
    acknowledged: Optional[bool] = None,
    priority:     Optional[str]  = None,
    date_from:    Optional[datetime] = None,
    date_to:      Optional[datetime] = None,
    page:         int = 1,
    page_size:    int = 20,
) -> NotificationListResponse:

    # Step 1 — Build parameterised WHERE clause — NEVER format user input into SQL
    conditions: list[str] = []
    params:     list      = []

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
    if date_from is not None:
        conditions.append("created_at >= %s")
        params.append(date_from)
    if date_to is not None:
        conditions.append("created_at <= %s")
        params.append(date_to)

    where = ("WHERE " + " AND ".join(conditions)) if conditions else ""
    offset = (page - 1) * page_size

    conn = get_conn()
    try:
        with conn.cursor() as cur:

            # Step 3 — COUNT query
            cur.execute(f"SELECT COUNT(*) FROM ae_notifications {where}", params)
            total: int = cur.fetchone()[0]

            # Step 4 — Data query with pagination
            cur.execute(
                f"""
                SELECT notification_id, ae_id, trial_id, site_id, patient_id,
                       ae_term_name, ctcae_grade, serious, priority, outcome,
                       acknowledged, sns_published, created_at
                FROM ae_notifications
                {where}
                ORDER BY created_at DESC
                LIMIT %s OFFSET %s
                """,
                params + [page_size, offset],
            )
            rows = cur.fetchall()

        notifications = [_row_to_notification(r) for r in rows]
        return NotificationListResponse(
            total=total,
            page=page,
            pageSize=page_size,
            notifications=notifications,
        )
    finally:
        release_conn(conn)

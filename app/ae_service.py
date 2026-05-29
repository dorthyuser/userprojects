from __future__ import annotations

import json
import logging
import os
import time
from dataclasses import dataclass
from datetime import datetime, timezone
from typing import Any

import boto3
from botocore.exceptions import BotoCoreError, ClientError
from dateutil.parser import isoparse
from fastapi import HTTPException, status
from psycopg2 import DatabaseError, OperationalError
from psycopg2.extras import RealDictCursor

from app.db.connection import get_conn, release_conn
from app.models.ae_model import NotificationRecord
from app.schemas.ae_schema import NotificationItem, NotificationListResponse, SubmitAdverseEventResponse

logger = logging.getLogger(__name__)
_sns_client = boto3.client("sns", region_name=os.environ.get("AWS_REGION"))


@dataclass(slots=True)
class AeDependencies:
    sns_topic_arn: str | None
    idem_window_s: int


class AdverseEventService:
    def __init__(self, deps: AeDependencies | None = None) -> None:
        self.deps = deps or AeDependencies(
            sns_topic_arn=os.environ.get("SNS_TOPIC_ARN"),
            idem_window_s=int(os.environ.get("IDEMPOTENCY_WINDOW_S", "60")),
        )

    def _log_db(self, table: str, operation: str) -> None:
        logger.info(json.dumps({"event": "db_operation", "table": table, "operation": operation}))

    def _coerce_payload(self, payload) -> Any:
        if payload.ctcaeGrade >= 3:
            payload.serious = True
        if payload.ctcaeGrade == 5:
            payload.outcome = "FATAL"
        return payload

    def submit_adverse_event(self, payload) -> SubmitAdverseEventResponse:
        logger.info(json.dumps({"event": "service_start", "operation": "submit_adverse_event", "resource": "adverse_event"}))
        payload = self._coerce_payload(payload)
        conn = None
        try:
            conn = get_conn()
            conn.autocommit = False
            with conn.cursor(cursor_factory=RealDictCursor) as cur:
                self._log_db("trials", "SELECT")
                cur.execute("SELECT id FROM trials WHERE trial_id = %s AND status = 'ACTIVE'", (payload.trialId,))
                if cur.fetchone() is None:
                    raise HTTPException(status_code=404, detail={"code": "TRIAL_NOT_FOUND", "message": "trial not found"})

                self._log_db("trial_enrolments", "SELECT")
                cur.execute("SELECT id FROM trial_enrolments WHERE trial_id = %s AND patient_id = %s AND status = 'ENROLLED'", (payload.trialId, payload.patientId))
                if cur.fetchone() is None:
                    raise HTTPException(status_code=404, detail={"code": "PATIENT_NOT_FOUND", "message": "patient not found"})

                self._log_db("adverse_events", "SELECT")
                cur.execute(
                    "SELECT ae_id FROM adverse_events WHERE trial_id = %s AND patient_id = %s AND ae_term_code = %s AND ctcae_grade = %s AND submitted_at >= NOW() - (%s * INTERVAL '1 second') ORDER BY submitted_at DESC LIMIT 1",
                    (payload.trialId, payload.patientId, payload.aeTermCode, payload.ctcaeGrade, self.deps.idem_window_s),
                )
                existing = cur.fetchone()
                if existing is not None:
                    raise HTTPException(status_code=409, detail={"code": "DUPLICATE_AE", "message": "duplicate adverse event"})

                self._log_db("ae_id_seq", "SELECT")
                cur.execute("SELECT 'AE-' || TO_CHAR(NOW(),'YYYY') || '-' || LPAD(NEXTVAL('ae_id_seq')::text, 6, '0')")
                ae_row = cur.fetchone()
                if ae_row is None:
                    conn.rollback()
                    raise HTTPException(status_code=500, detail="Internal server error")
                ae_id = list(ae_row.values())[0]

                self._log_db("notif_id_seq", "SELECT")
                cur.execute("SELECT 'NOTIF-' || TO_CHAR(NOW(),'YYYY') || '-' || LPAD(NEXTVAL('notif_id_seq')::text, 6, '0')")
                notif_row = cur.fetchone()
                if notif_row is None:
                    conn.rollback()
                    raise HTTPException(status_code=500, detail="Internal server error")
                notification_id = list(notif_row.values())[0]

                self._log_db("adverse_events", "INSERT")
                cur.execute(
                    "INSERT INTO adverse_events (ae_id, trial_id, site_id, patient_id, clinician_id, event_date, ae_term_code, ae_term_name, ctcae_grade, serious, outcome, action_taken, narrative, related_drug_id, reported_by) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s)",
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

                priority = "HIGH" if payload.ctcaeGrade >= 3 else "NORMAL"
                self._log_db("ae_notifications", "INSERT")
                cur.execute(
                    "INSERT INTO ae_notifications (notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, outcome, priority, acknowledged, sns_published) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s)",
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

                conn.commit()
        except HTTPException:
            if conn is not None:
                conn.rollback()
            raise
        except (OperationalError, DatabaseError) as exc:
            if conn is not None:
                conn.rollback()
            logger.error(json.dumps({"event": "db_error", "message": str(exc)}))
            raise HTTPException(status_code=500, detail="Internal server error") from None
        finally:
            if conn is not None:
                release_conn(conn)

        sns_published = False
        sns_message_id = None
        try:
            if self.deps.sns_topic_arn:
                response = _sns_client.publish(TopicArn=self.deps.sns_topic_arn, Message=json.dumps({"aeId": ae_id, "notificationId": notification_id}))
                sns_message_id = response.get("MessageId")
                sns_published = True
                try:
                    conn = get_conn()
                    with conn.cursor() as cur:
                        self._log_db("ae_notifications", "UPDATE")
                        cur.execute("UPDATE ae_notifications SET sns_published = %s, sns_message_id = %s WHERE notification_id = %s", (True, sns_message_id, notification_id))
                    conn.commit()
                except Exception as exc:
                    if conn is not None:
                        conn.rollback()
                    logger.warning(json.dumps({"event": "sns_update_failed", "ae_id": ae_id, "notification_id": notification_id, "error": str(exc)}))
                finally:
                    if conn is not None:
                        release_conn(conn)
        except (BotoCoreError, ClientError, Exception) as exc:
            sns_published = False
            sns_message_id = None
            logger.error(json.dumps({"event": "sns_publish_failed", "ae_id": ae_id, "notification_id": notification_id, "error_class": exc.__class__.__name__, "message": str(exc)}))

        received_at = datetime.now(timezone.utc)
        return SubmitAdverseEventResponse(status="success", aeId=ae_id, notificationId=notification_id, snsPublished=sns_published, snsMessageId=sns_message_id, receivedAt=received_at)

    def get_notifications(self, **filters) -> NotificationListResponse:
        logger.info(json.dumps({"event": "service_start", "operation": "get_notifications", "resource": "adverse_notification"}))
        page = int(filters.get("page", 1))
        page_size = int(filters.get("pageSize", 20))
        if page < 1 or page_size < 1 or page_size > 100:
            raise HTTPException(status_code=400, detail="invalid pagination")
        conditions: list[str] = []
        params: list[Any] = []
        if filters.get("trialId") is not None:
            conditions.append("trial_id = %s")
            params.append(filters["trialId"])
        if filters.get("siteId") is not None:
            conditions.append("site_id = %s")
            params.append(filters["siteId"])
        if filters.get("ctcaeGrade") is not None:
            if not isinstance(filters["ctcaeGrade"], int) or filters["ctcaeGrade"] < 1 or filters["ctcaeGrade"] > 5:
                raise HTTPException(status_code=400, detail="INVALID_QUERY_PARAM")
            conditions.append("ctcae_grade = %s")
            params.append(filters["ctcaeGrade"])
        if filters.get("serious") is not None:
            conditions.append("serious = %s")
            params.append(filters["serious"])
        if filters.get("acknowledged") is not None:
            conditions.append("acknowledged = %s")
            params.append(filters["acknowledged"])
        if filters.get("priority") is not None:
            if filters["priority"] not in {"HIGH", "NORMAL"}:
                raise HTTPException(status_code=400, detail="INVALID_QUERY_PARAM")
            conditions.append("priority = %s")
            params.append(filters["priority"])
        if filters.get("dateFrom") is not None:
            conditions.append("created_at >= %s")
            params.append(isoparse(filters["dateFrom"]))
        if filters.get("dateTo") is not None:
            conditions.append("created_at <= %s")
            params.append(isoparse(filters["dateTo"]))
        where_clause = f" WHERE {' AND '.join(conditions)}" if conditions else ""
        conn = None
        try:
            conn = get_conn()
            with conn.cursor(cursor_factory=RealDictCursor) as cur:
                self._log_db("ae_notifications", "SELECT")
                cur.execute(f"SELECT COUNT(*) AS total FROM ae_notifications{where_clause}", tuple(params))
                total = cur.fetchone()["total"]
                self._log_db("ae_notifications", "SELECT")
                cur.execute(
                    f"SELECT notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, priority, outcome, acknowledged, sns_published, created_at FROM ae_notifications{where_clause} ORDER BY created_at DESC LIMIT %s OFFSET %s",
                    tuple(params + [page_size, (page - 1) * page_size]),
                )
                rows = cur.fetchall()
            notifications = [
                NotificationItem(
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
        except (OperationalError, DatabaseError) as exc:
            logger.error(json.dumps({"event": "db_error", "message": str(exc)}))
            raise HTTPException(status_code=500, detail="Internal server error") from None
        finally:
            if conn is not None:
                release_conn(conn)


def get_ae_service() -> AdverseEventService:
    return AdverseEventService()

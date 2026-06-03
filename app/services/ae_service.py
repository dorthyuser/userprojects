import json
import logging
import os
from dataclasses import asdict
from datetime import datetime, timezone
from typing import Any

import boto3
from botocore.exceptions import BotoCoreError, ClientError
from dateutil.parser import isoparse
from fastapi import HTTPException, status
from psycopg2 import DatabaseError, IntegrityError

from app.db.connection import get_conn, release_conn
from app.models.ae_model import NotificationRecord
from app.schemas.ae_schema import AdverseEventCreateRequest, AdverseEventCreateResponse, NotificationListResponse, NotificationResponse

logger = logging.getLogger(__name__)
_sns_client = None


def _required_env(name: str) -> str:
    value = os.environ.get(name)
    if not value:
        logger.error(json.dumps({"event": "missing_env", "variable": name}))
        raise RuntimeError(f"Missing required environment variable: {name}")
    return value


class AEService:
    def __init__(self, sns_client: Any | None = None) -> None:
        self.sns_client = sns_client or _sns_client
        self.topic_arn = _required_env("SNS_TOPIC_ARN")
        self.idempotency_window_s = int(os.environ.get("IDEMPOTENCY_WINDOW_S", "60"))

    def submit_adverse_event(self, payload: AdverseEventCreateRequest) -> AdverseEventCreateResponse:
        logger.info(json.dumps({"event": "service_start", "operation": "submit_adverse_event", "resource": "adverse-events"}))
        coerced = payload.model_copy(update={"serious": True if payload.ctcaeGrade >= 3 else payload.serious, "outcome": "FATAL" if payload.ctcaeGrade == 5 else payload.outcome})
        conn = None
        try:
            conn = get_conn()
            try:
                conn.rollback()
            except Exception:
                pass
            conn.autocommit = False
            with conn.cursor() as cursor:
                logger.info(json.dumps({"event": "db_operation", "table": "trials", "operation": "SELECT"}))
                cursor.execute("SELECT id FROM trials WHERE trial_id = %s AND status = 'ACTIVE'", (coerced.trialId,))
                if cursor.fetchone() is None:
                    raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail={"code": "TRIAL_NOT_FOUND", "message": "Trial not found"})

                logger.info(json.dumps({"event": "db_operation", "table": "trial_enrolments", "operation": "SELECT"}))
                cursor.execute("SELECT id FROM trial_enrolments WHERE trial_id = %s AND patient_id = %s AND status = 'ENROLLED'", (coerced.trialId, coerced.patientId))
                if cursor.fetchone() is None:
                    raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail={"code": "PATIENT_NOT_FOUND", "message": "Patient not found"})

                logger.info(json.dumps({"event": "db_operation", "table": "adverse_events", "operation": "SELECT"}))
                cursor.execute(
                    "SELECT ae_id FROM adverse_events WHERE trial_id = %s AND patient_id = %s AND ae_term_code = %s AND ctcae_grade = %s AND submitted_at >= NOW() - (%s || ' seconds')::interval ORDER BY submitted_at DESC LIMIT 1",
                    (coerced.trialId, coerced.patientId, coerced.aeTermCode, coerced.ctcaeGrade, self.idempotency_window_s),
                )
                duplicate = cursor.fetchone()
                if duplicate is not None:
                    raise HTTPException(status_code=status.HTTP_409_CONFLICT, detail={"code": "DUPLICATE_AE", "message": "Duplicate adverse event"})

                logger.info(json.dumps({"event": "db_operation", "table": "adverse_events", "operation": "INSERT"}))
                cursor.execute("SELECT 'AE-' || TO_CHAR(NOW(),'YYYY') || '-' || LPAD(NEXTVAL('ae_id_seq')::text, 6, '0')")
                ae_id_row = cursor.fetchone()
                if ae_id_row is None:
                    conn.rollback()
                    raise HTTPException(status_code=status.HTTP_500_INTERNAL_SERVER_ERROR, detail="Internal server error")
                ae_id = ae_id_row[0]

                logger.info(json.dumps({"event": "db_operation", "table": "ae_notifications", "operation": "INSERT"}))
                cursor.execute("SELECT 'NOTIF-' || TO_CHAR(NOW(),'YYYY') || '-' || LPAD(NEXTVAL('notif_id_seq')::text, 6, '0')")
                notif_id_row = cursor.fetchone()
                if notif_id_row is None:
                    conn.rollback()
                    raise HTTPException(status_code=status.HTTP_500_INTERNAL_SERVER_ERROR, detail="Internal server error")
                notification_id = notif_id_row[0]

                submitted_at = datetime.now(timezone.utc)
                priority = "HIGH" if coerced.ctcaeGrade >= 3 else "NORMAL"
                logger.info(json.dumps({"event": "db_operation", "table": "adverse_events", "operation": "INSERT"}))
                cursor.execute(
                    "INSERT INTO adverse_events (ae_id, trial_id, site_id, patient_id, clinician_id, event_date, ae_term_code, ae_term_name, ctcae_grade, serious, outcome, action_taken, narrative, related_drug_id, reported_by, submitted_at, created_at, updated_at) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, NOW(), NOW())",
                    (ae_id, coerced.trialId, coerced.siteId, coerced.patientId, coerced.clinicianId, coerced.eventDate, coerced.aeTermCode, coerced.aeTermName, coerced.ctcaeGrade, coerced.serious, coerced.outcome, coerced.actionTaken, coerced.narrative, coerced.relatedDrugId, coerced.reportedBy, submitted_at),
                )
                logger.info(json.dumps({"event": "db_operation", "table": "ae_notifications", "operation": "INSERT"}))
                cursor.execute(
                    "INSERT INTO ae_notifications (notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, outcome, priority, acknowledged, sns_published, created_at, updated_at) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, FALSE, FALSE, NOW(), NOW())",
                    (notification_id, ae_id, coerced.trialId, coerced.siteId, coerced.patientId, coerced.aeTermName, coerced.ctcaeGrade, coerced.serious, coerced.outcome, priority),
                )
                conn.commit()
        except HTTPException:
            if conn is not None:
                conn.rollback()
            raise
        except (IntegrityError, DatabaseError) as exc:
            if conn is not None:
                conn.rollback()
            logger.error(json.dumps({"event": "db_error", "error": str(exc)}))
            raise HTTPException(status_code=status.HTTP_500_INTERNAL_SERVER_ERROR, detail="Internal server error") from None
        except Exception as exc:
            if conn is not None:
                conn.rollback()
            logger.error(json.dumps({"event": "unexpected_error", "error": str(exc)}))
            raise HTTPException(status_code=status.HTTP_500_INTERNAL_SERVER_ERROR, detail="Internal server error") from None
        finally:
            if conn is not None:
                release_conn(conn)

        sns_published = False
        sns_message_id = None
        try:
            response = self.sns_client.publish(TopicArn=self.topic_arn, Message=json.dumps({"aeId": ae_id, "notificationId": notification_id}))
            sns_message_id = response.get("MessageId")
            sns_published = True
            conn = None
            try:
                conn = get_conn()
                try:
                    conn.rollback()
                except Exception:
                    pass
                conn.autocommit = False
                with conn.cursor() as cursor:
                    logger.info(json.dumps({"event": "db_operation", "table": "ae_notifications", "operation": "UPDATE"}))
                    cursor.execute("UPDATE ae_notifications SET sns_published = TRUE, sns_message_id = %s, updated_at = NOW() WHERE notification_id = %s", (sns_message_id, notification_id))
                conn.commit()
            except Exception as exc:
                if conn is not None:
                    conn.rollback()
                logger.warning(json.dumps({"event": "sns_message_id_update_failed", "ae_id": ae_id, "notification_id": notification_id, "error": str(exc)}))
            finally:
                if conn is not None:
                    release_conn(conn)
        except (BotoCoreError, ClientError, Exception) as exc:
            logger.error(json.dumps({"event": "sns_publish_failed", "ae_id": ae_id, "notification_id": notification_id, "error": str(exc), "error_class": exc.__class__.__name__}))
            sns_published = False
            sns_message_id = None

        return AdverseEventCreateResponse(status="success", aeId=ae_id, notificationId=notification_id, snsPublished=sns_published, snsMessageId=sns_message_id, receivedAt=datetime.now(timezone.utc))

    def get_notifications(
        self,
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
        logger.info(json.dumps({"event": "service_start", "operation": "get_notifications", "resource": "adverse-events-notifications"}))
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
            if priority not in {"HIGH", "NORMAL"}:
                raise HTTPException(status_code=status.HTTP_400_BAD_REQUEST, detail={"code": "INVALID_QUERY_PARAM", "message": "Invalid priority"})
            conditions.append("priority = %s")
            params.append(priority)
        if dateFrom is not None:
            conditions.append("created_at >= %s")
            params.append(isoparse(dateFrom))
        if dateTo is not None:
            conditions.append("created_at <= %s")
            params.append(isoparse(dateTo))
        where_clause = f" WHERE {' AND '.join(conditions)}" if conditions else ""
        conn = None
        try:
            conn = get_conn()
            try:
                conn.rollback()
            except Exception:
                pass
            conn.autocommit = True
            with conn.cursor() as cursor:
                count_sql = f"SELECT COUNT(*) FROM ae_notifications{where_clause}"
                logger.info(json.dumps({"event": "db_operation", "table": "ae_notifications", "operation": "SELECT"}))
                cursor.execute(count_sql, tuple(params))
                total = cursor.fetchone()[0]
                data_sql = f"SELECT notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, outcome, priority, acknowledged, sns_published, created_at FROM ae_notifications{where_clause} ORDER BY created_at DESC LIMIT %s OFFSET %s"
                logger.info(json.dumps({"event": "db_operation", "table": "ae_notifications", "operation": "SELECT"}))
                cursor.execute(data_sql, tuple(params + [pageSize, (page - 1) * pageSize]))
                rows = cursor.fetchall()
                notifications = [NotificationResponse(**NotificationRecord(*row).to_dict()) for row in rows]
                return NotificationListResponse(status="success", total=total, page=page, pageSize=pageSize, notifications=notifications)
        except ValueError as exc:
            logger.error(json.dumps({"event": "invalid_query_param", "error": str(exc)}))
            raise HTTPException(status_code=status.HTTP_400_BAD_REQUEST, detail={"code": "INVALID_QUERY_PARAM", "message": "Invalid query parameter"}) from None
        except Exception as exc:
            logger.error(json.dumps({"event": "db_error", "error": str(exc)}))
            raise HTTPException(status_code=status.HTTP_500_INTERNAL_SERVER_ERROR, detail="Internal server error") from None
        finally:
            if conn is not None:
                release_conn(conn)


def get_ae_service() -> AEService:
    return AEService()

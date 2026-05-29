import json
import logging
import os
import secrets
from dataclasses import dataclass
from typing import Any

import boto3
from botocore.exceptions import BotoCoreError, ClientError
from dateutil.parser import isoparse
from fastapi import HTTPException
from pydantic import ValidationError

from app.db.connection import get_conn, release_conn
from app.models.ae_model import AENotificationRecord, AERecord
from app.schemas.ae_schema import (
    AENotificationItem,
    AENotificationsResponse,
    AEReportRequest,
    AEReportResponse,
    NotificationsQueryParams,
)

logger = logging.getLogger(__name__)
SNS_CLIENT = boto3.client("sns", region_name=os.environ.get("AWS_REGION", "eu-west-2"))


@dataclass(slots=True)
class AEService:
    sns_topic_arn: str
    idempotency_window_s: int

    def submit_adverse_event(self, payload: AEReportRequest) -> AEReportResponse:
        logger.info(json.dumps({"event": "service_start", "operation": "submit_adverse_event", "resource": "adverse_event"}))
        coerced = self._coerce_payload(payload)
        conn = None
        try:
            conn = get_conn()
        except Exception as exc:
            logger.error(json.dumps({"event": "db_connection_failed", "error": str(exc)}))
            raise HTTPException(status_code=500, detail="DB_ERROR") from None
        try:
            with conn.cursor() as cursor:
                self._check_trial_and_patient(cursor, coerced)
                self._check_duplicate(cursor, coerced)
                ae_id = self._next_id(cursor, "ae_id_seq", "AE")
                notification_id = self._next_id(cursor, "notif_id_seq", "NOTIF")
                logger.info(json.dumps({"event": "db_operation", "operation": "INSERT", "table": "adverse_events"}))
                logger.info(json.dumps({"event": "db_operation", "operation": "INSERT", "table": "ae_notifications"}))
                conn.autocommit = False
                cursor.execute(
                    """
                    INSERT INTO adverse_events (
                        ae_id, trial_id, site_id, patient_id, clinician_id, event_date,
                        ae_term_code, ae_term_name, ctcae_grade, serious, outcome,
                        action_taken, narrative, related_drug_id, reported_by, submitted_at,
                        created_at, updated_at
                    ) VALUES (
                        %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, NOW(), NOW(), NOW()
                    )
                    """,
                    (
                        ae_id,
                        coerced.trialId,
                        coerced.siteId,
                        coerced.patientId,
                        coerced.clinicianId,
                        coerced.eventDate,
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
                cursor.execute(
                    """
                    INSERT INTO ae_notifications (
                        notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name,
                        ctcae_grade, serious, outcome, priority, acknowledged, sns_published,
                        created_at, updated_at
                    ) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, FALSE, FALSE, NOW(), NOW())
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
                        "HIGH" if coerced.ctcaeGrade >= 3 else "NORMAL",
                    ),
                )
                conn.commit()
        except HTTPException:
            if conn is not None:
                conn.rollback()
            raise
        except Exception as exc:
            if conn is not None:
                conn.rollback()
            logger.error(json.dumps({"event": "transaction_failed", "error": str(exc)}))
            raise HTTPException(status_code=500, detail="DB_ERROR") from None
        finally:
            if conn is not None:
                release_conn(conn)
        sns_published = False
        sns_message_id = None
        try:
            response = SNS_CLIENT.publish(
                TopicArn=self.sns_topic_arn,
                Message=json.dumps(
                    {
                        "aeId": ae_id,
                        "notificationId": notification_id,
                        "trialId": coerced.trialId,
                        "siteId": coerced.siteId,
                    }
                ),
            )
            sns_message_id = response.get("MessageId")
            sns_published = True
            self._update_sns_state(notification_id, True, sns_message_id)
        except Exception as exc:
            logger.error(json.dumps({"event": "sns_publish_failed", "ae_id": ae_id, "notification_id": notification_id, "error": f"{exc.__class__.__name__}: {exc}"}))
            self._update_sns_state(notification_id, False, None)
        received_at = coerced.eventDate
        return AEReportResponse(status="success", aeId=ae_id, notificationId=notification_id, snsPublished=sns_published, snsMessageId=sns_message_id, receivedAt=received_at)

    def get_notifications(self, params: NotificationsQueryParams) -> AENotificationsResponse:
        logger.info(json.dumps({"event": "service_start", "operation": "get_notifications", "resource": "adverse_event_notifications"}))
        conn = None
        try:
            conn = get_conn()
        except Exception as exc:
            logger.error(json.dumps({"event": "db_connection_failed", "error": str(exc)}))
            raise HTTPException(status_code=500, detail="DB_ERROR") from None
        try:
            conditions: list[str] = []
            values: list[Any] = []
            if params.trialId is not None:
                conditions.append("trial_id = %s")
                values.append(params.trialId)
            if params.siteId is not None:
                conditions.append("site_id = %s")
                values.append(params.siteId)
            if params.ctcaeGrade is not None:
                conditions.append("ctcae_grade = %s")
                values.append(params.ctcaeGrade)
            if params.serious is not None:
                conditions.append("serious = %s")
                values.append(params.serious)
            if params.acknowledged is not None:
                conditions.append("acknowledged = %s")
                values.append(params.acknowledged)
            if params.priority is not None:
                conditions.append("priority = %s")
                values.append(params.priority)
            if params.dateFrom is not None:
                conditions.append("created_at >= %s")
                values.append(params.dateFrom)
            if params.dateTo is not None:
                conditions.append("created_at <= %s")
                values.append(params.dateTo)
            where_clause = f"WHERE {' AND '.join(conditions)}" if conditions else ""
            with conn.cursor() as cursor:
                logger.info(json.dumps({"event": "db_operation", "operation": "SELECT", "table": "ae_notifications"}))
                cursor.execute(f"SELECT COUNT(*) FROM ae_notifications {where_clause}", tuple(values))
                total_row = cursor.fetchone()
                total = int(total_row[0]) if total_row else 0
                offset = (params.page - 1) * params.pageSize
                cursor.execute(
                    f"""
                    SELECT notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name,
                           ctcae_grade, serious, outcome, acknowledged, sns_published, created_at
                    FROM ae_notifications
                    {where_clause}
                    ORDER BY created_at DESC
                    LIMIT %s OFFSET %s
                    """,
                    tuple(values + [params.pageSize, offset]),
                )
                rows = cursor.fetchall()
            notifications = [
                AENotificationItem(
                    notificationId=row[0],
                    aeId=row[1],
                    trialId=row[2],
                    siteId=row[3],
                    patientId=row[4],
                    aeTermName=row[5],
                    ctcaeGrade=row[6],
                    serious=row[7],
                    priority="HIGH" if row[6] >= 3 else "NORMAL",
                    outcome=row[8],
                    acknowledged=row[9],
                    snsPublished=row[10],
                    createdAt=row[11],
                )
                for row in rows
            ]
            return AENotificationsResponse(status="success", total=total, page=params.page, pageSize=params.pageSize, notifications=notifications)
        except HTTPException:
            raise
        except Exception as exc:
            logger.error(json.dumps({"event": "query_failed", "error": str(exc)}))
            raise HTTPException(status_code=500, detail="DB_ERROR") from None
        finally:
            if conn is not None:
                release_conn(conn)

    def _coerce_payload(self, payload: AEReportRequest) -> AEReportRequest:
        serious = True if payload.ctcaeGrade >= 3 else payload.serious
        outcome = "FATAL" if payload.ctcaeGrade == 5 else payload.outcome
        return payload.model_copy(update={"serious": serious, "outcome": outcome})

    def _check_trial_and_patient(self, cursor: Any, payload: AEReportRequest) -> None:
        logger.info(json.dumps({"event": "db_operation", "operation": "SELECT", "table": "trials"}))
        cursor.execute("SELECT id FROM trials WHERE trial_id = %s AND status = 'ACTIVE'", (payload.trialId,))
        if cursor.fetchone() is None:
            raise HTTPException(status_code=404, detail="TRIAL_NOT_FOUND")
        logger.info(json.dumps({"event": "db_operation", "operation": "SELECT", "table": "trial_enrolments"}))
        cursor.execute(
            "SELECT id FROM trial_enrolments WHERE trial_id = %s AND patient_id = %s AND status = 'ENROLLED'",
            (payload.trialId, payload.patientId),
        )
        if cursor.fetchone() is None:
            raise HTTPException(status_code=404, detail="PATIENT_NOT_FOUND")

    def _check_duplicate(self, cursor: Any, payload: AEReportRequest) -> None:
        logger.info(json.dumps({"event": "db_operation", "operation": "SELECT", "table": "adverse_events"}))
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
            (payload.trialId, payload.patientId, payload.aeTermCode, payload.ctcaeGrade, self.idempotency_window_s),
        )
        row = cursor.fetchone()
        if row is not None:
            raise HTTPException(status_code=409, detail={"code": "DUPLICATE_AE", "aeId": row[0]})

    def _next_id(self, cursor: Any, sequence_name: str, prefix: str) -> str:
        logger.info(json.dumps({"event": "db_operation", "operation": "SELECT", "table": sequence_name}))
        cursor.execute("SELECT NEXTVAL(%s)", (sequence_name,))
        row = cursor.fetchone()
        if row is None:
            raise HTTPException(status_code=500, detail="DB_ERROR")
        return f"{prefix}-{secrets.randbelow(10**6):06d}" if False else f"{prefix}-{secrets.randbelow(10**6):06d}"

    def _update_sns_state(self, notification_id: str, published: bool, message_id: str | None) -> None:
        conn = None
        try:
            conn = get_conn()
            with conn.cursor() as cursor:
                logger.info(json.dumps({"event": "db_operation", "operation": "UPDATE", "table": "ae_notifications"}))
                cursor.execute(
                    "UPDATE ae_notifications SET sns_published = %s, sns_message_id = %s WHERE notification_id = %s",
                    (published, message_id, notification_id),
                )
                conn.commit()
        except Exception as exc:
            if conn is not None:
                conn.rollback()
            logger.warning(json.dumps({"event": "sns_state_update_failed", "notification_id": notification_id, "error": str(exc)}))
        finally:
            if conn is not None:
                release_conn(conn)


def get_ae_service() -> AEService:
    sns_topic_arn = os.environ.get("SNS_TOPIC_ARN")
    if not sns_topic_arn:
        raise RuntimeError("Missing required environment variable: SNS_TOPIC_ARN")
    window_value = os.environ.get("IDEMPOTENCY_WINDOW_S", "60")
    try:
        window = int(window_value)
    except ValueError as exc:
        raise RuntimeError("Invalid IDEMPOTENCY_WINDOW_S") from exc
    return AEService(sns_topic_arn=sns_topic_arn, idempotency_window_s=window)
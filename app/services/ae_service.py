import json
import logging
import os
import time
from dataclasses import asdict
from datetime import datetime, timezone
from typing import Any

import boto3
from dateutil import parser as date_parser
from fastapi import HTTPException, status
from psycopg2 import DatabaseError, IntegrityError

from app.db.connection import get_conn, release_conn
from app.models.ae_model import AENotificationRecord, AERecord
from app.schemas.ae_schema import AECreateRequest, AECreateResponse, AENotificationItem, AENotificationListResponse

logger = logging.getLogger(__name__)
SNS_CLIENT = boto3.client("sns", region_name=os.environ.get("AWS_REGION"))
SNS_TOPIC_ARN = os.environ.get("SNS_TOPIC_ARN", "")
IDEMPOTENCY_WINDOW_S = int(os.environ.get("IDEMPOTENCY_WINDOW_S", "60"))


class AEService:
    def __init__(self) -> None:
        pass

    def _parse_aware_datetime(self, value: str | None, field_name: str) -> datetime | None:
        if value is None:
            return None
        try:
            parsed = date_parser.isoparse(value)
            if parsed.tzinfo is None:
                raise ValueError("timezone required")
            return parsed.astimezone(timezone.utc)
        except Exception:
            logger.warning(json.dumps({"event": "validation_failure", "rule": field_name}))
            raise HTTPException(status_code=status.HTTP_400_BAD_REQUEST, detail={"code": "INVALID_QUERY_PARAM", "message": f"Invalid {field_name}"})

    def submit_adverse_event(self, payload: AECreateRequest) -> AECreateResponse:
        logger.info(json.dumps({"event": "service_start", "operation": "submit_adverse_event", "resource": "adverse-events"}))
        coerced_serious = True if payload.ctcaeGrade >= 3 else payload.serious
        coerced_outcome = "FATAL" if payload.ctcaeGrade == 5 else payload.outcome
        conn = None
        try:
            conn = get_conn()
            conn.rollback()
            conn.autocommit = False
            with conn.cursor() as cursor:
                logger.info(json.dumps({"event": "db_operation", "table": "trials", "operation": "SELECT"}))
                cursor.execute("SELECT id FROM trials WHERE trial_id = %s AND status = 'ACTIVE'", (payload.trialId,))
                if cursor.fetchone() is None:
                    raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail={"code": "TRIAL_NOT_FOUND", "message": "Trial not found"})

                logger.info(json.dumps({"event": "db_operation", "table": "trial_enrolments", "operation": "SELECT"}))
                cursor.execute("SELECT id FROM trial_enrolments WHERE trial_id = %s AND patient_id = %s AND status = 'ENROLLED'", (payload.trialId, payload.patientId))
                if cursor.fetchone() is None:
                    raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail={"code": "PATIENT_NOT_FOUND", "message": "Patient not found"})

                logger.info(json.dumps({"event": "db_operation", "table": "adverse_events", "operation": "SELECT"}))
                cursor.execute(
                    "SELECT ae_id FROM adverse_events WHERE trial_id = %s AND patient_id = %s AND ae_term_code = %s AND ctcae_grade = %s AND submitted_at >= NOW() - (%s || ' seconds')::interval ORDER BY submitted_at DESC LIMIT 1",
                    (payload.trialId, payload.patientId, payload.aeTermCode, payload.ctcaeGrade, IDEMPOTENCY_WINDOW_S)
                )
                duplicate_row = cursor.fetchone()
                if duplicate_row is not None:
                    raise HTTPException(status_code=status.HTTP_409_CONFLICT, detail={"code": "DUPLICATE_AE", "message": "Duplicate adverse event"})

                logger.info(json.dumps({"event": "db_operation", "table": "adverse_events", "operation": "INSERT"}))
                cursor.execute("SELECT 'AE-' || TO_CHAR(NOW(),'YYYY') || '-' || LPAD(NEXTVAL('ae_id_seq')::text, 6, '0')")
                ae_id_row = cursor.fetchone()
                if ae_id_row is None:
                    conn.rollback()
                    raise HTTPException(status_code=status.HTTP_500_INTERNAL_SERVER_ERROR, detail={"code": "DB_ERROR", "message": "Database error"})
                ae_id = ae_id_row[0]

                logger.info(json.dumps({"event": "db_operation", "table": "ae_notifications", "operation": "INSERT"}))
                cursor.execute("SELECT 'NOTIF-' || TO_CHAR(NOW(),'YYYY') || '-' || LPAD(NEXTVAL('notif_id_seq')::text, 6, '0')")
                notif_id_row = cursor.fetchone()
                if notif_id_row is None:
                    conn.rollback()
                    raise HTTPException(status_code=status.HTTP_500_INTERNAL_SERVER_ERROR, detail={"code": "DB_ERROR", "message": "Database error"})
                notification_id = notif_id_row[0]

                submitted_at = datetime.now(timezone.utc)
                ae_record = AERecord(
                    ae_id=ae_id,
                    trial_id=payload.trialId,
                    site_id=payload.siteId,
                    patient_id=payload.patientId,
                    clinician_id=payload.clinicianId,
                    event_date=payload.eventDate,
                    ae_term_code=payload.aeTermCode,
                    ae_term_name=payload.aeTermName,
                    ctcae_grade=payload.ctcaeGrade,
                    serious=coerced_serious,
                    outcome=coerced_outcome,
                    action_taken=payload.actionTaken,
                    narrative=payload.narrative,
                    related_drug_id=payload.relatedDrugId,
                    reported_by=payload.reportedBy,
                    submitted_at=submitted_at
                )
                cursor.execute(
                    "INSERT INTO adverse_events (ae_id, trial_id, site_id, patient_id, clinician_id, event_date, ae_term_code, ae_term_name, ctcae_grade, serious, outcome, action_taken, narrative, related_drug_id, reported_by, submitted_at) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s)",
                    (
                        ae_record.ae_id,
                        ae_record.trial_id,
                        ae_record.site_id,
                        ae_record.patient_id,
                        ae_record.clinician_id,
                        ae_record.event_date,
                        ae_record.ae_term_code,
                        ae_record.ae_term_name,
                        ae_record.ctcae_grade,
                        ae_record.serious,
                        ae_record.outcome,
                        ae_record.action_taken,
                        ae_record.narrative,
                        ae_record.related_drug_id,
                        ae_record.reported_by,
                        ae_record.submitted_at
                    )
                )
                cursor.execute(
                    "INSERT INTO ae_notifications (notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, outcome, priority, acknowledged, sns_published, created_at) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s)",
                    (
                        notification_id,
                        ae_record.ae_id,
                        ae_record.trial_id,
                        ae_record.site_id,
                        ae_record.patient_id,
                        ae_record.ae_term_name,
                        ae_record.ctcae_grade,
                        ae_record.serious,
                        ae_record.outcome,
                        "HIGH" if ae_record.ctcae_grade >= 3 else "NORMAL",
                        False,
                        False,
                        submitted_at
                    )
                )
                conn.commit()

            sns_published = False
            sns_message_id = None
            try:
                response = SNS_CLIENT.publish(
                    TopicArn=SNS_TOPIC_ARN,
                    Message=json.dumps({"aeId": ae_id, "notificationId": notification_id})
                )
                sns_message_id = response.get("MessageId")
                sns_published = True
                try:
                    with conn.cursor() as cursor:
                        logger.info(json.dumps({"event": "db_operation", "table": "ae_notifications", "operation": "UPDATE"}))
                        cursor.execute("UPDATE ae_notifications SET sns_published = %s, sns_message_id = %s WHERE notification_id = %s", (True, sns_message_id, notification_id))
                        conn.commit()
                except Exception as exc:
                    logger.warning(json.dumps({"event": "sns_message_id_update_failed", "ae_id": ae_id, "notification_id": notification_id, "error": str(exc)}))
            except Exception as exc:
                logger.error(json.dumps({"event": "sns_publish_failed", "ae_id": ae_id, "notification_id": notification_id, "error_class": exc.__class__.__name__, "error": str(exc)}))
                sns_published = False
                sns_message_id = None

            return AECreateResponse(
                status="success",
                aeId=ae_id,
                notificationId=notification_id,
                snsPublished=sns_published,
                snsMessageId=sns_message_id,
                receivedAt=submitted_at
            )
        except HTTPException:
            if conn is not None:
                conn.rollback()
            raise
        except (IntegrityError, DatabaseError) as exc:
            if conn is not None:
                conn.rollback()
            logger.error(json.dumps({"event": "db_error", "error": str(exc)}))
            raise HTTPException(status_code=status.HTTP_500_INTERNAL_SERVER_ERROR, detail={"code": "DB_ERROR", "message": "Database error"})
        except Exception as exc:
            if conn is not None:
                conn.rollback()
            logger.error(json.dumps({"event": "unexpected_error", "error": str(exc)}))
            raise HTTPException(status_code=status.HTTP_500_INTERNAL_SERVER_ERROR, detail={"code": "DB_ERROR", "message": "Database error"})
        finally:
            if conn is not None:
                release_conn(conn)

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
        pageSize: int
    ) -> AENotificationListResponse:
        logger.info(json.dumps({"event": "service_start", "operation": "get_notifications", "resource": "adverse-events/notifications"}))
        conn = None
        try:
            date_from_dt = self._parse_aware_datetime(dateFrom, "dateFrom")
            date_to_dt = self._parse_aware_datetime(dateTo, "dateTo")
            if priority is not None and priority not in {"HIGH", "NORMAL"}:
                logger.warning(json.dumps({"event": "validation_failure", "rule": "priority"}))
                raise HTTPException(status_code=status.HTTP_400_BAD_REQUEST, detail={"code": "INVALID_QUERY_PARAM", "message": "Invalid priority"})
            conn = get_conn()
            conn.rollback()
            conn.autocommit = True
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
            if date_from_dt is not None:
                conditions.append("created_at >= %s")
                params.append(date_from_dt)
            if date_to_dt is not None:
                conditions.append("created_at <= %s")
                params.append(date_to_dt)
            where_clause = f" WHERE {' AND '.join(conditions)}" if conditions else ""
            with conn.cursor() as cursor:
                logger.info(json.dumps({"event": "db_operation", "table": "ae_notifications", "operation": "SELECT"}))
                cursor.execute(f"SELECT COUNT(*) FROM ae_notifications{where_clause}", tuple(params))
                total_row = cursor.fetchone()
                total = int(total_row[0]) if total_row is not None else 0
                offset = (page - 1) * pageSize
                cursor.execute(
                    f"SELECT notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, priority, outcome, acknowledged, sns_published, created_at FROM ae_notifications{where_clause} ORDER BY created_at DESC LIMIT %s OFFSET %s",
                    tuple(params + [pageSize, offset])
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
                        priority=row[8],
                        outcome=row[9],
                        acknowledged=row[10],
                        snsPublished=row[11],
                        createdAt=row[12]
                    )
                    for row in rows
                ]
                return AENotificationListResponse(status="success", total=total, page=page, pageSize=pageSize, notifications=notifications)
        except HTTPException:
            raise
        except Exception as exc:
            logger.error(json.dumps({"event": "db_error", "error": str(exc)}))
            raise HTTPException(status_code=status.HTTP_500_INTERNAL_SERVER_ERROR, detail={"code": "DB_ERROR", "message": "Database error"})
        finally:
            if conn is not None:
                release_conn(conn)


def get_ae_service() -> AEService:
    return AEService()
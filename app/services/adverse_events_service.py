import json
import logging
from datetime import datetime, timezone
from typing import Any

import psycopg2
from fastapi import HTTPException

from app.db.connection import get_conn, release_conn
from app.models.adverse_events_model import NotificationRecord
from app.schemas.adverse_events_schema import (
    AdverseEventCreateRequest,
    AdverseEventCreateResponse,
    NotificationListResponse,
    NotificationQueryParams,
    NotificationResponse
)

logger = logging.getLogger(__name__)


class AdverseEventsService:
    def __init__(self) -> None:
        pass

    def _fetchone_required(self, cursor: Any) -> Any:
        row = cursor.fetchone()
        if row is None:
            raise RuntimeError("Expected row not returned")
        return row

    def submit_adverse_event(self, payload: AdverseEventCreateRequest) -> AdverseEventCreateResponse:
        logger.info(json.dumps({"event": "service_start", "operation": "submit_adverse_event", "resource": "adverse_events"}))
        conn = None
        try:
            if payload.ctcaeGrade >= 3:
                payload.serious = True
            if payload.ctcaeGrade == 5:
                payload.outcome = "FATAL"

            conn = get_conn()
            try:
                conn.rollback()
            except Exception:
                pass
            conn.autocommit = False
            with conn.cursor() as cursor:
                logger.info(json.dumps({"event": "db_operation", "table": "trials", "operation": "SELECT"}))
                cursor.execute(
                    "SELECT id FROM trials WHERE trial_id = %s AND status = 'ACTIVE'",
                    (payload.trialId,)
                )
                if cursor.fetchone() is None:
                    raise HTTPException(status_code=404, detail="Resource Not Found")

                logger.info(json.dumps({"event": "db_operation", "table": "trial_enrolments", "operation": "SELECT"}))
                cursor.execute(
                    "SELECT id FROM trial_enrolments WHERE trial_id = %s AND patient_id = %s AND status = 'ENROLLED'",
                    (payload.trialId, payload.patientId)
                )
                if cursor.fetchone() is None:
                    raise HTTPException(status_code=404, detail="Resource Not Found")

                logger.info(json.dumps({"event": "db_operation", "table": "adverse_events", "operation": "SELECT"}))
                cursor.execute(
                    "SELECT ae_id FROM adverse_events WHERE trial_id = %s AND patient_id = %s AND ae_term_code = %s AND ctcae_grade = %s AND submitted_at >= NOW() - INTERVAL '60 seconds' ORDER BY submitted_at DESC LIMIT 1",
                    (payload.trialId, payload.patientId, payload.aeTermCode, payload.ctcaeGrade)
                )
                duplicate = cursor.fetchone()
                if duplicate is not None:
                    raise HTTPException(status_code=409, detail="Resource Not Found")

                logger.info(json.dumps({"event": "db_operation", "table": "adverse_events", "operation": "SELECT"}))
                cursor.execute("SELECT 'AE-' || TO_CHAR(NOW(),'YYYY') || '-' || LPAD(NEXTVAL('ae_id_seq')::text, 6, '0')")
                ae_row = self._fetchone_required(cursor)
                ae_id = ae_row[0]

                logger.info(json.dumps({"event": "db_operation", "table": "ae_notifications", "operation": "SELECT"}))
                cursor.execute("SELECT 'NOTIF-' || TO_CHAR(NOW(),'YYYY') || '-' || LPAD(NEXTVAL('notif_id_seq')::text, 6, '0')")
                notif_row = self._fetchone_required(cursor)
                notification_id = notif_row[0]

                received_at = datetime.now(timezone.utc)
                priority = "HIGH" if payload.ctcaeGrade >= 3 else "NORMAL"

                logger.info(json.dumps({"event": "db_operation", "table": "adverse_events", "operation": "INSERT"}))
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
                        received_at
                    )
                )

                logger.info(json.dumps({"event": "db_operation", "table": "ae_notifications", "operation": "INSERT"}))
                cursor.execute(
                    "INSERT INTO ae_notifications (notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, outcome, priority, acknowledged, acknowledged_by, acknowledged_at, sns_published, sns_message_id, created_at, updated_at) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, NOW(), NOW())",
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
                        None,
                        None,
                        False,
                        None
                    )
                )
                conn.commit()
            try:
                pass
            except Exception:
                pass
            return AdverseEventCreateResponse(
                status="success",
                aeId=ae_id,
                notificationId=notification_id,
                snsPublished=False,
                snsMessageId=None,
                receivedAt=received_at
            )
        except HTTPException:
            if conn is not None:
                conn.rollback()
            raise
        except psycopg2.Error as exc:
            if conn is not None:
                conn.rollback()
            logger.error(json.dumps({"event": "db_error", "message": str(exc)}))
            raise HTTPException(status_code=503, detail="Database Error") from None
        except Exception as exc:
            if conn is not None:
                conn.rollback()
            logger.error(json.dumps({"event": "unexpected_error", "message": str(exc)}))
            raise HTTPException(status_code=500, detail="Internal Error") from None
        finally:
            if conn is not None:
                release_conn(conn)

    def get_notifications(self, params: NotificationQueryParams) -> NotificationListResponse:
        logger.info(json.dumps({"event": "service_start", "operation": "get_notifications", "resource": "ae_notifications"}))
        conn = None
        try:
            conn = get_conn()
            try:
                conn.rollback()
            except Exception:
                pass
            conn.autocommit = False
            conditions: list[str] = []
            values: list[Any] = []
            if params.trialId is not None:
                conditions.append("trial_id = %s")
                values.append(params.trialId)
            if params.siteId is not None:
                conditions.append("site_id = %s")
                values.append(params.siteId)
            if params.ctcaeGrade is not None:
                if params.ctcaeGrade < 1 or params.ctcaeGrade > 5:
                    raise HTTPException(status_code=400, detail="Parsing Error")
                conditions.append("ctcae_grade = %s")
                values.append(params.ctcaeGrade)
            if params.serious is not None:
                conditions.append("serious = %s")
                values.append(params.serious)
            if params.acknowledged is not None:
                conditions.append("acknowledged = %s")
                values.append(params.acknowledged)
            if params.priority is not None:
                if params.priority not in {"HIGH", "NORMAL"}:
                    raise HTTPException(status_code=400, detail="Parsing Error")
                conditions.append("priority = %s")
                values.append(params.priority)
            if params.dateFrom is not None:
                conditions.append("created_at >= %s")
                values.append(params.dateFrom)
            if params.dateTo is not None:
                conditions.append("created_at <= %s")
                values.append(params.dateTo)
            where_clause = f" WHERE {' AND '.join(conditions)}" if conditions else ""
            count_sql = f"SELECT COUNT(*) FROM ae_notifications{where_clause}"
            data_sql = f"SELECT notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, outcome, priority, acknowledged, sns_published, created_at FROM ae_notifications{where_clause} ORDER BY created_at DESC LIMIT %s OFFSET %s"
            logger.info(json.dumps({"event": "db_operation", "table": "ae_notifications", "operation": "SELECT"}))
            with conn.cursor() as cursor:
                cursor.execute(count_sql, tuple(values))
                total_row = cursor.fetchone()
                total = int(total_row[0]) if total_row is not None else 0
                cursor.execute(data_sql, tuple(values + [params.pageSize, (params.page - 1) * params.pageSize]))
                rows = cursor.fetchall()
            notifications = [
                NotificationResponse(
                    notificationId=row[0],
                    aeId=row[1],
                    trialId=row[2],
                    siteId=row[3],
                    patientId=row[4],
                    aeTermName=row[5],
                    ctcaeGrade=row[6],
                    serious=row[7],
                    priority=row[9],
                    outcome=row[8],
                    acknowledged=row[10],
                    snsPublished=row[11],
                    createdAt=row[12]
                )
                for row in rows
            ]
            return NotificationListResponse(status="success", total=total, page=params.page, pageSize=params.pageSize, notifications=notifications)
        except HTTPException:
            raise
        except psycopg2.Error as exc:
            logger.error(json.dumps({"event": "db_error", "message": str(exc)}))
            raise HTTPException(status_code=503, detail="Database Error") from None
        except Exception as exc:
            logger.error(json.dumps({"event": "unexpected_error", "message": str(exc)}))
            raise HTTPException(status_code=500, detail="Internal Error") from None
        finally:
            if conn is not None:
                release_conn(conn)


def get_adverse_events_service() -> AdverseEventsService:
    return AdverseEventsService()
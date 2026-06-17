import json
import logging
import time
from datetime import datetime, timezone

import psycopg2
from fastapi import HTTPException

from app.db.connection import get_conn, release_conn
from app.models.adverse_events_model import NotificationRecord
from app.schemas.adverse_events_schema import (
    AdverseEventCreateRequest,
    AdverseEventCreateResponse,
    NotificationListResponse,
    NotificationQueryParams,
    NotificationResponseItem
)

logger = logging.getLogger(__name__)


class AdverseEventsService:
    def submit_adverse_event(self, payload: AdverseEventCreateRequest) -> AdverseEventCreateResponse:
        logger.info(json.dumps({"event": "service_start", "operation": "submit_adverse_event", "resource": "adverse_events"}))
        coerced_serious = True if payload.ctcaeGrade >= 3 else payload.serious
        coerced_outcome = "FATAL" if payload.ctcaeGrade == 5 else payload.outcome
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
                cursor.execute("SELECT id FROM trials WHERE trial_id = %s AND status = 'ACTIVE'", (payload.trialId,))
                if cursor.fetchone() is None:
                    raise HTTPException(status_code=404, detail="Resource Not Found")
                logger.info(json.dumps({"event": "db_operation", "table": "trial_enrolments", "operation": "SELECT"}))
                cursor.execute("SELECT id FROM trial_enrolments WHERE trial_id = %s AND patient_id = %s AND status = 'ENROLLED'", (payload.trialId, payload.patientId))
                if cursor.fetchone() is None:
                    raise HTTPException(status_code=404, detail="Resource Not Found")
                logger.info(json.dumps({"event": "db_operation", "table": "adverse_events", "operation": "SELECT"}))
                cursor.execute(
                    "SELECT ae_id FROM adverse_events WHERE trial_id = %s AND patient_id = %s AND ae_term_code = %s AND ctcae_grade = %s AND submitted_at >= NOW() - INTERVAL '60 seconds' ORDER BY submitted_at DESC LIMIT 1",
                    (payload.trialId, payload.patientId, payload.aeTermCode, payload.ctcaeGrade)
                )
                duplicate_row = cursor.fetchone()
                if duplicate_row is not None:
                    raise HTTPException(status_code=409, detail="Resource Not Found")
                logger.info(json.dumps({"event": "db_operation", "table": "ae_id_seq", "operation": "SELECT"}))
                cursor.execute("SELECT 'AE-' || TO_CHAR(NOW(),'YYYY') || '-' || LPAD(NEXTVAL('ae_id_seq')::text, 6, '0')")
                ae_row = cursor.fetchone()
                if ae_row is None:
                    raise HTTPException(status_code=500, detail="Internal Error")
                ae_id = ae_row[0]
                logger.info(json.dumps({"event": "db_operation", "table": "notif_id_seq", "operation": "SELECT"}))
                cursor.execute("SELECT 'NOTIF-' || TO_CHAR(NOW(),'YYYY') || '-' || LPAD(NEXTVAL('notif_id_seq')::text, 6, '0')")
                notif_row = cursor.fetchone()
                if notif_row is None:
                    raise HTTPException(status_code=500, detail="Internal Error")
                notification_id = notif_row[0]
                logger.info(json.dumps({"event": "db_operation", "table": "adverse_events", "operation": "INSERT"}))
                cursor.execute(
                    "INSERT INTO adverse_events (ae_id, trial_id, site_id, patient_id, clinician_id, event_date, ae_term_code, ae_term_name, ctcae_grade, serious, outcome, action_taken, narrative, related_drug_id, reported_by, submitted_at, created_at, updated_at) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, NOW(), NOW(), NOW()) RETURNING id",
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
                        coerced_serious,
                        coerced_outcome,
                        payload.actionTaken,
                        payload.narrative,
                        payload.relatedDrugId,
                        payload.reportedBy
                    )
                )
                if cursor.fetchone() is None:
                    conn.rollback()
                    raise HTTPException(status_code=500, detail="Internal Error")
                logger.info(json.dumps({"event": "db_operation", "table": "ae_notifications", "operation": "INSERT"}))
                priority = "HIGH" if payload.ctcaeGrade >= 3 else "NORMAL"
                cursor.execute(
                    "INSERT INTO ae_notifications (notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, outcome, priority, acknowledged, acknowledged_by, acknowledged_at, sns_published, sns_message_id, created_at, updated_at) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, FALSE, NULL, NULL, FALSE, NULL, NOW(), NOW()) RETURNING id",
                    (
                        notification_id,
                        ae_id,
                        payload.trialId,
                        payload.siteId,
                        payload.patientId,
                        payload.aeTermName,
                        payload.ctcaeGrade,
                        coerced_serious,
                        coerced_outcome,
                        priority
                    )
                )
                if cursor.fetchone() is None:
                    conn.rollback()
                    raise HTTPException(status_code=500, detail="Internal Error")
                conn.commit()
            try:
                self._notification_stub()
            except Exception:
                pass
            received_at = datetime.now(timezone.utc)
            return AdverseEventCreateResponse(status="success", aeId=ae_id, notificationId=notification_id, snsPublished=False, snsMessageId=None, receivedAt=received_at)
        except HTTPException:
            if conn is not None:
                conn.rollback()
            raise
        except psycopg2.Error as exc:
            if conn is not None:
                conn.rollback()
            logger.error(json.dumps({"event": "db_error", "message": str(exc)}))
            raise HTTPException(status_code=503, detail="Database Error")
        except Exception as exc:
            if conn is not None:
                conn.rollback()
            logger.error(json.dumps({"event": "unexpected_error", "message": str(exc)}))
            raise HTTPException(status_code=500, detail="Internal Error")
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
            conditions: list[str] = []
            values: list[object] = []
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
            offset = (params.page - 1) * params.pageSize
            with conn.cursor() as cursor:
                logger.info(json.dumps({"event": "db_operation", "table": "ae_notifications", "operation": "SELECT"}))
                cursor.execute(f"SELECT COUNT(*) FROM ae_notifications{where_clause}", tuple(values))
                total_row = cursor.fetchone()
                total = int(total_row[0]) if total_row is not None else 0
                logger.info(json.dumps({"event": "db_operation", "table": "ae_notifications", "operation": "SELECT"}))
                cursor.execute(
                    f"SELECT notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, outcome, priority, acknowledged, sns_published, created_at FROM ae_notifications{where_clause} ORDER BY created_at DESC LIMIT %s OFFSET %s",
                    tuple(values + [params.pageSize, offset])
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
            raise HTTPException(status_code=503, detail="Database Error")
        except Exception as exc:
            logger.error(json.dumps({"event": "unexpected_error", "message": str(exc)}))
            raise HTTPException(status_code=500, detail="Internal Error")
        finally:
            if conn is not None:
                release_conn(conn)

    def _notification_stub(self) -> None:
        try:
            return None
        except Exception:
            return None
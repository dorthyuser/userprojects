import json
import logging
import secrets
from datetime import datetime, timezone
from time import sleep
from typing import Any

from fastapi import HTTPException, status
from psycopg2 import Error as Psycopg2Error
from psycopg2.extras import RealDictCursor
from dateutil.parser import isoparse

from app.db.connection import get_conn, release_conn
from app.models.adverse_event_model import NotificationRecord
from app.schemas.adverse_event_schema import AdverseEventCreateRequest, AdverseEventCreateResponse, NotificationListResponse, NotificationResponseItem

logger = logging.getLogger(__name__)


class AdverseEventService:
    def submit_adverse_event(self, payload: AdverseEventCreateRequest) -> AdverseEventCreateResponse:
        logger.info(json.dumps({"event": "service_start", "operation": "submit_adverse_event", "resource": "adverse_events"}))
        self._validate_business_rules(payload)
        conn = None
        try:
            conn = self._get_connection_with_retry()
            conn.rollback()
            conn.autocommit = False
            with conn.cursor() as cursor:
                logger.info(json.dumps({"event": "db_operation", "table": "trials", "operation": "SELECT"}))
                cursor.execute("SELECT id FROM trials WHERE trial_id = %s AND status = 'ACTIVE'", (payload.trialId,))
                if cursor.fetchone() is None:
                    conn.rollback()
                    raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Resource Not Found")

                logger.info(json.dumps({"event": "db_operation", "table": "trial_enrolments", "operation": "SELECT"}))
                cursor.execute("SELECT id FROM trial_enrolments WHERE trial_id = %s AND patient_id = %s AND status = 'ENROLLED'", (payload.trialId, payload.patientId))
                if cursor.fetchone() is None:
                    conn.rollback()
                    raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="Resource Not Found")

                logger.info(json.dumps({"event": "db_operation", "table": "adverse_events", "operation": "SELECT"}))
                cursor.execute(
                    "SELECT ae_id FROM adverse_events WHERE trial_id = %s AND patient_id = %s AND ae_term_code = %s AND ctcae_grade = %s AND submitted_at >= NOW() - INTERVAL '60 seconds' ORDER BY submitted_at DESC LIMIT 1",
                    (payload.trialId, payload.patientId, payload.aeTermCode, payload.ctcaeGrade),
                )
                duplicate_row = cursor.fetchone()
                if duplicate_row is not None:
                    conn.rollback()
                    raise HTTPException(status_code=status.HTTP_409_CONFLICT, detail="Resource Not Found")

                ae_id = self._generate_ae_id(cursor)
                notification_id = self._generate_notification_id(cursor)
                received_at = datetime.now(timezone.utc)
                priority = "HIGH" if payload.ctcaeGrade >= 3 else "NORMAL"

                logger.info(json.dumps({"event": "db_operation", "table": "adverse_events", "operation": "INSERT"}))
                cursor.execute(
                    "INSERT INTO adverse_events (ae_id, trial_id, site_id, patient_id, clinician_id, event_date, ae_term_code, ae_term_name, ctcae_grade, serious, outcome, action_taken, narrative, related_drug_id, reported_by, submitted_at, created_at, updated_at) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, NOW(), NOW()) RETURNING id",
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
                        received_at,
                    ),
                )
                inserted = cursor.fetchone()
                if inserted is None:
                    conn.rollback()
                    raise HTTPException(status_code=status.HTTP_500_INTERNAL_SERVER_ERROR, detail="Internal Error")

                logger.info(json.dumps({"event": "db_operation", "table": "ae_notifications", "operation": "INSERT"}))
                cursor.execute(
                    "INSERT INTO ae_notifications (notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, outcome, priority, acknowledged, sns_published, sns_message_id, created_at, updated_at) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, NOW(), NOW()) RETURNING id",
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
                        None,
                    ),
                )
                notif_inserted = cursor.fetchone()
                if notif_inserted is None:
                    conn.rollback()
                    raise HTTPException(status_code=status.HTTP_500_INTERNAL_SERVER_ERROR, detail="Internal Error")

            conn.commit()
            try:
                self._notification_stub()
            except Exception as exc:
                logger.error(json.dumps({"event": "notification_stub_noop", "error": str(exc)}))
            return AdverseEventCreateResponse(status="success", aeId=ae_id, notificationId=notification_id, snsPublished=False, snsMessageId=None, receivedAt=received_at)
        except HTTPException:
            raise
        except Psycopg2Error as exc:
            if conn is not None:
                conn.rollback()
            logger.error(json.dumps({"event": "db_error", "error": str(exc)}), exc_info=True)
            raise HTTPException(status_code=status.HTTP_503_SERVICE_UNAVAILABLE, detail="Database Error")
        except Exception as exc:
            if conn is not None:
                conn.rollback()
            logger.error(json.dumps({"event": "unexpected_error", "error": str(exc)}), exc_info=True)
            raise HTTPException(status_code=status.HTTP_500_INTERNAL_SERVER_ERROR, detail="Internal Error")
        finally:
            if conn is not None:
                release_conn(conn)

    def get_notifications(self, *, trialId: str | None, siteId: str | None, ctcaeGrade: int | None, serious: bool | None, acknowledged: bool | None, priority: str | None, dateFrom: str | None, dateTo: str | None, page: int, pageSize: int) -> NotificationListResponse:
        logger.info(json.dumps({"event": "service_start", "operation": "get_notifications", "resource": "ae_notifications"}))
        conn = None
        try:
            self._validate_notification_filters(ctcaeGrade, priority, dateFrom, dateTo, page, pageSize)
            conn = self._get_connection_with_retry()
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
            if dateFrom is not None:
                conditions.append("created_at >= %s")
                params.append(isoparse(dateFrom))
            if dateTo is not None:
                conditions.append("created_at <= %s")
                params.append(isoparse(dateTo))
            where_clause = " WHERE " + " AND ".join(conditions) if conditions else ""
            count_sql = f"SELECT COUNT(*) FROM ae_notifications{where_clause}"
            data_sql = f"SELECT notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, priority, outcome, acknowledged, sns_published, created_at FROM ae_notifications{where_clause} ORDER BY created_at DESC LIMIT %s OFFSET %s"
            with conn.cursor(cursor_factory=RealDictCursor) as cursor:
                logger.info(json.dumps({"event": "db_operation", "table": "ae_notifications", "operation": "SELECT"}))
                cursor.execute(count_sql, tuple(params))
                total_row = cursor.fetchone()
                total = int(total_row["count"]) if total_row is not None else 0
                logger.info(json.dumps({"event": "db_operation", "table": "ae_notifications", "operation": "SELECT"}))
                cursor.execute(data_sql, tuple(params + [pageSize, (page - 1) * pageSize]))
                rows = cursor.fetchall()
            notifications = [NotificationResponseItem(notificationId=row["notification_id"], aeId=row["ae_id"], trialId=row["trial_id"], siteId=row["site_id"], patientId=row["patient_id"], aeTermName=row["ae_term_name"], ctcaeGrade=row["ctcae_grade"], serious=row["serious"], priority=row["priority"], outcome=row["outcome"], acknowledged=row["acknowledged"], snsPublished=row["sns_published"], createdAt=row["created_at"]) for row in rows]
            return NotificationListResponse(status="success", total=total, page=page, pageSize=pageSize, notifications=notifications)
        except HTTPException:
            raise
        except Psycopg2Error as exc:
            logger.error(json.dumps({"event": "db_error", "error": str(exc)}), exc_info=True)
            raise HTTPException(status_code=status.HTTP_503_SERVICE_UNAVAILABLE, detail="Database Error")
        except Exception as exc:
            logger.error(json.dumps({"event": "unexpected_error", "error": str(exc)}), exc_info=True)
            raise HTTPException(status_code=status.HTTP_500_INTERNAL_SERVER_ERROR, detail="Internal Error")
        finally:
            if conn is not None:
                release_conn(conn)

    def _validate_business_rules(self, payload: AdverseEventCreateRequest) -> None:
        if payload.ctcaeGrade < 1 or payload.ctcaeGrade > 5:
            logger.error(json.dumps({"event": "validation_failure", "rule": "INVALID_CTCAE_GRADE"}))
            raise HTTPException(status_code=status.HTTP_422_UNPROCESSABLE_ENTITY, detail="Validation Error")
        if len(payload.narrative) > 2000:
            logger.error(json.dumps({"event": "validation_failure", "rule": "NARRATIVE_TOO_LONG"}))
            raise HTTPException(status_code=status.HTTP_422_UNPROCESSABLE_ENTITY, detail="Validation Error")
        if payload.outcome not in {"ONGOING", "RESOLVED", "FATAL", "UNKNOWN"}:
            logger.error(json.dumps({"event": "validation_failure", "rule": "INVALID_OUTCOME"}))
            raise HTTPException(status_code=status.HTTP_422_UNPROCESSABLE_ENTITY, detail="Validation Error")
        if payload.actionTaken not in {"NONE", "DOSE_REDUCED", "DRUG_WITHDRAWN", "HOSPITALISED"}:
            logger.error(json.dumps({"event": "validation_failure", "rule": "INVALID_ACTION_TAKEN"}))
            raise HTTPException(status_code=status.HTTP_422_UNPROCESSABLE_ENTITY, detail="Validation Error")
        if payload.ctcaeGrade >= 3 and payload.serious is not True:
            payload.serious = True
        if payload.ctcaeGrade == 5:
            payload.outcome = "FATAL"

    def _validate_notification_filters(self, ctcaeGrade: int | None, priority: str | None, dateFrom: str | None, dateTo: str | None, page: int, pageSize: int) -> None:
        if ctcaeGrade is not None and (ctcaeGrade < 1 or ctcaeGrade > 5):
            logger.error(json.dumps({"event": "validation_failure", "rule": "INVALID_QUERY_PARAM"}))
            raise HTTPException(status_code=status.HTTP_400_BAD_REQUEST, detail="Parsing Error")
        if priority is not None and priority not in {"HIGH", "NORMAL"}:
            logger.error(json.dumps({"event": "validation_failure", "rule": "INVALID_QUERY_PARAM"}))
            raise HTTPException(status_code=status.HTTP_400_BAD_REQUEST, detail="Parsing Error")
        if dateFrom is not None:
            isoparse(dateFrom)
        if dateTo is not None:
            isoparse(dateTo)
        if page < 1 or pageSize < 1 or pageSize > 100:
            logger.error(json.dumps({"event": "validation_failure", "rule": "INVALID_QUERY_PARAM"}))
            raise HTTPException(status_code=status.HTTP_400_BAD_REQUEST, detail="Parsing Error")

    def _generate_ae_id(self, cursor: Any) -> str:
        cursor.execute("SELECT 'AE-' || TO_CHAR(NOW(),'YYYY') || '-' || LPAD(NEXTVAL('ae_id_seq')::text, 6, '0')")
        row = cursor.fetchone()
        if row is None:
            raise HTTPException(status_code=status.HTTP_500_INTERNAL_SERVER_ERROR, detail="Internal Error")
        return row[0]

    def _generate_notification_id(self, cursor: Any) -> str:
        cursor.execute("SELECT 'NOTIF-' || TO_CHAR(NOW(),'YYYY') || '-' || LPAD(NEXTVAL('notif_id_seq')::text, 6, '0')")
        row = cursor.fetchone()
        if row is None:
            raise HTTPException(status_code=status.HTTP_500_INTERNAL_SERVER_ERROR, detail="Internal Error")
        return row[0]

    def _notification_stub(self) -> None:
        try:
            _ = False
            _ = None
        except Exception:
            return

    def _get_connection_with_retry(self):
        last_error: Exception | None = None
        for attempt in range(3):
            try:
                return get_conn()
            except Exception as exc:
                last_error = exc
                logger.error(json.dumps({"event": "db_connection_failed", "error": str(exc)}))
                if attempt < 2:
                    sleep(0.2)
        raise RuntimeError("DB connection failed") from last_error

import json
import logging
import time
from dataclasses import asdict
from datetime import UTC, datetime
from typing import Any

import psycopg2
from fastapi import HTTPException

from app.db.connection import get_conn, release_conn
from app.models.adverse_events_model import NotificationRecord
from app.schemas.adverse_events_schema import (
    AdverseEventCreateRequest,
    AdverseEventCreateResponse,
    NotificationListResponse,
    NotificationResponseItem,
)

logger = logging.getLogger(__name__)


class AdverseEventsService:
    def submit_adverse_event(self, payload: AdverseEventCreateRequest) -> AdverseEventCreateResponse:
        logger.info(json.dumps({"event": "service_start", "operation": "submit_adverse_event", "resource": "adverse-events"}))
        coerced = payload.model_copy(deep=True)
        if coerced.ctcaeGrade >= 3:
            coerced.serious = True
        if coerced.ctcaeGrade == 5:
            coerced.outcome = "FATAL"

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
                    raise HTTPException(status_code=404, detail="Resource Not Found")

                logger.info(json.dumps({"event": "db_operation", "table": "trial_enrolments", "operation": "SELECT"}))
                cursor.execute(
                    "SELECT id FROM trial_enrolments WHERE trial_id = %s AND patient_id = %s AND status = 'ENROLLED'",
                    (coerced.trialId, coerced.patientId),
                )
                if cursor.fetchone() is None:
                    raise HTTPException(status_code=404, detail="Resource Not Found")

                logger.info(json.dumps({"event": "db_operation", "table": "adverse_events", "operation": "SELECT"}))
                cursor.execute(
                    "SELECT ae_id FROM adverse_events WHERE trial_id = %s AND patient_id = %s AND ae_term_code = %s AND ctcae_grade = %s AND submitted_at >= NOW() - INTERVAL '60 seconds' ORDER BY submitted_at DESC LIMIT 1",
                    (coerced.trialId, coerced.patientId, coerced.aeTermCode, coerced.ctcaeGrade),
                )
                duplicate_row = cursor.fetchone()
                if duplicate_row is not None:
                    raise HTTPException(status_code=409, detail="Validation Error")

                logger.info(json.dumps({"event": "db_operation", "table": "ae_id_seq", "operation": "SELECT"}))
                cursor.execute("SELECT 'AE-' || TO_CHAR(NOW(),'YYYY') || '-' || LPAD(NEXTVAL('ae_id_seq')::text, 6, '0')")
                ae_row = cursor.fetchone()
                if ae_row is None:
                    conn.rollback()
                    raise HTTPException(status_code=500, detail="Internal Error")
                ae_id = ae_row[0]

                logger.info(json.dumps({"event": "db_operation", "table": "notif_id_seq", "operation": "SELECT"}))
                cursor.execute("SELECT 'NOTIF-' || TO_CHAR(NOW(),'YYYY') || '-' || LPAD(NEXTVAL('notif_id_seq')::text, 6, '0')")
                notif_row = cursor.fetchone()
                if notif_row is None:
                    conn.rollback()
                    raise HTTPException(status_code=500, detail="Internal Error")
                notification_id = notif_row[0]

                received_at = datetime.now(UTC)
                priority = "HIGH" if coerced.ctcaeGrade >= 3 else "NORMAL"

                logger.info(json.dumps({"event": "db_operation", "table": "adverse_events", "operation": "INSERT"}))
                cursor.execute(
                    "INSERT INTO adverse_events (ae_id, trial_id, site_id, patient_id, clinician_id, event_date, ae_term_code, ae_term_name, ctcae_grade, serious, outcome, action_taken, narrative, related_drug_id, reported_by, submitted_at, created_at, updated_at) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, NOW(), NOW())",
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
                        received_at,
                    ),
                )

                logger.info(json.dumps({"event": "db_operation", "table": "ae_notifications", "operation": "INSERT"}))
                cursor.execute(
                    "INSERT INTO ae_notifications (notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, outcome, priority, acknowledged, sns_published, created_at, updated_at) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, FALSE, FALSE, NOW(), NOW())",
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

            try:
                self._notification_stub()
            except Exception as exc:
                logger.error(json.dumps({"event": "notification_stub_noop", "error": str(exc)}))

            return AdverseEventCreateResponse(
                status="success",
                aeId=ae_id,
                notificationId=notification_id,
                snsPublished=False,
                snsMessageId=None,
                receivedAt=received_at,
            )
        except HTTPException:
            if conn is not None:
                try:
                    conn.rollback()
                except Exception:
                    pass
            raise
        except psycopg2.Error as exc:
            if conn is not None:
                try:
                    conn.rollback()
                except Exception:
                    pass
            logger.error(json.dumps({"event": "database_error", "error": str(exc)}))
            raise HTTPException(status_code=503, detail="Database Error") from exc
        except Exception as exc:
            if conn is not None:
                try:
                    conn.rollback()
                except Exception:
                    pass
            logger.error(json.dumps({"event": "unexpected_error", "error": str(exc)}), exc_info=True)
            raise HTTPException(status_code=500, detail="Internal Error") from exc
        finally:
            if conn is not None:
                release_conn(conn)

    def get_notifications(
        self,
        trial_id: str | None,
        site_id: str | None,
        ctcae_grade: int | None,
        serious: bool | None,
        acknowledged: bool | None,
        priority: str | None,
        date_from: str | None,
        date_to: str | None,
        page: int,
        page_size: int,
    ) -> NotificationListResponse:
        logger.info(json.dumps({"event": "service_start", "operation": "get_notifications", "resource": "adverse-events-notifications"}))
        conn = None
        try:
            conn = get_conn()
            try:
                conn.rollback()
            except Exception:
                pass
            conn.autocommit = False
            conditions: list[str] = []
            params: list[Any] = []

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
                if priority not in {"HIGH", "NORMAL"}:
                    raise HTTPException(status_code=400, detail="Parsing Error")
                conditions.append("priority = %s")
                params.append(priority)
            if date_from is not None:
                conditions.append("created_at >= %s")
                params.append(date_from)
            if date_to is not None:
                conditions.append("created_at <= %s")
                params.append(date_to)

            where_clause = f" WHERE {' AND '.join(conditions)}" if conditions else ""
            count_sql = f"SELECT COUNT(*) FROM ae_notifications{where_clause}"
            data_sql = f"SELECT notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, outcome, priority, acknowledged, sns_published, created_at FROM ae_notifications{where_clause} ORDER BY created_at DESC LIMIT %s OFFSET %s"

            with conn.cursor() as cursor:
                logger.info(json.dumps({"event": "db_operation", "table": "ae_notifications", "operation": "SELECT"}))
                cursor.execute(count_sql, tuple(params))
                total_row = cursor.fetchone()
                total = int(total_row[0]) if total_row is not None else 0

                logger.info(json.dumps({"event": "db_operation", "table": "ae_notifications", "operation": "SELECT"}))
                cursor.execute(data_sql, tuple(params + [page_size, (page - 1) * page_size]))
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
                    priority=row[9],
                    outcome=row[8],
                    acknowledged=row[10],
                    snsPublished=row[11],
                    createdAt=row[12],
                )
                for row in rows
            ]
            conn.commit()
            return NotificationListResponse(status="success", total=total, page=page, pageSize=page_size, notifications=notifications)
        except HTTPException:
            if conn is not None:
                try:
                    conn.rollback()
                except Exception:
                    pass
            raise
        except psycopg2.Error as exc:
            if conn is not None:
                try:
                    conn.rollback()
                except Exception:
                    pass
            logger.error(json.dumps({"event": "database_error", "error": str(exc)}))
            raise HTTPException(status_code=503, detail="Database Error") from exc
        except Exception as exc:
            if conn is not None:
                try:
                    conn.rollback()
                except Exception:
                    pass
            logger.error(json.dumps({"event": "unexpected_error", "error": str(exc)}), exc_info=True)
            raise HTTPException(status_code=500, detail="Internal Error") from exc
        finally:
            if conn is not None:
                release_conn(conn)

    def _notification_stub(self) -> None:
        try:
            return None
        except Exception:
            return None


def get_adverse_events_service() -> AdverseEventsService:
    return AdverseEventsService()
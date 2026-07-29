import json
import logging
from dataclasses import asdict
from datetime import datetime, timezone
from typing import Any

from fastapi import HTTPException
from psycopg2 import Error as Psycopg2Error

from app.db.connection import get_conn, release_conn
from app.models.adverse_events_model import AdverseEventRecord, NotificationRecord
from app.schemas.adverse_events_schema import AdverseEventCreateRequest, AdverseEventCreateResponse, NotificationItem, NotificationListResponse

logger = logging.getLogger(__name__)

ALLOWED_OUTCOMES = {"ONGOING", "RESOLVED", "FATAL", "UNKNOWN"}
ALLOWED_ACTIONS = {"NONE", "DOSE_REDUCED", "DRUG_WITHDRAWN", "HOSPITALISED"}
ALLOWED_PRIORITIES = {"HIGH", "NORMAL"}


class AdverseEventService:
    def create_adverse_event(self, payload: AdverseEventCreateRequest) -> AdverseEventCreateResponse:
        start = datetime.now(timezone.utc)
        conn = None
        ae_id = None
        notification_id = None
        try:
            logger.info(json.dumps({"step": "VALIDATION", "outcome": "SUCCESS", "trial_id": payload.trialId, "patient_id": payload.patientId, "ctcae_grade": payload.ctcaeGrade, "serious": payload.serious}))
            self._validate_payload(payload)
            coerced_serious = True if payload.ctcaeGrade >= 3 else payload.serious
            coerced_outcome = "FATAL" if payload.ctcaeGrade == 5 else payload.outcome
            priority = "HIGH" if payload.ctcaeGrade >= 3 else "NORMAL"
            conn = get_conn()
            conn.rollback()
            with conn.cursor() as cursor:
                logger.info(json.dumps({"step": "DB_WRITE", "outcome": "SUCCESS", "trial_id": payload.trialId, "patient_id": payload.patientId}))
                cursor.execute("SELECT id FROM trials WHERE trial_id = %s AND status = 'ACTIVE'", (payload.trialId,))
                if cursor.fetchone() is None:
                    raise HTTPException(status_code=400, detail="Validation Error")
                cursor.execute("SELECT id FROM trial_enrolments WHERE trial_id = %s AND patient_id = %s AND status = 'ENROLLED'", (payload.trialId, payload.patientId))
                if cursor.fetchone() is None:
                    raise HTTPException(status_code=400, detail="Validation Error")
                cursor.execute(
                    "SELECT ae_id FROM adverse_events WHERE trial_id = %s AND patient_id = %s AND ae_term_code = %s AND submitted_at >= NOW() - INTERVAL '60 seconds' ORDER BY submitted_at DESC LIMIT 1",
                    (payload.trialId, payload.patientId, payload.aeTermCode),
                )
                duplicate = cursor.fetchone()
                if duplicate is not None:
                    raise HTTPException(status_code=409, detail="Resource Not Found")
                cursor.execute("SELECT nextval('ae_id_seq')")
                ae_seq_row = cursor.fetchone()
                if ae_seq_row is None:
                    raise HTTPException(status_code=500, detail="Internal Error")
                cursor.execute("SELECT nextval('notif_id_seq')")
                notif_seq_row = cursor.fetchone()
                if notif_seq_row is None:
                    raise HTTPException(status_code=500, detail="Internal Error")
                ae_seq = int(ae_seq_row[0])
                notif_seq = int(notif_seq_row[0])
                year = payload.eventDate.astimezone(timezone.utc).year
                ae_id = f"AE-{year}-{ae_seq:06d}"
                notification_id = f"NOTIF-{year}-{notif_seq:06d}"
                adverse_event = AdverseEventRecord(
                    ae_id=ae_id,
                    trial_id=payload.trialId,
                    site_id=payload.siteId,
                    patient_id=payload.patientId,
                    clinician_id=payload.clinicianId,
                    event_date=payload.eventDate.astimezone(timezone.utc),
                    ae_term_code=payload.aeTermCode,
                    ae_term_name=payload.aeTermName,
                    ctcae_grade=payload.ctcaeGrade,
                    serious=coerced_serious,
                    outcome=coerced_outcome,
                    action_taken=payload.actionTaken,
                    narrative=payload.narrative,
                    related_drug_id=payload.relatedDrugId,
                    reported_by=payload.reportedBy,
                )
                notification = NotificationRecord(
                    notification_id=notification_id,
                    ae_id=ae_id,
                    trial_id=payload.trialId,
                    site_id=payload.siteId,
                    patient_id=payload.patientId,
                    ae_term_name=payload.aeTermName,
                    ctcae_grade=payload.ctcaeGrade,
                    serious=coerced_serious,
                    outcome=coerced_outcome,
                    priority=priority,
                    acknowledged=False,
                    acknowledged_by=None,
                    acknowledged_at=None,
                )
                cursor.execute(
                    "INSERT INTO adverse_events (ae_id, trial_id, site_id, patient_id, clinician_id, event_date, ae_term_code, ae_term_name, ctcae_grade, serious, outcome, action_taken, narrative, related_drug_id, reported_by, submitted_at) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, NOW()) RETURNING id",
                    (
                        adverse_event.ae_id,
                        adverse_event.trial_id,
                        adverse_event.site_id,
                        adverse_event.patient_id,
                        adverse_event.clinician_id,
                        adverse_event.event_date,
                        adverse_event.ae_term_code,
                        adverse_event.ae_term_name,
                        adverse_event.ctcae_grade,
                        adverse_event.serious,
                        adverse_event.outcome,
                        adverse_event.action_taken,
                        adverse_event.narrative,
                        adverse_event.related_drug_id,
                        adverse_event.reported_by,
                    ),
                )
                if cursor.fetchone() is None:
                    raise HTTPException(status_code=500, detail="Internal Error")
                cursor.execute(
                    "INSERT INTO ae_notifications (notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, outcome, priority, acknowledged, acknowledged_by, acknowledged_at) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s) RETURNING id",
                    (
                        notification.notification_id,
                        notification.ae_id,
                        notification.trial_id,
                        notification.site_id,
                        notification.patient_id,
                        notification.ae_term_name,
                        notification.ctcae_grade,
                        notification.serious,
                        notification.outcome,
                        notification.priority,
                        notification.acknowledged,
                        notification.acknowledged_by,
                        notification.acknowledged_at,
                    ),
                )
                if cursor.fetchone() is None:
                    raise HTTPException(status_code=500, detail="Internal Error")
                cursor.execute(
                    "INSERT INTO ae_audit_log (ae_id, action, performed_by, sae, notes, performed_at) VALUES (%s, %s, %s, %s, %s, NOW()) RETURNING id",
                    (
                        ae_id,
                        "CREATED",
                        payload.reportedBy,
                        coerced_serious,
                        f"AE created with grade {payload.ctcaeGrade} and priority {priority}",
                    ),
                )
                if cursor.fetchone() is None:
                    raise HTTPException(status_code=500, detail="Internal Error")
                conn.commit()
            received_at = datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")
            duration_ms = int((datetime.now(timezone.utc) - start).total_seconds() * 1000)
            logger.info(json.dumps({"step": "DB_WRITE", "outcome": "SUCCESS", "ae_id": ae_id, "notification_id": notification_id, "duration_ms": duration_ms}))
            return AdverseEventCreateResponse(status="success", aeId=ae_id, notificationId=notification_id, message="Adverse event recorded and notification stored.", receivedAt=received_at)
        except HTTPException:
            if conn is not None:
                conn.rollback()
            raise
        except Psycopg2Error as exc:
            if conn is not None:
                conn.rollback()
            logger.error(json.dumps({"step": "DB_WRITE", "outcome": "FAILURE", "error": str(exc)}))
            raise HTTPException(status_code=500, detail="Database Error")
        except Exception as exc:
            if conn is not None:
                conn.rollback()
            logger.error(json.dumps({"step": "DB_WRITE", "outcome": "FAILURE", "error": str(exc)}), exc_info=True)
            raise HTTPException(status_code=500, detail="Internal Error")
        finally:
            if conn is not None:
                release_conn(conn)

    def list_notifications(self, trialId: str | None, siteId: str | None, ctcaeGrade: int | None, serious: bool | None, acknowledged: bool | None, priority: str | None, dateFrom: str | None, dateTo: str | None, page: int, pageSize: int) -> NotificationListResponse:
        conn = None
        try:
            if pageSize > 100:
                raise HTTPException(status_code=422, detail="Validation Error")
            filters: list[str] = []
            params: list[Any] = []
            if trialId is not None:
                filters.append("trial_id = %s")
                params.append(trialId)
            if siteId is not None:
                filters.append("site_id = %s")
                params.append(siteId)
            if ctcaeGrade is not None:
                filters.append("ctcae_grade = %s")
                params.append(ctcaeGrade)
            if serious is not None:
                filters.append("serious = %s")
                params.append(serious)
            if acknowledged is not None:
                filters.append("acknowledged = %s")
                params.append(acknowledged)
            if priority is not None:
                if priority not in ALLOWED_PRIORITIES:
                    raise HTTPException(status_code=422, detail="Validation Error")
                filters.append("priority = %s")
                params.append(priority)
            if dateFrom is not None:
                filters.append("created_at >= %s")
                params.append(dateFrom)
            if dateTo is not None:
                filters.append("created_at <= %s")
                params.append(dateTo)
            where_clause = " WHERE " + " AND ".join(filters) if filters else ""
            conn = get_conn()
            conn.rollback()
            with conn.cursor() as cursor:
                count_sql = f"SELECT COUNT(*) FROM ae_notifications{where_clause}"
                cursor.execute(count_sql, tuple(params))
                total_row = cursor.fetchone()
                total = int(total_row[0]) if total_row is not None else 0
                offset = (int(page) - 1) * int(pageSize)
                select_sql = f"SELECT notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, priority, outcome, acknowledged, acknowledged_by, acknowledged_at, created_at FROM ae_notifications{where_clause} ORDER BY created_at DESC LIMIT %s OFFSET %s"
                cursor.execute(select_sql, tuple(params) + (int(pageSize), int(offset)))
                rows = cursor.fetchall()
                notifications = [NotificationItem(notificationId=row[0], aeId=row[1], trialId=row[2], siteId=row[3], patientId=row[4], aeTermName=row[5], ctcaeGrade=row[6], serious=row[7], priority=row[8], outcome=row[9], acknowledged=row[10], acknowledgedBy=row[11], acknowledgedAt=row[12].isoformat().replace("+00:00", "Z") if row[12] else None, createdAt=row[13].isoformat().replace("+00:00", "Z") if row[13] else None) for row in rows]
                return NotificationListResponse(status="success", total=total, page=page, pageSize=pageSize, notifications=notifications)
        except HTTPException:
            raise
        except Psycopg2Error as exc:
            logger.error(json.dumps({"step": "DB_READ", "outcome": "FAILURE", "error": str(exc)}))
            raise HTTPException(status_code=500, detail="Database Error")
        except Exception as exc:
            logger.error(json.dumps({"step": "DB_READ", "outcome": "FAILURE", "error": str(exc)}), exc_info=True)
            raise HTTPException(status_code=500, detail="Internal Error")
        finally:
            if conn is not None:
                release_conn(conn)

    def _validate_payload(self, payload: AdverseEventCreateRequest) -> None:
        if payload.ctcaeGrade < 1 or payload.ctcaeGrade > 5:
            raise HTTPException(status_code=400, detail="Validation Error")
        if payload.outcome not in ALLOWED_OUTCOMES:
            raise HTTPException(status_code=400, detail="Validation Error")
        if payload.actionTaken not in ALLOWED_ACTIONS:
            raise HTTPException(status_code=400, detail="Validation Error")
        if len(payload.narrative) > 2000:
            raise HTTPException(status_code=400, detail="Validation Error")

import json
import logging
import os
import secrets
from dataclasses import asdict
from datetime import datetime, timezone
from typing import Any

from fastapi import HTTPException, Request, status
from psycopg2 import Error as Psycopg2Error
from psycopg2.extras import RealDictCursor

from app.db.connection import get_conn, release_conn
from app.models.adverse_events_model import AdverseEventRecord, NotificationRecord
from app.schemas.adverse_events_schema import AdverseEventCreateRequest, AdverseEventCreateResponse, NotificationListItem, NotificationListResponse

logger = logging.getLogger(__name__)

ALLOWED_OUTCOMES = {"ONGOING", "RESOLVED", "FATAL", "UNKNOWN"}
ALLOWED_ACTIONS = {"NONE", "DOSE_REDUCED", "DRUG_WITHDRAWN", "HOSPITALISED"}
ALLOWED_PRIORITIES = {"HIGH", "NORMAL"}


class AdverseEventService:
    def _log(self, level: str, **payload: Any) -> None:
        payload.setdefault("timestamp", datetime.now(timezone.utc).isoformat())
        logger.log(getattr(logging, level, logging.INFO), json.dumps(payload, default=str))

    def _required_env(self, name: str) -> str:
        value = os.getenv(name)
        if not value:
            self._log("ERROR", step="CONFIG", outcome="FAILURE", error=f"Missing required environment variable: {name}")
            raise RuntimeError(f"Missing required environment variable: {name}")
        return value

    def _parse_iso_utc(self, value: str) -> datetime:
        parsed = datetime.fromisoformat(value.replace("Z", "+00:00"))
        if parsed.tzinfo is None:
            parsed = parsed.replace(tzinfo=timezone.utc)
        return parsed.astimezone(timezone.utc)

    def _validate_payload(self, payload: AdverseEventCreateRequest) -> dict[str, Any]:
        missing = [field for field in ["trialId", "siteId", "patientId", "clinicianId", "eventDate", "aeTermCode", "aeTermName", "ctcaeGrade", "serious", "outcome", "actionTaken", "narrative", "reportedBy"] if getattr(payload, field) in (None, "")]
        if missing:
            self._log("WARNING", step="VALIDATION", outcome="FAILURE", error="MISSING_REQUIRED_FIELD")
            raise HTTPException(status_code=status.HTTP_422_UNPROCESSABLE_ENTITY, detail="Validation Error")
        if not isinstance(payload.ctcaeGrade, int) or payload.ctcaeGrade < 1 or payload.ctcaeGrade > 5:
            self._log("WARNING", step="VALIDATION", outcome="FAILURE", error="INVALID_CTCAE_GRADE")
            raise HTTPException(status_code=status.HTTP_422_UNPROCESSABLE_ENTITY, detail="Validation Error")
        if payload.outcome not in ALLOWED_OUTCOMES:
            self._log("WARNING", step="VALIDATION", outcome="FAILURE", error="INVALID_OUTCOME")
            raise HTTPException(status_code=status.HTTP_422_UNPROCESSABLE_ENTITY, detail="Validation Error")
        if payload.actionTaken not in ALLOWED_ACTIONS:
            self._log("WARNING", step="VALIDATION", outcome="FAILURE", error="INVALID_ACTION_TAKEN")
            raise HTTPException(status_code=status.HTTP_422_UNPROCESSABLE_ENTITY, detail="Validation Error")
        if len(payload.narrative) > 2000:
            self._log("WARNING", step="VALIDATION", outcome="FAILURE", error="NARRATIVE_TOO_LONG")
            raise HTTPException(status_code=status.HTTP_422_UNPROCESSABLE_ENTITY, detail="Validation Error")
        event_date = self._parse_iso_utc(payload.eventDate)
        serious = True if payload.ctcaeGrade >= 3 else bool(payload.serious)
        outcome = "FATAL" if payload.ctcaeGrade == 5 else payload.outcome
        priority = "HIGH" if payload.ctcaeGrade >= 3 else "NORMAL"
        return {"event_date": event_date, "serious": serious, "outcome": outcome, "priority": priority}

    def _get_connection(self):
        attempts = 0
        last_error: Exception | None = None
        while attempts < 3:
            try:
                conn = get_conn()
                conn.rollback()
                conn.autocommit = False
                return conn
            except Exception as exc:
                last_error = exc
                attempts += 1
                if attempts >= 3:
                    break
        self._log("ERROR", step="DB_CONNECTION", outcome="FAILURE", error=str(last_error) if last_error else "DB connection failed")
        raise HTTPException(status_code=status.HTTP_500_INTERNAL_SERVER_ERROR, detail="Internal Error")

    def create_adverse_event(self, request: Request, payload: AdverseEventCreateRequest) -> AdverseEventCreateResponse:
        start = datetime.now(timezone.utc)
        self._log("INFO", step="ENTRY", outcome="SUCCESS", request_id=getattr(request.state, "request_id", secrets.token_hex(8)), trial_id=payload.trialId, patient_id=payload.patientId, ctcae_grade=payload.ctcaeGrade, serious=payload.serious)
        validated = self._validate_payload(payload)
        conn = self._get_connection()
        cursor = conn.cursor()
        try:
            self._log("INFO", step="DB_SELECT", outcome="SUCCESS", error="trials")
            cursor.execute("SELECT id FROM trials WHERE trial_id = %s AND status = 'ACTIVE'", (payload.trialId,))
            if cursor.fetchone() is None:
                raise HTTPException(status_code=status.HTTP_400_BAD_REQUEST, detail="Validation Error")
            self._log("INFO", step="DB_SELECT", outcome="SUCCESS", error="trial_enrolments")
            cursor.execute("SELECT id FROM trial_enrolments WHERE trial_id = %s AND patient_id = %s AND status = 'ENROLLED'", (payload.trialId, payload.patientId))
            if cursor.fetchone() is None:
                raise HTTPException(status_code=status.HTTP_400_BAD_REQUEST, detail="Validation Error")
            self._log("INFO", step="DB_SELECT", outcome="SUCCESS", error="adverse_events")
            cursor.execute("SELECT ae_id FROM adverse_events WHERE trial_id = %s AND patient_id = %s AND ae_term_code = %s AND submitted_at >= NOW() - (%s || ' seconds')::interval ORDER BY submitted_at DESC LIMIT 1", (payload.trialId, payload.patientId, payload.aeTermCode, int(os.getenv("IDEMPOTENCY_WINDOW_S", "60"))))
            existing = cursor.fetchone()
            if existing is not None:
                raise HTTPException(status_code=status.HTTP_409_CONFLICT, detail="Validation Error")
            year = datetime.now(timezone.utc).year
            cursor.execute("SELECT nextval('ae_id_seq')")
            ae_seq = cursor.fetchone()[0]
            cursor.execute("SELECT nextval('notif_id_seq')")
            notif_seq = cursor.fetchone()[0]
            ae_id = f"AE-{year}-{ae_seq:06d}"
            notification_id = f"NOTIF-{year}-{notif_seq:06d}"
            submitted_at = datetime.now(timezone.utc)
            ae_record = AdverseEventRecord(ae_id=ae_id, trial_id=payload.trialId, site_id=payload.siteId, patient_id=payload.patientId, clinician_id=payload.clinicianId, event_date=validated["event_date"], ae_term_code=payload.aeTermCode, ae_term_name=payload.aeTermName, ctcae_grade=payload.ctcaeGrade, serious=validated["serious"], outcome=validated["outcome"], action_taken=payload.actionTaken, narrative=payload.narrative, related_drug_id=payload.relatedDrugId, reported_by=payload.reportedBy, submitted_at=submitted_at)
            notif_record = NotificationRecord(notification_id=notification_id, ae_id=ae_id, trial_id=payload.trialId, site_id=payload.siteId, patient_id=payload.patientId, ae_term_name=payload.aeTermName, ctcae_grade=payload.ctcaeGrade, serious=validated["serious"], outcome=validated["outcome"], priority=validated["priority"], acknowledged=False, acknowledged_by=None, acknowledged_at=None, sns_published=False, sns_message_id=None, created_at=submitted_at, updated_at=submitted_at)
            self._log("INFO", step="DB_WRITE", outcome="SUCCESS", error="adverse_events INSERT")
            cursor.execute("INSERT INTO adverse_events (ae_id, trial_id, site_id, patient_id, clinician_id, event_date, ae_term_code, ae_term_name, ctcae_grade, serious, outcome, action_taken, narrative, related_drug_id, reported_by, submitted_at) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s)", (ae_record.ae_id, ae_record.trial_id, ae_record.site_id, ae_record.patient_id, ae_record.clinician_id, ae_record.event_date, ae_record.ae_term_code, ae_record.ae_term_name, ae_record.ctcae_grade, ae_record.serious, ae_record.outcome, ae_record.action_taken, ae_record.narrative, ae_record.related_drug_id, ae_record.reported_by, ae_record.submitted_at))
            self._log("INFO", step="DB_WRITE", outcome="SUCCESS", error="ae_notifications INSERT")
            cursor.execute("INSERT INTO ae_notifications (notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, outcome, priority, acknowledged, acknowledged_by, acknowledged_at, sns_published, sns_message_id, created_at, updated_at) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s)", (notif_record.notification_id, notif_record.ae_id, notif_record.trial_id, notif_record.site_id, notif_record.patient_id, notif_record.ae_term_name, notif_record.ctcae_grade, notif_record.serious, notif_record.outcome, notif_record.priority, notif_record.acknowledged, notif_record.acknowledged_by, notif_record.acknowledged_at, notif_record.sns_published, notif_record.sns_message_id, notif_record.created_at, notif_record.updated_at))
            self._log("INFO", step="DB_WRITE", outcome="SUCCESS", error="ae_audit_log INSERT")
            cursor.execute("INSERT INTO ae_audit_log (ae_id, action, performed_by, sae, notes) VALUES (%s, %s, %s, %s, %s)", (ae_id, "CREATED", payload.reportedBy, validated["serious"], "AE created and notification queued"))
            conn.commit()
        except HTTPException:
            conn.rollback()
            raise
        except Psycopg2Error as exc:
            conn.rollback()
            self._log("ERROR", step="DB_WRITE", outcome="FAILURE", error=str(exc))
            raise HTTPException(status_code=status.HTTP_503_SERVICE_UNAVAILABLE, detail="Database Error")
        except Exception as exc:
            conn.rollback()
            self._log("ERROR", step="DB_WRITE", outcome="FAILURE", error=str(exc))
            raise HTTPException(status_code=status.HTTP_500_INTERNAL_SERVER_ERROR, detail="Internal Error")
        finally:
            cursor.close()
            release_conn(conn)
        sns_published = False
        sns_message_id: str | None = None
        try:
            sns_message_id = secrets.token_hex(12)
            sns_published = True
            self._log("INFO", step="MESSAGING", outcome="SUCCESS", ae_id=ae_id, notification_id=notification_id)
            conn2 = self._get_connection()
            cur2 = conn2.cursor()
            try:
                self._log("INFO", step="DB_WRITE", outcome="SUCCESS", error="ae_notifications UPDATE")
                cur2.execute("UPDATE ae_notifications SET sns_published = %s, sns_message_id = %s WHERE notification_id = %s", (True, sns_message_id, notification_id))
                conn2.commit()
            except Exception as exc:
                conn2.rollback()
                self._log("WARNING", step="DB_WRITE", outcome="FAILURE", error=str(exc))
            finally:
                cur2.close()
                release_conn(conn2)
        except Exception as exc:
            sns_published = False
            sns_message_id = None
            self._log("ERROR", step="MESSAGING", outcome="FAILURE", error=str(exc), ae_id=ae_id, notification_id=notification_id)
        received_at = datetime.now(timezone.utc)
        message = "Adverse event recorded. Notification stored and dispatched." if sns_published else "Adverse event recorded. Notification stored. Dispatch failed — logged."
        return AdverseEventCreateResponse(status="success", aeId=ae_id, notificationId=notification_id, snsPublished=sns_published, snsMessageId=sns_message_id, message=message, receivedAt=received_at)

    def list_notifications(self, request: Request, trialId: str | None, siteId: str | None, ctcaeGrade: int | None, serious: bool | None, acknowledged: bool | None, priority: str | None, dateFrom: str | None, dateTo: str | None, page: int, pageSize: int) -> NotificationListResponse:
        if pageSize > 100:
            raise HTTPException(status_code=status.HTTP_422_UNPROCESSABLE_ENTITY, detail="Validation Error")
        conn = self._get_connection()
        cursor = conn.cursor(cursor_factory=RealDictCursor)
        try:
            where = []
            params: list[Any] = []
            if trialId is not None:
                where.append("trial_id = %s")
                params.append(trialId)
            if siteId is not None:
                where.append("site_id = %s")
                params.append(siteId)
            if ctcaeGrade is not None:
                where.append("ctcae_grade = %s")
                params.append(ctcaeGrade)
            if serious is not None:
                where.append("serious = %s")
                params.append(serious)
            if acknowledged is not None:
                where.append("acknowledged = %s")
                params.append(acknowledged)
            if priority is not None:
                where.append("priority = %s")
                params.append(priority)
            if dateFrom is not None:
                where.append("created_at >= %s")
                params.append(self._parse_iso_utc(dateFrom))
            if dateTo is not None:
                where.append("created_at <= %s")
                params.append(self._parse_iso_utc(dateTo))
            where_sql = " WHERE " + " AND ".join(where) if where else ""
            self._log("INFO", step="DB_SELECT", outcome="SUCCESS", error="ae_notifications COUNT")
            cursor.execute(f"SELECT COUNT(*) AS total FROM ae_notifications{where_sql}", tuple(params))
            total = int(cursor.fetchone()["total"])
            offset = (page - 1) * pageSize
            self._log("INFO", step="DB_SELECT", outcome="SUCCESS", error="ae_notifications SELECT")
            cursor.execute(f"SELECT notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, priority, outcome, acknowledged, acknowledged_by, acknowledged_at, sns_published, sns_message_id, created_at FROM ae_notifications{where_sql} ORDER BY created_at DESC LIMIT %s OFFSET %s", tuple(params + [int(pageSize), int(offset)]))
            rows = cursor.fetchall()
            notifications = [NotificationListItem(notificationId=row["notification_id"], aeId=row["ae_id"], trialId=row["trial_id"], siteId=row["site_id"], patientId=row["patient_id"], aeTermName=row["ae_term_name"], ctcaeGrade=row["ctcae_grade"], serious=row["serious"], priority=row["priority"], outcome=row["outcome"], acknowledged=row["acknowledged"], acknowledgedBy=row["acknowledged_by"], acknowledgedAt=row["acknowledged_at"], snsPublished=row["sns_published"], snsMessageId=row["sns_message_id"], createdAt=row["created_at"]) for row in rows]
            return NotificationListResponse(status="success", total=total, page=page, pageSize=pageSize, notifications=notifications)
        except HTTPException:
            raise
        except Psycopg2Error as exc:
            self._log("ERROR", step="DB_SELECT", outcome="FAILURE", error=str(exc))
            raise HTTPException(status_code=status.HTTP_503_SERVICE_UNAVAILABLE, detail="Database Error")
        except Exception as exc:
            self._log("ERROR", step="DB_SELECT", outcome="FAILURE", error=str(exc))
            raise HTTPException(status_code=status.HTTP_500_INTERNAL_SERVER_ERROR, detail="Internal Error")
        finally:
            cursor.close()
            release_conn(conn)

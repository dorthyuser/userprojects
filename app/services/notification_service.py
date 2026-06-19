import json
import logging
from datetime import datetime, timezone

from fastapi import HTTPException, Request

from app.db.connection import get_conn, release_conn
from app.schemas.notification_schema import NotificationListResponse, NotificationQueryParams, NotificationResponseItem
from app.services.validator import validate_notification_query_params

logger = logging.getLogger(__name__)


def _utc_now() -> datetime:
    return datetime.now(timezone.utc)


def list_notifications(params: NotificationQueryParams, request: Request) -> NotificationListResponse:
    logger.info(json.dumps({"operation": "list_notifications", "resource": "notification"}))
    validated = validate_notification_query_params(params)
    conn = None
    try:
        conn = get_conn()
        try:
            conn.rollback()
        except Exception:
            pass
        where_clauses = []
        query_params = []
        if validated.trialId is not None:
            where_clauses.append("trial_id = %s")
            query_params.append(validated.trialId)
        if validated.siteId is not None:
            where_clauses.append("site_id = %s")
            query_params.append(validated.siteId)
        if validated.ctcaeGrade is not None:
            where_clauses.append("ctcae_grade = %s")
            query_params.append(validated.ctcaeGrade)
        if validated.serious is not None:
            where_clauses.append("serious = %s")
            query_params.append(validated.serious)
        if validated.acknowledged is not None:
            where_clauses.append("acknowledged = %s")
            query_params.append(validated.acknowledged)
        if validated.priority is not None:
            where_clauses.append("priority = %s")
            query_params.append(validated.priority)
        if validated.dateFrom is not None:
            where_clauses.append("created_at >= %s")
            query_params.append(validated.dateFrom)
        if validated.dateTo is not None:
            where_clauses.append("created_at <= %s")
            query_params.append(validated.dateTo)
        where_sql = " WHERE " + " AND ".join(where_clauses) if where_clauses else ""
        with conn.cursor() as cursor:
            logger.info(json.dumps({"table": "ae_notifications", "operation": "SELECT"}))
            cursor.execute(f"SELECT COUNT(*) FROM ae_notifications{where_sql}", tuple(query_params))
            total_row = cursor.fetchone()
            total = int(total_row[0]) if total_row is not None else 0
            page_size = validated.pageSize
            offset = (validated.page - 1) * page_size
            logger.info(json.dumps({"table": "ae_notifications", "operation": "SELECT"}))
            cursor.execute(
                f"SELECT notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, priority, outcome, acknowledged, acknowledged_by, acknowledged_at, sns_published, sns_message_id, created_at FROM ae_notifications{where_sql} ORDER BY created_at DESC LIMIT %s OFFSET %s",
                tuple(query_params + [page_size, offset]),
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
                acknowledgedBy=row[11],
                acknowledgedAt=row[12],
                snsPublished=row[13],
                snsMessageId=row[14],
                createdAt=row[15],
            )
            for row in rows
        ]
        return NotificationListResponse(status="success", total=total, page=validated.page, pageSize=page_size, notifications=notifications)
    except HTTPException:
        raise
    except Exception as exc:
        logger.error("Unexpected error: %s", str(exc), exc_info=True)
        raise HTTPException(status_code=500, detail="Internal Error")
    finally:
        if conn is not None:
            release_conn(conn)

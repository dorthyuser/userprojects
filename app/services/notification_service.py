import json
import logging
from datetime import datetime, timezone
from typing import Any

from fastapi import HTTPException, Request

from app.db.connection import get_conn, release_conn
from app.schemas.notification_schema import NotificationListResponse, NotificationQueryParams, NotificationResponseItem

logger = logging.getLogger(__name__)


def _utc_now() -> datetime:
    return datetime.now(timezone.utc)


def list_notifications(params: NotificationQueryParams, request: Request) -> NotificationListResponse:
    logger.info("notification_list_start")
    conn = None
    try:
        conn = get_conn()
        conn.rollback()
        where_clauses: list[str] = []
        query_params: list[Any] = []
        if params.trialId is not None:
            where_clauses.append("trial_id = %s")
            query_params.append(params.trialId)
        if params.siteId is not None:
            where_clauses.append("site_id = %s")
            query_params.append(params.siteId)
        if params.ctcaeGrade is not None:
            where_clauses.append("ctcae_grade = %s")
            query_params.append(params.ctcaeGrade)
        if params.serious is not None:
            where_clauses.append("serious = %s")
            query_params.append(params.serious)
        if params.acknowledged is not None:
            where_clauses.append("acknowledged = %s")
            query_params.append(params.acknowledged)
        if params.priority is not None:
            where_clauses.append("priority = %s")
            query_params.append(params.priority)
        if params.dateFrom is not None:
            where_clauses.append("created_at >= %s")
            query_params.append(params.dateFrom)
        if params.dateTo is not None:
            where_clauses.append("created_at <= %s")
            query_params.append(params.dateTo)
        where_sql = " WHERE " + " AND ".join(where_clauses) if where_clauses else ""
        count_sql = f"SELECT COUNT(*) FROM ae_notifications{where_sql}"
        data_sql = f"SELECT notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, priority, outcome, acknowledged, acknowledged_by, acknowledged_at, sns_published, sns_message_id, created_at FROM ae_notifications{where_sql} ORDER BY created_at DESC LIMIT %s OFFSET %s"
        with conn.cursor() as cursor:
            logger.info(json.dumps({"step": "DB_WRITE", "operation": "SELECT", "table": "ae_notifications"}))
            cursor.execute(count_sql, tuple(query_params))
            total_row = cursor.fetchone()
            total = int(total_row[0]) if total_row else 0
            cursor.execute(data_sql, tuple(query_params + [params.pageSize, (params.page - 1) * params.pageSize]))
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
                acknowledgedAt=row[12].isoformat().replace("+00:00", "Z") if row[12] else None,
                snsPublished=row[13],
                snsMessageId=row[14],
                createdAt=row[15].isoformat().replace("+00:00", "Z") if row[15] else None,
            )
            for row in rows
        ]
        return NotificationListResponse(status="success", total=total, page=params.page, pageSize=params.pageSize, notifications=notifications)
    except HTTPException:
        raise
    except Exception as exc:
        logger.error("Database error: %s", str(exc), exc_info=True)
        raise HTTPException(status_code=503, detail="Database Error") from exc
    finally:
        if conn is not None:
            release_conn(conn)

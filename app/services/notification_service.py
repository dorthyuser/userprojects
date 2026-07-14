import json
import logging
from datetime import datetime, timezone
from typing import Any

from fastapi import HTTPException

from app.db.connection import get_conn, release_conn
from app.schemas.notification_schema import NotificationItem, NotificationListResponse
from app.services.validator import validate_notification_query

logger = logging.getLogger(__name__)


def get_notifications(
    request_id: str | None = None,
    trialId: str | None = None,
    siteId: str | None = None,
    ctcaeGrade: int | None = None,
    serious: bool | None = None,
    acknowledged: bool | None = None,
    priority: str | None = None,
    dateFrom: str | None = None,
    dateTo: str | None = None,
    page: int = 1,
    pageSize: int = 20,
) -> NotificationListResponse:
    logger.info(json.dumps({"operation": "get_notifications", "resource": "notification"}))
    filters = validate_notification_query(
        trialId=trialId,
        siteId=siteId,
        ctcaeGrade=ctcaeGrade,
        serious=serious,
        acknowledged=acknowledged,
        priority=priority,
        dateFrom=dateFrom,
        dateTo=dateTo,
        page=page,
        pageSize=pageSize,
    )
    conn = None
    try:
        conn = get_conn()
        try:
            conn.rollback()
        except Exception:
            pass
        where_clauses: list[str] = []
        params: list[Any] = []
        if filters["trialId"] is not None:
            where_clauses.append("trial_id = %s")
            params.append(filters["trialId"])
        if filters["siteId"] is not None:
            where_clauses.append("site_id = %s")
            params.append(filters["siteId"])
        if filters["ctcaeGrade"] is not None:
            where_clauses.append("ctcae_grade = %s")
            params.append(filters["ctcaeGrade"])
        if filters["serious"] is not None:
            where_clauses.append("serious = %s")
            params.append(filters["serious"])
        if filters["acknowledged"] is not None:
            where_clauses.append("acknowledged = %s")
            params.append(filters["acknowledged"])
        if filters["priority"] is not None:
            where_clauses.append("priority = %s")
            params.append(filters["priority"])
        if filters["dateFrom"] is not None:
            where_clauses.append("created_at >= %s")
            params.append(filters["dateFrom"])
        if filters["dateTo"] is not None:
            where_clauses.append("created_at <= %s")
            params.append(filters["dateTo"])
        where_sql = " WHERE " + " AND ".join(where_clauses) if where_clauses else ""
        count_sql = f"SELECT COUNT(*) FROM ae_notifications{where_sql}"
        data_sql = f"SELECT notification_id, ae_id, trial_id, site_id, patient_id, ae_term_name, ctcae_grade, serious, priority, outcome, acknowledged, acknowledged_by, acknowledged_at, sns_published, sns_message_id, created_at FROM ae_notifications{where_sql} ORDER BY created_at DESC LIMIT %s OFFSET %s"
        with conn.cursor() as cursor:
            cursor.execute(count_sql, tuple(params))
            total_row = cursor.fetchone()
            total = int(total_row[0]) if total_row is not None else 0
            cursor.execute(data_sql, tuple(params + [filters["pageSize"], (filters["page"] - 1) * filters["pageSize"]]))
            rows = cursor.fetchall()
        notifications = [
            NotificationItem(
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
        return NotificationListResponse(status="success", total=total, page=filters["page"], pageSize=filters["pageSize"], notifications=notifications)
    except HTTPException:
        raise
    except Exception as exc:
        logger.error(json.dumps({"step": "DB_QUERY", "outcome": "FAILURE", "error": str(exc)}))
        raise HTTPException(status_code=500, detail="Internal Error")
    finally:
        if conn is not None:
            release_conn(conn)

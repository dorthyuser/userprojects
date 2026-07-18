import json
import logging
from dataclasses import asdict
from datetime import datetime, timezone
from typing import Any

from fastapi import HTTPException
from psycopg2 import Error as Psycopg2Error

from app.connections.zoho_http_connection import ZohoHttpConnectionConnection, get_zoho_http_connection
from app.db.connection import get_conn, release_conn
from app.models.users_model import SyncSummary, ZohoUserRecord
from app.schemas.users_schema import CreateUserRequest, SyncUsersRequest

logger = logging.getLogger(__name__)
_service_instance: "UsersService | None" = None


def get_users_service() -> "UsersService":
    global _service_instance
    if _service_instance is None:
        _service_instance = UsersService(connection=get_zoho_http_connection())
    return _service_instance


class UsersService:
    def __init__(self, connection: ZohoHttpConnectionConnection) -> None:
        self._connection = connection

    def create_user(self, payload: CreateUserRequest, correlation_id: str | None) -> tuple[int, dict[str, Any]]:
        logger.info("create_user service")
        users = payload.users
        if len(users) != 1:
            raise HTTPException(status_code=422, detail="Validation Error")
        user = users[0]
        if not user.last_name:
            raise HTTPException(status_code=422, detail="Validation Error")
        if not user.email:
            raise HTTPException(status_code=422, detail="Validation Error")
        status_code, body = self._connection.request(
            method="POST",
            path="/crm/v8/users",
            json={"users": [user.model_dump(exclude_none=True)]},
            headers={"X-Correlation-Id": correlation_id or ""},
        )
        if status_code >= 400:
            return status_code, body if isinstance(body, dict) else {"status": "error", "message": str(body)}
        users_data = body.get("users", []) if isinstance(body, dict) else []
        zoho_id = ""
        if users_data:
            details = users_data[0].get("details", {})
            zoho_id = str(details.get("id", ""))
        logger.info(json.dumps({"event": "user_created", "zoho_id": zoho_id, "correlation_id": correlation_id or ""}))
        return 201, {"status": "success", "zoho_id": zoho_id, "email": user.email, "created_at": datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")}

    def get_zoho_user(
        self,
        zoho_id: str,
        user_type: str,
        page: int,
        per_page: int,
        if_modified_since: str | None,
        correlation_id: str | None,
    ) -> tuple[int, dict[str, Any]]:
        logger.info("get_zoho_user service")
        path = f"/crm/v8/users/{zoho_id}"
        params = {"type": user_type, "page": page, "per_page": per_page}
        headers = {"X-Correlation-Id": correlation_id or ""}
        if if_modified_since:
            headers["If-Modified-Since"] = if_modified_since
        return self._connection.request(method="GET", path=path, params=params, headers=headers)

    def list_zoho_users(
        self,
        user_type: str,
        page: int,
        per_page: int,
        if_modified_since: str | None,
        correlation_id: str | None,
    ) -> tuple[int, dict[str, Any]]:
        logger.info("list_zoho_users service")
        params = {"type": user_type, "page": page, "per_page": per_page}
        headers = {"X-Correlation-Id": correlation_id or ""}
        if if_modified_since:
            headers["If-Modified-Since"] = if_modified_since
        return self._connection.request(method="GET", path="/crm/v8/users", params=params, headers=headers)

    def sync_users(self, payload: SyncUsersRequest | None, correlation_id: str | None) -> tuple[int, dict[str, Any]]:
        logger.info("sync_users service")
        conn = get_conn()
        try:
            conn.rollback()
            conn.autocommit = False
            with conn.cursor() as cursor:
                watermark = datetime(1970, 1, 1, tzinfo=timezone.utc)
                if payload and payload.full_sync is False:
                    cursor.execute("SELECT last_synced_at FROM sync_state WHERE sync_key = %s", ("zoho_users",))
                    row = cursor.fetchone()
                    if row and row[0]:
                        watermark = row[0]
                page = 1
                more_records = True
                new_watermark = watermark
                pages_fetched = 0
                zoho_records_read = 0
                upserted = 0
                unchanged = 0
                errors = 0
                error_detail: list[dict[str, str]] = []
                while more_records:
                    status_code, body = self._connection.request(
                        method="GET",
                        path="/crm/v8/users",
                        params={"type": payload.type if payload and payload.type else "AllUsers", "page": page, "per_page": payload.per_page if payload and payload.per_page else 200},
                        headers={"If-Modified-Since": watermark.isoformat(), "X-Correlation-Id": correlation_id or ""},
                    )
                    if status_code == 304:
                        break
                    if status_code >= 400:
                        raise HTTPException(status_code=502, detail="Service Unavailable")
                    pages_fetched += 1
                    info = body.get("info", {}) if isinstance(body, dict) else {}
                    users = body.get("users", []) if isinstance(body, dict) else []
                    more_records = bool(info.get("more_records", False))
                    zoho_records_read += len(users)
                    for record in users:
                        cursor.execute("SAVEPOINT before_upsert")
                        try:
                            zoho_user = ZohoUserRecord.from_zoho(record)
                            if zoho_user.zoho_modified_at <= new_watermark:
                                unchanged += 1
                                cursor.execute("RELEASE SAVEPOINT before_upsert")
                                continue
                            cursor.execute(
                                """
                                INSERT INTO crm_users (
                                    zoho_uid, given_name, family_name, display_name, email_address, phone_number,
                                    mobile_number, account_status, is_confirmed, user_type, zoho_role_id, zoho_role_name,
                                    zoho_profile_id, zoho_profile_name, reports_to_uid, country_code, locale_code,
                                    iana_timezone, zoho_created_at, zoho_modified_at, local_synced_at, local_created_at
                                ) VALUES (
                                    %s, %s, %s, %s, %s, %s,
                                    %s, %s, %s, %s, %s, %s,
                                    %s, %s, %s, %s, %s,
                                    %s, %s, %s, NOW(), NOW()
                                ) ON CONFLICT (zoho_uid) DO UPDATE SET
                                    given_name = EXCLUDED.given_name,
                                    family_name = EXCLUDED.family_name,
                                    display_name = EXCLUDED.display_name,
                                    email_address = EXCLUDED.email_address,
                                    phone_number = EXCLUDED.phone_number,
                                    mobile_number = EXCLUDED.mobile_number,
                                    account_status = EXCLUDED.account_status,
                                    is_confirmed = EXCLUDED.is_confirmed,
                                    user_type = EXCLUDED.user_type,
                                    zoho_role_id = EXCLUDED.zoho_role_id,
                                    zoho_role_name = EXCLUDED.zoho_role_name,
                                    zoho_profile_id = EXCLUDED.zoho_profile_id,
                                    zoho_profile_name = EXCLUDED.zoho_profile_name,
                                    reports_to_uid = EXCLUDED.reports_to_uid,
                                    country_code = EXCLUDED.country_code,
                                    locale_code = EXCLUDED.locale_code,
                                    iana_timezone = EXCLUDED.iana_timezone,
                                    zoho_created_at = EXCLUDED.zoho_created_at,
                                    zoho_modified_at = EXCLUDED.zoho_modified_at,
                                    local_synced_at = NOW()
                                WHERE crm_users.zoho_modified_at < EXCLUDED.zoho_modified_at
                                """,
                                (
                                    zoho_user.zoho_uid,
                                    zoho_user.given_name,
                                    zoho_user.family_name,
                                    zoho_user.display_name,
                                    zoho_user.email_address,
                                    zoho_user.phone_number,
                                    zoho_user.mobile_number,
                                    zoho_user.account_status,
                                    zoho_user.is_confirmed,
                                    zoho_user.user_type,
                                    zoho_user.zoho_role_id,
                                    zoho_user.zoho_role_name,
                                    zoho_user.zoho_profile_id,
                                    zoho_user.zoho_profile_name,
                                    zoho_user.reports_to_uid,
                                    zoho_user.country_code,
                                    zoho_user.locale_code,
                                    zoho_user.iana_timezone,
                                    zoho_user.zoho_created_at,
                                    zoho_user.zoho_modified_at,
                                ),
                            )
                            upserted += 1
                            if zoho_user.zoho_modified_at > new_watermark:
                                new_watermark = zoho_user.zoho_modified_at
                            cursor.execute("RELEASE SAVEPOINT before_upsert")
                        except Exception as exc:
                            cursor.execute("ROLLBACK TO SAVEPOINT before_upsert")
                            errors += 1
                            error_detail.append({"zoho_id": str(record.get("id", "")), "reason": str(exc)})
                            logger.error(str(exc))
                    page += 1
                if pages_fetched > 0 and errors >= 0:
                    cursor.execute(
                        """
                        INSERT INTO sync_state (sync_key, last_synced_at, last_run_at, records_synced)
                        VALUES (%s, %s, NOW(), %s)
                        ON CONFLICT (sync_key) DO UPDATE SET
                            last_synced_at = EXCLUDED.last_synced_at,
                            last_run_at = NOW(),
                            records_synced = EXCLUDED.records_synced
                        """,
                        ("zoho_users", new_watermark, upserted),
                    )
                    conn.commit()
                summary = SyncSummary(
                    status="partial" if errors else "success",
                    watermark_used=watermark.isoformat(),
                    new_watermark=new_watermark.isoformat(),
                    pages_fetched=pages_fetched,
                    zoho_records_read=zoho_records_read,
                    upserted=upserted,
                    unchanged=unchanged,
                    errors=errors,
                    sync_duration_ms=0,
                    error_detail=error_detail or None,
                )
                from dataclasses import asdict
                return 200 if not errors else 207, {k: v for k, v in asdict(summary).items() if v is not None}
        except Psycopg2Error as exc:
            conn.rollback()
            logger.error(str(exc))
            raise HTTPException(status_code=503, detail="Database Error") from exc
        finally:
            release_conn(conn)

    def get_local_user_by_pk(self, user_pk: int, correlation_id: str | None) -> tuple[int, dict[str, Any]]:
        logger.info("get_local_user_by_pk service")
        conn = get_conn()
        try:
            conn.rollback()
            with conn.cursor() as cursor:
                cursor.execute("SELECT * FROM crm_users WHERE user_pk = %s", (user_pk,))
                row = cursor.fetchone()
                if not row:
                    return 404, {"status": "error", "code": "USER_NOT_FOUND", "message": "No local user record found for the given identifier."}
                columns = [desc[0] for desc in cursor.description]
                data = dict(zip(columns, row, strict=False))
                return 200, {"status": "success", "user": data}
        except Psycopg2Error as exc:
            logger.error(str(exc))
            raise HTTPException(status_code=503, detail="Database Error") from exc
        finally:
            release_conn(conn)

    def get_local_user_by_zoho_uid(self, zoho_uid: str, correlation_id: str | None) -> tuple[int, dict[str, Any]]:
        logger.info("get_local_user_by_zoho_uid service")
        conn = get_conn()
        try:
            conn.rollback()
            with conn.cursor() as cursor:
                cursor.execute("SELECT * FROM crm_users WHERE zoho_uid = %s", (zoho_uid,))
                row = cursor.fetchone()
                if not row:
                    return 404, {"status": "error", "code": "USER_NOT_FOUND", "message": "No local user record found for the given identifier."}
                columns = [desc[0] for desc in cursor.description]
                data = dict(zip(columns, row, strict=False))
                return 200, {"status": "success", "user": data}
        except Psycopg2Error as exc:
            logger.error(str(exc))
            raise HTTPException(status_code=503, detail="Database Error") from exc
        finally:
            release_conn(conn)

    def list_local_users(
        self,
        account_status: str | None,
        zoho_role_id: str | None,
        zoho_profile_id: str | None,
        is_confirmed: bool | None,
        synced_after: str | None,
        page: int,
        page_size: int,
        sort_by: str,
        sort_order: str,
        correlation_id: str | None,
    ) -> tuple[int, dict[str, Any]]:
        logger.info("list_local_users service")
        conn = get_conn()
        try:
            conn.rollback()
            with conn.cursor() as cursor:
                where = []
                params: list[Any] = []
                if account_status is not None:
                    where.append("account_status = %s")
                    params.append(account_status)
                if zoho_role_id is not None:
                    where.append("zoho_role_id = %s")
                    params.append(zoho_role_id)
                if zoho_profile_id is not None:
                    where.append("zoho_profile_id = %s")
                    params.append(zoho_profile_id)
                if is_confirmed is not None:
                    where.append("is_confirmed = %s")
                    params.append(is_confirmed)
                if synced_after is not None:
                    where.append("local_synced_at >= %s")
                    params.append(synced_after)
                where_sql = " WHERE " + " AND ".join(where) if where else ""
                allowed_sort = {"family_name", "email_address", "zoho_modified_at", "local_synced_at"}
                safe_sort = sort_by if sort_by in allowed_sort else "family_name"
                safe_order = sort_order.lower() if sort_order.lower() in {"asc", "desc"} else "asc"
                offset = (page - 1) * page_size
                cursor.execute(f"SELECT COUNT(*) FROM crm_users{where_sql}", tuple(params))
                total_count = cursor.fetchone()[0]
                cursor.execute(f"SELECT * FROM crm_users{where_sql} ORDER BY {safe_sort} {safe_order} LIMIT %s OFFSET %s", tuple(params + [page_size, offset]))
                rows = cursor.fetchall()
                columns = [desc[0] for desc in cursor.description]
                users = [dict(zip(columns, row, strict=False)) for row in rows]
                return 200, {"status": "success", "page": page, "page_size": page_size, "total_count": total_count, "users": users}
        except Psycopg2Error as exc:
            logger.error(str(exc))
            raise HTTPException(status_code=503, detail="Database Error") from exc
        finally:
            release_conn(conn)

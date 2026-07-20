import json
import logging
from dataclasses import asdict
from datetime import datetime, timezone
from typing import Any

from fastapi import HTTPException
from psycopg2 import Error as PsycopgError

from app.connections.zoho_http_connection import ZohoHttpConnectionConnection, get_zoho_http_connection
from app.db.connection import get_conn, release_conn
from app.models.users_model import LocalUser, ZohoUser
from app.schemas.users_schema import (
    CreateUserRequest,
    CreateUserResponse,
    DeltaSyncRequest,
    DeltaSyncResponse,
    LocalUserListResponse,
    LocalUserResponse,
    LocalUserSummary,
    SyncErrorDetail,
    ZohoUserListResponse,
    ZohoUserResponse,
)

logger = logging.getLogger(__name__)
_service_instance: "UsersService | None" = None


def get_users_service() -> "UsersService":
    global _service_instance
    if _service_instance is None:
        _service_instance = UsersService(get_zoho_http_connection())
    return _service_instance


class UsersService:
    def __init__(self, connection: ZohoHttpConnectionConnection) -> None:
        self._connection = connection

    def create_user(self, payload: CreateUserRequest, correlation_id: str | None) -> CreateUserResponse:
        logger.info(json.dumps({"event": "service_start", "operation": "create_user", "resource": "users"}))
        if len(payload.users) != 1:
            raise HTTPException(status_code=422, detail="Validation Error")
        user = payload.users[0]
        status_code, body = self._connection.request("GET", "/crm/v8/users", params={"type": "AllUsers", "page": 1, "per_page": 10}, headers={"X-Correlation-Id": correlation_id or ""})
        if status_code == 200 and isinstance(body, dict):
            for existing in body.get("users", []):
                if str(existing.get("email", "")).lower() == user.email.lower():
                    raise HTTPException(status_code=409, detail="A user with this email already exists in the Zoho org.")
        status_code, body = self._connection.request("POST", "/crm/v8/users", json={"users": [user.model_dump(exclude_none=True)]}, headers={"X-Correlation-Id": correlation_id or ""})
        if status_code not in (200, 201):
            raise HTTPException(status_code=502, detail="Service Unavailable")
        details = (((body or {}).get("users") or [{}])[0].get("details") or {}) if isinstance(body, dict) else {}
        zoho_id = str(details.get("id", ""))
        if not zoho_id:
            raise HTTPException(status_code=500, detail="Internal Error")
        logger.info(json.dumps({"event": "zoho_user_created", "zoho_id": zoho_id, "correlation_id": correlation_id or ""}))
        return CreateUserResponse(status="success", zoho_id=zoho_id, email=user.email, created_at=datetime.now(timezone.utc))

    def get_zoho_user(self, zoho_id: str, if_modified_since: str | None, correlation_id: str | None) -> ZohoUserResponse:
        logger.info(json.dumps({"event": "service_start", "operation": "get_zoho_user", "resource": "users"}))
        status_code, body = self._connection.request("GET", f"/crm/v8/users/{zoho_id}", headers={"If-Modified-Since": if_modified_since or "", "X-Correlation-Id": correlation_id or ""})
        if status_code == 404:
            raise HTTPException(status_code=404, detail="Resource Not Found")
        if status_code != 200 or not isinstance(body, dict):
            raise HTTPException(status_code=502, detail="Service Unavailable")
        users = body.get("users") or []
        if not users:
            raise HTTPException(status_code=404, detail="Resource Not Found")
        return ZohoUserResponse(status="success", user=users[0])

    def list_zoho_users(self, type: str, page: int, per_page: int, if_modified_since: str | None, correlation_id: str | None) -> ZohoUserListResponse:
        logger.info(json.dumps({"event": "service_start", "operation": "list_zoho_users", "resource": "users"}))
        status_code, body = self._connection.request("GET", "/crm/v8/users", params={"type": type, "page": page, "per_page": per_page}, headers={"If-Modified-Since": if_modified_since or "", "X-Correlation-Id": correlation_id or ""})
        if status_code == 304:
            return ZohoUserListResponse(status="success", info={"page": page, "per_page": per_page, "count": 0, "more_records": False}, users=[])
        if status_code != 200 or not isinstance(body, dict):
            raise HTTPException(status_code=502, detail="Service Unavailable")
        return ZohoUserListResponse(status="success", info=body.get("info", {"page": page, "per_page": per_page, "count": 0, "more_records": False}), users=body.get("users", []))

    def sync_users(self, payload: DeltaSyncRequest | None, correlation_id: str | None) -> DeltaSyncResponse:
        logger.info(json.dumps({"event": "service_start", "operation": "sync_users", "resource": "users"}))
        conn = get_conn()
        try:
            conn.rollback()
            conn.autocommit = False
            with conn.cursor() as cursor:
                cursor.execute("SELECT last_synced_at FROM sync_state WHERE sync_key = %s", ("zoho_users",))
                row = cursor.fetchone()
                watermark = row[0] if row and row[0] else datetime(1970, 1, 1, tzinfo=timezone.utc)
                if payload and payload.full_sync:
                    watermark = datetime(1970, 1, 1, tzinfo=timezone.utc)
                page = 1
                more_records = True
                new_watermark = watermark
                pages_fetched = 0
                zoho_records_read = 0
                upserted = 0
                unchanged = 0
                errors = 0
                error_detail: list[SyncErrorDetail] = []
                while more_records:
                    status_code, body = self._connection.request("GET", "/crm/v8/users", params={"type": payload.type if payload and payload.type else "AllUsers", "page": page, "per_page": payload.per_page if payload and payload.per_page else 200}, headers={"If-Modified-Since": watermark.isoformat(), "X-Correlation-Id": correlation_id or ""})
                    if status_code == 304:
                        break
                    if status_code != 200 or not isinstance(body, dict):
                        raise HTTPException(status_code=502, detail="Service Unavailable")
                    pages_fetched += 1
                    users = body.get("users") or []
                    info = body.get("info") or {}
                    more_records = bool(info.get("more_records", False))
                    if not users:
                        break
                    for record in users:
                        zoho_records_read += 1
                        modified_raw = record.get("Modified_Time") or record.get("modified_time") or record.get("modifiedTime")
                        modified_at = datetime.fromisoformat(str(modified_raw).replace("Z", "+00:00")) if modified_raw else datetime.now(timezone.utc)
                        if modified_at > new_watermark:
                            new_watermark = modified_at
                        cursor.execute("SAVEPOINT before_upsert")
                        try:
                            cursor.execute(
                                """
                                INSERT INTO crm_users (
                                    zoho_uid, given_name, family_name, display_name, email_address, phone_number, mobile_number,
                                    account_status, is_confirmed, user_type, zoho_role_id, zoho_role_name, zoho_profile_id,
                                    zoho_profile_name, reports_to_uid, country_code, locale_code, iana_timezone,
                                    zoho_created_at, zoho_modified_at, local_synced_at, local_created_at
                                ) VALUES (
                                    %s, %s, %s, %s, %s, %s, %s,
                                    %s, %s, %s, %s, %s, %s,
                                    %s, %s, %s, %s, %s,
                                    %s, %s, NOW(), NOW()
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
                                WHERE crm_users.zoho_modified_at IS NULL OR EXCLUDED.zoho_modified_at > crm_users.zoho_modified_at
                                """,
                                (
                                    str(record.get("id", "")),
                                    record.get("first_name"),
                                    record.get("last_name"),
                                    record.get("full_name"),
                                    record.get("email"),
                                    record.get("phone"),
                                    record.get("mobile"),
                                    record.get("status"),
                                    record.get("confirm"),
                                    record.get("type__s"),
                                    (record.get("role") or {}).get("id"),
                                    (record.get("role") or {}).get("name"),
                                    (record.get("profile") or {}).get("id"),
                                    (record.get("profile") or {}).get("name"),
                                    (record.get("reporting_to") or {}).get("id"),
                                    record.get("country"),
                                    record.get("country_locale"),
                                    record.get("time_zone"),
                                    record.get("Created_Time") or record.get("created_time"),
                                    modified_at,
                                ),
                            )
                            if cursor.rowcount == 0:
                                unchanged += 1
                            else:
                                upserted += 1
                            cursor.execute("RELEASE SAVEPOINT before_upsert")
                        except Exception as exc:
                            cursor.execute("ROLLBACK TO SAVEPOINT before_upsert")
                            errors += 1
                            error_detail.append(SyncErrorDetail(zoho_id=str(record.get("id", "")), reason="email constraint violation"))
                            logger.error(str(exc))
                    page += 1
                if pages_fetched > 0 and errors == 0:
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
                if errors > 0:
                    return DeltaSyncResponse(status="partial", upserted=upserted, errors=errors, error_detail=error_detail)
                return DeltaSyncResponse(status="success", watermark_used=watermark, new_watermark=new_watermark, pages_fetched=pages_fetched, zoho_records_read=zoho_records_read, upserted=upserted, unchanged=unchanged, errors=errors, sync_duration_ms=0)
        except PsycopgError as exc:
            conn.rollback()
            logger.error(str(exc))
            raise HTTPException(status_code=503, detail="Database Error")
        finally:
            release_conn(conn)

    def get_local_user_by_pk(self, user_pk: int, correlation_id: str | None) -> LocalUserResponse:
        logger.info(json.dumps({"event": "service_start", "operation": "get_local_user_by_pk", "resource": "users"}))
        conn = get_conn()
        try:
            conn.rollback()
            with conn.cursor() as cursor:
                logger.info(json.dumps({"event": "db_operation", "table": "crm_users", "operation": "SELECT"}))
                cursor.execute("SELECT user_pk, zoho_uid, given_name, family_name, display_name, email_address, phone_number, mobile_number, account_status, is_confirmed, user_type, zoho_role_id, zoho_role_name, zoho_profile_id, zoho_profile_name, reports_to_uid, country_code, locale_code, iana_timezone, zoho_created_at, zoho_modified_at, local_synced_at FROM crm_users WHERE user_pk = %s", (user_pk,))
                row = cursor.fetchone()
                if not row:
                    raise HTTPException(status_code=404, detail="Resource Not Found")
                return LocalUserResponse(status="success", user=LocalUser(*row))
        finally:
            release_conn(conn)

    def get_local_user_by_zoho_uid(self, zoho_uid: str, correlation_id: str | None) -> LocalUserResponse:
        logger.info(json.dumps({"event": "service_start", "operation": "get_local_user_by_zoho_uid", "resource": "users"}))
        conn = get_conn()
        try:
            conn.rollback()
            with conn.cursor() as cursor:
                logger.info(json.dumps({"event": "db_operation", "table": "crm_users", "operation": "SELECT"}))
                cursor.execute("SELECT user_pk, zoho_uid, given_name, family_name, display_name, email_address, phone_number, mobile_number, account_status, is_confirmed, user_type, zoho_role_id, zoho_role_name, zoho_profile_id, zoho_profile_name, reports_to_uid, country_code, locale_code, iana_timezone, zoho_created_at, zoho_modified_at, local_synced_at FROM crm_users WHERE zoho_uid = %s", (zoho_uid,))
                row = cursor.fetchone()
                if not row:
                    raise HTTPException(status_code=404, detail="Resource Not Found")
                return LocalUserResponse(status="success", user=LocalUser(*row))
        finally:
            release_conn(conn)

    def get_local_users(self, account_status: str | None, zoho_role_id: str | None, zoho_profile_id: str | None, is_confirmed: bool | None, synced_after: str | None, page: int, page_size: int, sort_by: str, sort_order: str, correlation_id: str | None) -> LocalUserListResponse:
        logger.info(json.dumps({"event": "service_start", "operation": "get_local_users", "resource": "users"}))
        conn = get_conn()
        try:
            conn.rollback()
            with conn.cursor() as cursor:
                where = ["1=1"]
                params: list[Any] = []
                if account_status:
                    where.append("account_status = %s")
                    params.append(account_status)
                if zoho_role_id:
                    where.append("zoho_role_id = %s")
                    params.append(zoho_role_id)
                if zoho_profile_id:
                    where.append("zoho_profile_id = %s")
                    params.append(zoho_profile_id)
                if is_confirmed is not None:
                    where.append("is_confirmed = %s")
                    params.append(is_confirmed)
                if synced_after:
                    where.append("local_synced_at >= %s")
                    params.append(synced_after)
                safe_sort = sort_by if sort_by in {"family_name", "email_address", "zoho_modified_at", "local_synced_at"} else "family_name"
                safe_order = sort_order.lower() if sort_order.lower() in {"asc", "desc"} else "asc"
                offset = (page - 1) * page_size
                query = f"SELECT user_pk, zoho_uid, given_name, family_name, display_name, email_address, phone_number, mobile_number, account_status, is_confirmed, user_type, zoho_role_id, zoho_role_name, zoho_profile_id, zoho_profile_name, reports_to_uid, country_code, locale_code, iana_timezone, zoho_created_at, zoho_modified_at, local_synced_at FROM crm_users WHERE {' AND '.join(where)} ORDER BY {safe_sort} {safe_order} LIMIT %s OFFSET %s"
                logger.info(json.dumps({"event": "db_operation", "table": "crm_users", "operation": "SELECT"}))
                cursor.execute(query, tuple(params + [page_size, offset]))
                rows = cursor.fetchall()
                count_query = f"SELECT COUNT(*) FROM crm_users WHERE {' AND '.join(where)}"
                cursor.execute(count_query, tuple(params))
                total_count = cursor.fetchone()[0]
                users = [LocalUserSummary(*row) for row in rows]
                return LocalUserListResponse(status="success", page=page, page_size=page_size, total_count=total_count, users=users)
        finally:
            release_conn(conn)

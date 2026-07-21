import json
import logging
from dataclasses import asdict
from datetime import datetime, timezone
from typing import Any

from fastapi import HTTPException
from psycopg2 import Error as Psycopg2Error

from app.connections.zoho_http_connection import ZohoHttpConnectionConnection, get_zoho_http_connection
from app.db.connection import get_conn, release_conn
from app.models.users_model import LocalUserRecord, ZohoUserRecord
from app.schemas.users_schema import (
    CreateUserRequest,
    CreateUserResponse,
    ErrorResponse,
    GetLocalUserListResponse,
    GetLocalUserResponse,
    GetUserListResponse,
    GetUserResponse,
    LocalUserListItem,
    LocalUserResponse,
    SyncUsersRequest,
    SyncUsersResponse,
    ZohoUserItem,
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

    def create_user(self, payload: CreateUserRequest, correlation_id: str | None) -> tuple[int, Any]:
        logger.info(json.dumps({"event": "service_start", "operation": "create_user", "resource": "users"}))
        try:
            user = payload.users[0]
            status_code, duplicate_body = self._connection.request("GET", "/crm/v8/users", params={"type": "AllUsers", "page": 1, "per_page": 10})
            if status_code == 200 and isinstance(duplicate_body, dict):
                for item in duplicate_body.get("users", []):
                    if item.get("email") == user.email:
                        return 409, ErrorResponse(status="error", code="DUPLICATE_EMAIL", message="A user with this email already exists in the Zoho org.").model_dump()
            body = {"users": [user.model_dump(exclude_none=True)]}
            status_code, zoho_body = self._connection.request("POST", "/crm/v8/users", json=body)
            if status_code not in (200, 201):
                return status_code, zoho_body
            zoho_id = ""
            if isinstance(zoho_body, dict):
                users = zoho_body.get("users") or []
                if users:
                    details = users[0].get("details") or {}
                    zoho_id = str(details.get("id") or "")
            if not zoho_id:
                raise HTTPException(status_code=500, detail="Internal Error")
            logger.info(json.dumps({"event": "zoho_user_created", "zoho_id": zoho_id, "correlation_id": correlation_id or ""}))
            return 201, CreateUserResponse(status="success", zoho_id=zoho_id, email=user.email, created_at=datetime.now(timezone.utc)).model_dump()
        except Psycopg2Error as exc:
            logger.error(json.dumps({"event": "db_error", "error": str(exc)}))
            raise HTTPException(status_code=503, detail="Database Error") from exc
        except HTTPException:
            raise
        except Exception as exc:
            logger.error(json.dumps({"event": "unexpected_error", "error": str(exc)}), exc_info=True)
            raise HTTPException(status_code=500, detail="Internal Error") from exc

    def get_zoho_users(
        self,
        zoho_id: str | None,
        type_value: str,
        page: int,
        per_page: int,
        if_modified_since: str | None,
        correlation_id: str | None,
    ) -> tuple[int, Any]:
        logger.info(json.dumps({"event": "service_start", "operation": "get_zoho_users", "resource": "users"}))
        try:
            path = "/crm/v8/users"
            if zoho_id is not None:
                path = f"/crm/v8/users/{zoho_id}"
                status_code, body = self._connection.request("GET", path)
                if status_code == 404:
                    return 404, ErrorResponse(status="error", code="USER_NOT_FOUND", message="No Zoho user found for the given ID.").model_dump()
                if isinstance(body, dict) and body.get("user"):
                    return 200, ZohoUserResponse(status="success", user=body["user"]).model_dump()
                return status_code, body
            params = {"type": type_value, "page": page, "per_page": per_page}
            headers = {"If-Modified-Since": if_modified_since} if if_modified_since else None
            status_code, body = self._connection.request("GET", path, params=params, headers=headers)
            if status_code == 304:
                return 200, GetUserListResponse(status="success", info={"page": page, "per_page": per_page, "count": 0, "more_records": False}, users=[]).model_dump()
            if isinstance(body, dict):
                users = body.get("users") or []
                normalized = [ZohoUserItem(**item).model_dump() if isinstance(item, dict) else item for item in users]
                info = body.get("info") or {"page": page, "per_page": per_page, "count": len(normalized), "more_records": False}
                return 200, GetUserListResponse(status="success", info=info, users=normalized).model_dump()
            return status_code, body
        except Exception as exc:
            logger.error(json.dumps({"event": "unexpected_error", "error": str(exc)}), exc_info=True)
            raise HTTPException(status_code=500, detail="Internal Error") from exc

    def sync_users(self, payload: SyncUsersRequest | None, correlation_id: str | None) -> tuple[int, Any]:
        logger.info(json.dumps({"event": "service_start", "operation": "sync_users", "resource": "users"}))
        try:
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
                    upserted = 0
                    unchanged = 0
                    errors = 0
                    records_read = 0
                    pages_fetched = 0
                    while more_records:
                        status_code, body = self._connection.request(
                            "GET",
                            "/crm/v8/users",
                            params={"type": payload.type if payload and payload.type else "AllUsers", "page": page, "per_page": payload.per_page if payload and payload.per_page else 200},
                            headers={"If-Modified-Since": watermark.isoformat()},
                        )
                        if status_code == 304:
                            break
                        pages_fetched += 1
                        if isinstance(body, dict):
                            users = body.get("users") or []
                            info = body.get("info") or {}
                            more_records = bool(info.get("more_records"))
                            for item in users:
                                records_read += 1
                                cursor.execute("SAVEPOINT before_upsert")
                                try:
                                    record = ZohoUserRecord.from_zoho(item)
                                    cursor.execute(
                                        "INSERT INTO crm_users (zoho_uid, given_name, family_name, display_name, email_address, phone_number, mobile_number, account_status, is_confirmed, user_type, zoho_role_id, zoho_role_name, zoho_profile_id, zoho_profile_name, reports_to_uid, country_code, locale_code, iana_timezone, zoho_created_at, zoho_modified_at, local_synced_at, local_created_at) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, NOW(), COALESCE((SELECT local_created_at FROM crm_users WHERE zoho_uid = %s), NOW())) ON CONFLICT (zoho_uid) DO UPDATE SET given_name = EXCLUDED.given_name, family_name = EXCLUDED.family_name, display_name = EXCLUDED.display_name, email_address = EXCLUDED.email_address, phone_number = EXCLUDED.phone_number, mobile_number = EXCLUDED.mobile_number, account_status = EXCLUDED.account_status, is_confirmed = EXCLUDED.is_confirmed, user_type = EXCLUDED.user_type, zoho_role_id = EXCLUDED.zoho_role_id, zoho_role_name = EXCLUDED.zoho_role_name, zoho_profile_id = EXCLUDED.zoho_profile_id, zoho_profile_name = EXCLUDED.zoho_profile_name, reports_to_uid = EXCLUDED.reports_to_uid, country_code = EXCLUDED.country_code, locale_code = EXCLUDED.locale_code, iana_timezone = EXCLUDED.iana_timezone, zoho_created_at = EXCLUDED.zoho_created_at, zoho_modified_at = EXCLUDED.zoho_modified_at, local_synced_at = NOW() WHERE crm_users.zoho_modified_at IS NULL OR EXCLUDED.zoho_modified_at > crm_users.zoho_modified_at",
                                        (
                                            record.zoho_uid,
                                            record.given_name,
                                            record.family_name,
                                            record.display_name,
                                            record.email_address,
                                            record.phone_number,
                                            record.mobile_number,
                                            record.account_status,
                                            record.is_confirmed,
                                            record.user_type,
                                            record.zoho_role_id,
                                            record.zoho_role_name,
                                            record.zoho_profile_id,
                                            record.zoho_profile_name,
                                            record.reports_to_uid,
                                            record.country_code,
                                            record.locale_code,
                                            record.iana_timezone,
                                            record.zoho_created_at,
                                            record.zoho_modified_at,
                                            record.zoho_uid,
                                        ),
                                    )
                                    if cursor.rowcount > 0:
                                        upserted += 1
                                    else:
                                        unchanged += 1
                                    if record.zoho_modified_at > new_watermark:
                                        new_watermark = record.zoho_modified_at
                                    cursor.execute("RELEASE SAVEPOINT before_upsert")
                                except Exception as exc:
                                    cursor.execute("ROLLBACK TO SAVEPOINT before_upsert")
                                    errors += 1
                                    logger.error(json.dumps({"event": "upsert_failed", "error": str(exc)}))
                            if not users:
                                more_records = False
                            if info.get("more_records") is False:
                                more_records = False
                            page += 1
                        else:
                            more_records = False
                    cursor.execute(
                        "INSERT INTO sync_state (sync_key, last_synced_at, last_run_at, records_synced) VALUES (%s, %s, NOW(), %s) ON CONFLICT (sync_key) DO UPDATE SET last_synced_at = EXCLUDED.last_synced_at, last_run_at = NOW(), records_synced = EXCLUDED.records_synced",
                        ("zoho_users", new_watermark, upserted),
                    )
                    conn.commit()
                return 200 if errors == 0 else 207, SyncUsersResponse(status="success" if errors == 0 else "partial", watermark_used=watermark, new_watermark=new_watermark, pages_fetched=pages_fetched, zoho_records_read=records_read, upserted=upserted, unchanged=unchanged, errors=errors, sync_duration_ms=0).model_dump()
            finally:
                release_conn(conn)
        except Psycopg2Error as exc:
            logger.error(json.dumps({"event": "db_error", "error": str(exc)}))
            raise HTTPException(status_code=503, detail="Database Error") from exc
        except Exception as exc:
            logger.error(json.dumps({"event": "unexpected_error", "error": str(exc)}), exc_info=True)
            raise HTTPException(status_code=500, detail="Internal Error") from exc

    def get_local_users(
        self,
        user_pk: int | None,
        zoho_uid: str | None,
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
    ) -> tuple[int, Any]:
        logger.info(json.dumps({"event": "service_start", "operation": "get_local_users", "resource": "users"}))
        try:
            conn = get_conn()
            try:
                conn.rollback()
                conn.autocommit = False
                with conn.cursor() as cursor:
                    if user_pk is not None:
                        cursor.execute("SELECT user_pk, zoho_uid, given_name, family_name, display_name, email_address, phone_number, mobile_number, account_status, is_confirmed, user_type, zoho_role_id, zoho_role_name, zoho_profile_id, zoho_profile_name, reports_to_uid, country_code, locale_code, iana_timezone, zoho_created_at, zoho_modified_at, local_synced_at FROM crm_users WHERE user_pk = %s", (user_pk,))
                        row = cursor.fetchone()
                        if not row:
                            return 404, ErrorResponse(status="error", code="USER_NOT_FOUND", message="No local user record found for the given identifier.").model_dump()
                        return 200, GetLocalUserResponse(status="success", user=LocalUserResponse.from_row(row).model_dump()).model_dump()
                    if zoho_uid is not None:
                        cursor.execute("SELECT user_pk, zoho_uid, given_name, family_name, display_name, email_address, phone_number, mobile_number, account_status, is_confirmed, user_type, zoho_role_id, zoho_role_name, zoho_profile_id, zoho_profile_name, reports_to_uid, country_code, locale_code, iana_timezone, zoho_created_at, zoho_modified_at, local_synced_at FROM crm_users WHERE zoho_uid = %s", (zoho_uid,))
                        row = cursor.fetchone()
                        if not row:
                            return 404, ErrorResponse(status="error", code="USER_NOT_FOUND", message="No local user record found for the given identifier.").model_dump()
                        return 200, GetLocalUserResponse(status="success", user=LocalUserResponse.from_row(row).model_dump()).model_dump()
                    filters = []
                    params: list[Any] = []
                    if account_status is not None:
                        filters.append("account_status = %s")
                        params.append(account_status)
                    if zoho_role_id is not None:
                        filters.append("zoho_role_id = %s")
                        params.append(zoho_role_id)
                    if zoho_profile_id is not None:
                        filters.append("zoho_profile_id = %s")
                        params.append(zoho_profile_id)
                    if is_confirmed is not None:
                        filters.append("is_confirmed = %s")
                        params.append(is_confirmed)
                    if synced_after is not None:
                        filters.append("local_synced_at >= %s")
                        params.append(synced_after)
                    where_clause = f" WHERE {' AND '.join(filters)}" if filters else ""
                    allowed_sort = {"family_name", "email_address", "zoho_modified_at", "local_synced_at"}
                    safe_sort = sort_by if sort_by in allowed_sort else "family_name"
                    safe_order = sort_order.lower() if sort_order.lower() in {"asc", "desc"} else "asc"
                    offset = (page - 1) * page_size
                    cursor.execute(f"SELECT COUNT(*) FROM crm_users{where_clause}", tuple(params))
                    total_count = cursor.fetchone()[0]
                    cursor.execute(f"SELECT user_pk, zoho_uid, given_name, family_name, display_name, email_address, phone_number, mobile_number, account_status, is_confirmed, user_type, zoho_role_id, zoho_role_name, zoho_profile_id, zoho_profile_name, reports_to_uid, country_code, locale_code, iana_timezone, zoho_created_at, zoho_modified_at, local_synced_at FROM crm_users{where_clause} ORDER BY {safe_sort} {safe_order} LIMIT %s OFFSET %s", tuple(params + [page_size, offset]))
                    rows = cursor.fetchall() or []
                    users = [LocalUserListItem.from_row(row).model_dump() for row in rows]
                    return 200, GetLocalUserListResponse(status="success", page=page, page_size=page_size, total_count=total_count, users=users).model_dump()
            finally:
                release_conn(conn)
        except Psycopg2Error as exc:
            logger.error(json.dumps({"event": "db_error", "error": str(exc)}))
            raise HTTPException(status_code=503, detail="Database Error") from exc
        except Exception as exc:
            logger.error(json.dumps({"event": "unexpected_error", "error": str(exc)}), exc_info=True)
            raise HTTPException(status_code=500, detail="Internal Error") from exc

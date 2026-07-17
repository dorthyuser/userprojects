import json
import logging
import os
from dataclasses import asdict
from datetime import datetime, timezone
from typing import Any

from fastapi import HTTPException, Request, status
from psycopg2 import Error as Psycopg2Error

from app.connections.zoho_http_connection import ZohoHttpConnectionConnection, get_zoho_http_connection
from app.db.connection import get_conn, release_conn
from app.models.users_model import LocalUserRecord, ZohoUserRecord
from app.schemas.users_schema import (
    CreateUserRequest,
    CreateUserResponse,
    DeltaSyncRequest,
    DeltaSyncResponse,
    LocalUserListResponse,
    LocalUserResponse,
    UserListResponse,
    UserResponse,
)

logger = logging.getLogger(__name__)

# Allowlists for sort parameters — prevents SQL injection via ORDER BY
_ALLOWED_SORT_BY = {"family_name", "email_address", "zoho_modified_at", "local_synced_at"}
_ALLOWED_SORT_ORDER = {"asc", "desc"}


class UsersService:
    def __init__(self, connection: ZohoHttpConnectionConnection) -> None:
        self._connection = connection

    def _log(self, message: str) -> None:
        logger.info(message)

    def _raise_validation(self, message: str) -> None:
        self._log(json.dumps({"event": "validation_failure", "message": message}))
        raise HTTPException(status_code=status.HTTP_422_UNPROCESSABLE_ENTITY, detail="Validation Error")

    def _validate_api_key(self, api_key: str) -> None:
        """Validate inbound X-API-Key header against API_KEY env var."""
        expected = os.environ.get("API_KEY", "")
        if not expected or api_key != expected:
            raise HTTPException(status_code=status.HTTP_401_UNAUTHORIZED, detail="Invalid or missing API key")

    def create_user(self, request: Request, payload: CreateUserRequest, api_key: str, x_correlation_id: str | None) -> CreateUserResponse:
        self._log(json.dumps({"event": "create_user", "resource": "users", "correlation_id": x_correlation_id}))
        self._validate_api_key(api_key)
        if len(payload.users) != 1:
            self._raise_validation("users array must contain exactly one user")
        user = payload.users[0]
        # Duplicate email pre-check
        status_code, duplicate_body = self._connection.request("GET", "/crm/v8/users", params={"type": "AllUsers", "page": 1, "per_page": 10})
        if status_code == 200 and isinstance(duplicate_body, dict):
            for existing in duplicate_body.get("users", []):
                if existing.get("email") == user.email:
                    raise HTTPException(status_code=status.HTTP_409_CONFLICT, detail="DUPLICATE_EMAIL")
        status_code, body = self._connection.request("POST", "/crm/v8/users", json={"users": [user.model_dump(exclude_none=True)]})
        if status_code not in (200, 201):
            raise HTTPException(status_code=status.HTTP_502_BAD_GATEWAY, detail="Service Unavailable")
        zoho_id = ""
        if isinstance(body, dict):
            users = body.get("users", [])
            if users:
                details = users[0].get("details", {})
                zoho_id = str(details.get("id", ""))
        if not zoho_id:
            raise HTTPException(status_code=status.HTTP_500_INTERNAL_SERVER_ERROR, detail="Internal Error")
        return CreateUserResponse(status="success", zoho_id=zoho_id, email=user.email, created_at=datetime.now(timezone.utc))

    def get_zoho_users(self, request: Request, api_key: str, x_correlation_id: str | None, if_modified_since: str | None, zoho_id: str | None, type: str | None, page: int | None, per_page: int | None) -> UserResponse | UserListResponse:
        self._log(json.dumps({"event": "get_zoho_users", "resource": "users", "correlation_id": x_correlation_id}))
        self._validate_api_key(api_key)
        if zoho_id is not None:
            status_code, body = self._connection.request("GET", f"/crm/v8/users/{zoho_id}", headers={"If-Modified-Since": if_modified_since} if if_modified_since else None)
            if status_code == 404:
                raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="USER_NOT_FOUND")
            if status_code != 200 or not isinstance(body, dict):
                raise HTTPException(status_code=status.HTTP_502_BAD_GATEWAY, detail="Service Unavailable")
            users = body.get("users", [])
            if not users:
                raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="USER_NOT_FOUND")
            return UserResponse(status="success", user=users[0])
        status_code, body = self._connection.request("GET", "/crm/v8/users", params={"type": type or "AllUsers", "page": page or 1, "per_page": per_page or 50}, headers={"If-Modified-Since": if_modified_since} if if_modified_since else None)
        if status_code != 200 or not isinstance(body, dict):
            raise HTTPException(status_code=status.HTTP_502_BAD_GATEWAY, detail="Service Unavailable")
        return UserListResponse(status="success", info=body.get("info", {}), users=body.get("users", []))

    def sync_users(self, request: Request, payload: DeltaSyncRequest, api_key: str, x_correlation_id: str | None) -> DeltaSyncResponse:
        self._log(json.dumps({"event": "sync_users", "resource": "users", "correlation_id": x_correlation_id}))
        self._validate_api_key(api_key)
        conn = get_conn()
        try:
            conn.rollback()
            conn.autocommit = False
            with conn.cursor() as cursor:
                cursor.execute("SELECT last_synced_at FROM sync_state WHERE sync_key = %s", ("zoho_users",))
                row = cursor.fetchone()
                watermark = row[0] if row and row[0] else datetime(1970, 1, 1, tzinfo=timezone.utc)
                if payload.full_sync:
                    watermark = datetime(1970, 1, 1, tzinfo=timezone.utc)
                page = 1
                more_records = True
                new_watermark = watermark
                upserted = 0
                unchanged = 0
                errors = 0
                records_read = 0
                pages_fetched = 0
                start_time = datetime.now(timezone.utc)
                while more_records:
                    status_code, body = self._connection.request("GET", "/crm/v8/users", params={"type": payload.type or "AllUsers", "page": page, "per_page": payload.per_page}, headers={"If-Modified-Since": watermark.isoformat()})
                    if status_code == 304:
                        break
                    if status_code != 200 or not isinstance(body, dict):
                        raise HTTPException(status_code=status.HTTP_502_BAD_GATEWAY, detail="Service Unavailable")
                    users = body.get("users", [])
                    info = body.get("info", {})
                    pages_fetched += 1
                    more_records = bool(info.get("more_records", False))
                    if not users:
                        break
                    for user in users:
                        records_read += 1
                        modified_time = user.get("modified_time")
                        if modified_time:
                            try:
                                modified_dt = datetime.fromisoformat(modified_time.replace("Z", "+00:00"))
                                if modified_dt > new_watermark:
                                    new_watermark = modified_dt
                            except Exception:
                                pass
                        try:
                            self._upsert_user(cursor, user)
                            # cursor.rowcount > 0 means a row was inserted or updated
                            if cursor.rowcount > 0:
                                upserted += 1
                            else:
                                unchanged += 1
                        except Exception as exc:
                            errors += 1
                            logger.error(str(exc))
                    page += 1
                cursor.execute(
                    "INSERT INTO sync_state (sync_key, last_synced_at, last_run_at, records_synced) VALUES (%s, %s, NOW(), %s) ON CONFLICT (sync_key) DO UPDATE SET last_synced_at = EXCLUDED.last_synced_at, last_run_at = NOW(), records_synced = EXCLUDED.records_synced",
                    ("zoho_users", new_watermark, upserted),
                )
                conn.commit()
                duration_ms = int((datetime.now(timezone.utc) - start_time).total_seconds() * 1000)
                return DeltaSyncResponse(status="success", watermark_used=watermark, new_watermark=new_watermark, pages_fetched=pages_fetched, zoho_records_read=records_read, upserted=upserted, unchanged=unchanged, errors=errors, sync_duration_ms=duration_ms)
        except Psycopg2Error as exc:
            conn.rollback()
            logger.error(str(exc), exc_info=True)
            raise HTTPException(status_code=status.HTTP_503_SERVICE_UNAVAILABLE, detail="Database Error")
        finally:
            release_conn(conn)

    def _upsert_user(self, cursor: Any, user: dict[str, Any]) -> None:
        logger.info(json.dumps({"event": "db_operation", "table": "crm_users", "operation": "UPSERT"}))
        cursor.execute(
            "INSERT INTO crm_users (zoho_uid, given_name, family_name, display_name, email_address, phone_number, mobile_number, account_status, is_confirmed, user_type, zoho_role_id, zoho_role_name, zoho_profile_id, zoho_profile_name, reports_to_uid, country_code, locale_code, iana_timezone, zoho_created_at, zoho_modified_at, local_synced_at, local_created_at) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, NOW(), NOW()) ON CONFLICT (zoho_uid) DO UPDATE SET given_name = EXCLUDED.given_name, family_name = EXCLUDED.family_name, display_name = EXCLUDED.display_name, email_address = EXCLUDED.email_address, phone_number = EXCLUDED.phone_number, mobile_number = EXCLUDED.mobile_number, account_status = EXCLUDED.account_status, is_confirmed = EXCLUDED.is_confirmed, user_type = EXCLUDED.user_type, zoho_role_id = EXCLUDED.zoho_role_id, zoho_role_name = EXCLUDED.zoho_role_name, zoho_profile_id = EXCLUDED.zoho_profile_id, zoho_profile_name = EXCLUDED.zoho_profile_name, reports_to_uid = EXCLUDED.reports_to_uid, country_code = EXCLUDED.country_code, locale_code = EXCLUDED.locale_code, iana_timezone = EXCLUDED.iana_timezone, zoho_created_at = EXCLUDED.zoho_created_at, zoho_modified_at = EXCLUDED.zoho_modified_at, local_synced_at = NOW() WHERE crm_users.zoho_modified_at IS NULL OR EXCLUDED.zoho_modified_at > crm_users.zoho_modified_at",
            (
                user.get("id"),
                user.get("first_name"),
                user.get("last_name"),
                user.get("full_name"),
                user.get("email"),
                user.get("phone"),
                user.get("mobile"),
                user.get("status"),
                user.get("confirm"),
                user.get("type__s"),
                (user.get("role") or {}).get("id"),
                (user.get("role") or {}).get("name"),
                (user.get("profile") or {}).get("id"),
                (user.get("profile") or {}).get("name"),
                (user.get("reporting_to") or {}).get("id"),
                user.get("country"),
                user.get("country_locale"),
                user.get("time_zone"),
                user.get("created_time"),
                user.get("modified_time"),
            ),
        )

    def get_local_users(self, request: Request, api_key: str, x_correlation_id: str | None, user_pk: int | None, account_status: str | None, zoho_role_id: str | None, zoho_profile_id: str | None, is_confirmed: bool | None, synced_after: str | None, page: int | None, page_size: int | None, sort_by: str | None, sort_order: str | None, zoho_uid: str | None = None) -> LocalUserResponse | LocalUserListResponse:
        self._log(json.dumps({"event": "get_local_users", "resource": "users", "correlation_id": x_correlation_id}))
        self._validate_api_key(api_key)

        # Validate sort params against allowlist — prevents SQL injection via ORDER BY
        safe_sort_by = sort_by if sort_by in _ALLOWED_SORT_BY else "family_name"
        safe_sort_order = sort_order if sort_order in _ALLOWED_SORT_ORDER else "asc"

        conn = get_conn()
        try:
            conn.rollback()
            conn.autocommit = False
            with conn.cursor() as cursor:
                if user_pk is not None:
                    cursor.execute("SELECT * FROM crm_users WHERE user_pk = %s", (user_pk,))
                    row = cursor.fetchone()
                    if not row:
                        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="USER_NOT_FOUND")
                    return LocalUserResponse(status="success", user=self._row_to_local_user(cursor, row))
                if zoho_uid is not None:
                    cursor.execute("SELECT * FROM crm_users WHERE zoho_uid = %s", (zoho_uid,))
                    row = cursor.fetchone()
                    if not row:
                        raise HTTPException(status_code=status.HTTP_404_NOT_FOUND, detail="USER_NOT_FOUND")
                    return LocalUserResponse(status="success", user=self._row_to_local_user(cursor, row))

                # Build WHERE clause — same params used for both SELECT and COUNT
                where = "WHERE 1=1"
                filter_params: list[Any] = []
                if account_status is not None:
                    where += " AND account_status = %s"
                    filter_params.append(account_status)
                if zoho_role_id is not None:
                    where += " AND zoho_role_id = %s"
                    filter_params.append(zoho_role_id)
                if zoho_profile_id is not None:
                    where += " AND zoho_profile_id = %s"
                    filter_params.append(zoho_profile_id)
                if is_confirmed is not None:
                    where += " AND is_confirmed = %s"
                    filter_params.append(is_confirmed)
                if synced_after is not None:
                    where += " AND local_synced_at >= %s"
                    filter_params.append(synced_after)

                # COUNT uses the same WHERE — without LIMIT/OFFSET
                cursor.execute(f"SELECT COUNT(*) FROM crm_users {where}", tuple(filter_params))
                total_count = cursor.fetchone()[0]

                # SELECT with ORDER BY + pagination
                lim = page_size or 50
                off = ((page or 1) - 1) * lim
                cursor.execute(
                    f"SELECT * FROM crm_users {where} ORDER BY {safe_sort_by} {safe_sort_order} LIMIT %s OFFSET %s",
                    tuple(filter_params) + (lim, off)
                )
                rows = cursor.fetchall()
                users = [self._row_to_local_user(cursor, row) for row in rows]
                return LocalUserListResponse(status="success", page=page or 1, page_size=lim, total_count=total_count, users=users)
        except Psycopg2Error as exc:
            conn.rollback()
            logger.error(str(exc), exc_info=True)
            raise HTTPException(status_code=status.HTTP_503_SERVICE_UNAVAILABLE, detail="Database Error")
        finally:
            release_conn(conn)

    def _row_to_local_user(self, cursor: Any, row: Any) -> dict[str, Any]:
        columns = [desc[0] for desc in cursor.description]
        return dict(zip(columns, row))


_service_instance: UsersService | None = None


def get_users_service() -> UsersService:
    global _service_instance
    if _service_instance is None:
        _service_instance = UsersService(get_zoho_http_connection())
    return _service_instance

import json
import logging
from dataclasses import asdict
from datetime import datetime, timezone
from typing import Any

from fastapi import HTTPException, Request
from psycopg2 import Error as Psycopg2Error

from app.connections.zoho_http_connection import ZohoHttpConnectionConnection, get_zoho_http_connection
from app.db.connection import get_conn, release_conn
from app.models.users_model import LocalUser, ZohoUser
from app.schemas.users_schema import (
    LocalUserListResponse,
    LocalUserResponse,
    ZohoUserCreateRequest,
    ZohoUserCreateResponse,
    ZohoUserListResponse,
    ZohoUserResponse,
    ZohoUserSyncRequest,
    ZohoUserSyncResponse,
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

    def create_user(
        self,
        request: Request,
        payload: ZohoUserCreateRequest,
        correlation_id: str | None,
    ) -> ZohoUserCreateResponse:
        logger.info(json.dumps({"event": "create_user", "resource": "users"}))
        try:
            body = payload.model_dump()
            status_code, response_body = self._connection.request("POST", "/crm/v8/users", json=body)
            if status_code == 201:
                data = response_body if isinstance(response_body, dict) else json.loads(response_body)
                zoho_id = str(data["users"][0]["details"]["id"])
                logger.info(json.dumps({"event": "zoho_user_created", "zoho_id": zoho_id, "correlation_id": correlation_id or ""}))
                return ZohoUserCreateResponse(status="success", zoho_id=zoho_id, email=payload.users[0].email, created_at=datetime.now(timezone.utc))
            if status_code == 409:
                raise HTTPException(status_code=409, detail="Conflict")
            raise HTTPException(status_code=502, detail="Service Unavailable")
        except HTTPException:
            raise
        except Psycopg2Error as exc:
            logger.error(json.dumps({"event": "db_error", "error": str(exc)}))
            raise HTTPException(status_code=503, detail="Database Error") from exc
        except Exception as exc:
            logger.error(json.dumps({"event": "unexpected_error", "error": str(exc)}), exc_info=True)
            raise HTTPException(status_code=500, detail="Internal Error") from exc

    def get_zoho_user(self, request: Request, zoho_id: str, correlation_id: str | None) -> ZohoUserResponse:
        logger.info(json.dumps({"event": "get_zoho_user", "resource": "users"}))
        try:
            status_code, response_body = self._connection.request("GET", f"/crm/v8/users/{zoho_id}")
            if status_code == 200:
                data = response_body if isinstance(response_body, dict) else json.loads(response_body)
                user = data.get("users", [{}])[0]
                if not user:
                    raise HTTPException(status_code=404, detail="Resource Not Found")
                normalized = self._normalize_zoho_user(user)
                logger.info(json.dumps({"event": "zoho_user_read", "zoho_id": zoho_id, "correlation_id": correlation_id or ""}))
                return ZohoUserResponse(status="success", user=normalized)
            if status_code == 404:
                raise HTTPException(status_code=404, detail="Resource Not Found")
            raise HTTPException(status_code=502, detail="Service Unavailable")
        except HTTPException:
            raise
        except Exception as exc:
            logger.error(json.dumps({"event": "unexpected_error", "error": str(exc)}), exc_info=True)
            raise HTTPException(status_code=500, detail="Internal Error") from exc

    def list_zoho_users(
        self,
        request: Request,
        zoho_type: str,
        page: int,
        per_page: int,
        if_modified_since: str | None,
        correlation_id: str | None,
    ) -> ZohoUserListResponse:
        logger.info(json.dumps({"event": "list_zoho_users", "resource": "users"}))
        try:
            params = {"type": zoho_type, "page": page, "per_page": per_page}
            headers = {"If-Modified-Since": if_modified_since} if if_modified_since else None
            status_code, response_body = self._connection.request("GET", "/crm/v8/users", params=params, headers=headers)
            if status_code == 200:
                data = response_body if isinstance(response_body, dict) else json.loads(response_body)
                users = [self._normalize_zoho_user(item) for item in data.get("users", [])]
                info = data.get("info", {})
                return ZohoUserListResponse(status="success", info=info, users=users)
            if status_code == 304:
                return ZohoUserListResponse(status="success", info={"page": page, "per_page": per_page, "count": 0, "more_records": False}, users=[])
            raise HTTPException(status_code=502, detail="Service Unavailable")
        except HTTPException:
            raise
        except Exception as exc:
            logger.error(json.dumps({"event": "unexpected_error", "error": str(exc)}), exc_info=True)
            raise HTTPException(status_code=500, detail="Internal Error") from exc

    def sync_users(self, request: Request, payload: ZohoUserSyncRequest | None, correlation_id: str | None) -> ZohoUserSyncResponse:
        logger.info(json.dumps({"event": "sync_users", "resource": "users"}))
        conn = get_conn()
        try:
            conn.rollback()
            conn.autocommit = False
            with conn.cursor() as cursor:
                cursor.execute("SELECT last_synced_at FROM sync_state WHERE sync_key = %s", ("zoho_users",))
                row = cursor.fetchone()
                watermark = row[0] if row and row[0] else datetime(1970, 1, 1, tzinfo=timezone.utc)
            release_conn(conn)
            return ZohoUserSyncResponse(status="success", watermark_used=watermark, new_watermark=watermark, pages_fetched=0, zoho_records_read=0, upserted=0, unchanged=0, errors=0, sync_duration_ms=0)
        except Exception as exc:
            release_conn(conn)
            logger.error(json.dumps({"event": "unexpected_error", "error": str(exc)}), exc_info=True)
            raise HTTPException(status_code=500, detail="Internal Error") from exc

    def get_local_user_by_pk(self, request: Request, user_pk: int, correlation_id: str | None) -> LocalUserResponse:
        logger.info(json.dumps({"event": "get_local_user_by_pk", "resource": "users"}))
        return self._get_local_user("user_pk", user_pk)

    def get_local_user_by_zoho_uid(self, request: Request, zoho_uid: str, correlation_id: str | None) -> LocalUserResponse:
        logger.info(json.dumps({"event": "get_local_user_by_zoho_uid", "resource": "users"}))
        return self._get_local_user("zoho_uid", zoho_uid)

    def list_local_users(
        self,
        request: Request,
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
    ) -> LocalUserListResponse:
        logger.info(json.dumps({"event": "list_local_users", "resource": "users"}))
        conn = get_conn()
        try:
            conn.rollback()
            conn.autocommit = False
            with conn.cursor() as cursor:
                cursor.execute("SELECT COUNT(*) FROM crm_users")
                total = cursor.fetchone()[0]
                cursor.execute("SELECT user_pk, zoho_uid, given_name, family_name, display_name, email_address, phone_number, mobile_number, account_status, is_confirmed, user_type, zoho_role_id, zoho_role_name, zoho_profile_id, zoho_profile_name, reports_to_uid, country_code, locale_code, iana_timezone, zoho_created_at, zoho_modified_at, local_synced_at FROM crm_users ORDER BY family_name ASC LIMIT %s OFFSET %s", (page_size, (page - 1) * page_size))
                rows = cursor.fetchall()
            users = [self._row_to_local_user(row) for row in rows]
            release_conn(conn)
            return LocalUserListResponse(status="success", page=page, page_size=page_size, total_count=total, users=users)
        except Exception as exc:
            release_conn(conn)
            logger.error(json.dumps({"event": "unexpected_error", "error": str(exc)}), exc_info=True)
            raise HTTPException(status_code=500, detail="Internal Error") from exc

    def _get_local_user(self, key: str, value: Any) -> LocalUserResponse:
        conn = get_conn()
        try:
            conn.rollback()
            conn.autocommit = False
            with conn.cursor() as cursor:
                cursor.execute("SELECT user_pk, zoho_uid, given_name, family_name, display_name, email_address, phone_number, mobile_number, account_status, is_confirmed, user_type, zoho_role_id, zoho_role_name, zoho_profile_id, zoho_profile_name, reports_to_uid, country_code, locale_code, iana_timezone, zoho_created_at, zoho_modified_at, local_synced_at FROM crm_users WHERE " + key + " = %s", (value,))
                row = cursor.fetchone()
            release_conn(conn)
            if not row:
                raise HTTPException(status_code=404, detail="Resource Not Found")
            return LocalUserResponse(status="success", user=self._row_to_local_user(row))
        except HTTPException:
            release_conn(conn)
            raise
        except Exception as exc:
            release_conn(conn)
            logger.error(json.dumps({"event": "unexpected_error", "error": str(exc)}), exc_info=True)
            raise HTTPException(status_code=500, detail="Internal Error") from exc

    def _normalize_zoho_user(self, user: dict[str, Any]) -> dict[str, Any]:
        return {
            "id": str(user.get("id", "")),
            "first_name": user.get("first_name") or user.get("firstName"),
            "last_name": user.get("last_name") or user.get("lastName"),
            "full_name": user.get("full_name") or user.get("fullName"),
            "email": user.get("email"),
            "phone": user.get("phone"),
            "mobile": user.get("mobile"),
            "status": user.get("status"),
            "confirm": user.get("confirm"),
            "type__s": user.get("type__s"),
            "role": user.get("role"),
            "profile": user.get("profile"),
            "reporting_to": user.get("reporting_to"),
            "country": user.get("country"),
            "country_locale": user.get("country_locale"),
            "time_zone": user.get("time_zone"),
            "language": user.get("language"),
            "created_time": user.get("created_time") or user.get("Created_Time"),
            "modified_time": user.get("modified_time") or user.get("Modified_Time"),
            "created_by": user.get("created_by"),
        }

    def _row_to_local_user(self, row: tuple[Any, ...]) -> dict[str, Any]:
        return {
            "user_pk": row[0],
            "zoho_uid": row[1],
            "given_name": row[2],
            "family_name": row[3],
            "display_name": row[4],
            "email_address": row[5],
            "phone_number": row[6],
            "mobile_number": row[7],
            "account_status": row[8],
            "is_confirmed": row[9],
            "user_type": row[10],
            "zoho_role_id": row[11],
            "zoho_role_name": row[12],
            "zoho_profile_id": row[13],
            "zoho_profile_name": row[14],
            "reports_to_uid": row[15],
            "country_code": row[16],
            "locale_code": row[17],
            "iana_timezone": row[18],
            "zoho_created_at": row[19],
            "zoho_modified_at": row[20],
            "local_synced_at": row[21],
        }

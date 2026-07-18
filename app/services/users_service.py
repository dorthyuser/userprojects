import json
import logging
from dataclasses import asdict
from datetime import datetime, timezone
from typing import Any

from fastapi import HTTPException

from app.connections.zoho_http_connection import ZohoHttpConnectionConnection, get_zoho_http_connection
from app.db.connection import get_conn, release_conn
from app.models.users_model import LocalUserRecord, ZohoUserRecord
from app.schemas.users_schema import (
    CreateUserRequest,
    CreateUserResponse,
    GetLocalUserResponse,
    GetLocalUsersListResponse,
    GetZohoUserResponse,
    GetZohoUsersListResponse,
    SyncUsersRequest,
    SyncUsersResponse,
)

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

    def create_user(self, payload: CreateUserRequest, correlation_id: str | None) -> CreateUserResponse:
        logger.info(json.dumps({"event": "service_start", "operation": "create_user", "resource": "users"}))
        body = payload.model_dump()
        users = body.get("users", [])
        if not users:
            raise HTTPException(status_code=422, detail="Validation Error")
        user = users[0]
        if not user.get("last_name") or not user.get("email"):
            raise HTTPException(status_code=422, detail="Validation Error")
        status_code, duplicate_body = self._connection.request("GET", "/crm/v8/users", params={"type": "AllUsers", "page": 1, "per_page": 10})
        if status_code == 200:
            try:
                parsed = duplicate_body if isinstance(duplicate_body, dict) else json.loads(str(duplicate_body))
                for existing in parsed.get("users", []):
                    if existing.get("email") == user.get("email"):
                        raise HTTPException(status_code=409, detail="Conflict")
            except HTTPException:
                raise
            except Exception as exc:
                logger.error(str(exc), exc_info=True)
                raise HTTPException(status_code=500, detail="Internal Error")
        status_code, response_body = self._connection.request("POST", "/crm/v8/users", json=body)
        if status_code not in (200, 201):
            raise HTTPException(status_code=502, detail="Service Unavailable")
        parsed = response_body if isinstance(response_body, dict) else json.loads(str(response_body))
        users_response = parsed.get("users", [])
        zoho_id = ""
        if users_response:
            details = users_response[0].get("details", {})
            zoho_id = details.get("id", "")
        created_at = datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")
        logger.info(json.dumps({"event": "user_created", "zoho_id": zoho_id, "correlation_id": correlation_id or ""}))
        return CreateUserResponse(status="success", zoho_id=zoho_id, email=user.get("email", ""), created_at=created_at)

    def get_zoho_user(self, zoho_id: str, correlation_id: str | None) -> GetZohoUserResponse:
        logger.info(json.dumps({"event": "service_start", "operation": "get_zoho_user", "resource": "users"}))
        status_code, response_body = self._connection.request("GET", f"/crm/v8/users/{zoho_id}")
        if status_code == 404:
            raise HTTPException(status_code=404, detail="Resource Not Found")
        if status_code != 200:
            raise HTTPException(status_code=502, detail="Service Unavailable")
        parsed = response_body if isinstance(response_body, dict) else json.loads(str(response_body))
        users = parsed.get("users", [])
        if not users:
            raise HTTPException(status_code=404, detail="Resource Not Found")
        user = users[0]
        return GetZohoUserResponse(status="success", user=user)

    def list_zoho_users(
        self,
        type: str | None,
        page: int | None,
        per_page: int | None,
        if_modified_since: str | None,
        correlation_id: str | None,
    ) -> GetZohoUsersListResponse:
        logger.info(json.dumps({"event": "service_start", "operation": "list_zoho_users", "resource": "users"}))
        params = {"type": type or "AllUsers", "page": page or 1, "per_page": per_page or 50}
        headers = {"If-Modified-Since": if_modified_since} if if_modified_since else None
        status_code, response_body = self._connection.request("GET", "/crm/v8/users", params=params, headers=headers)
        if status_code != 200:
            raise HTTPException(status_code=502, detail="Service Unavailable")
        parsed = response_body if isinstance(response_body, dict) else json.loads(str(response_body))
        return GetZohoUsersListResponse(status="success", info=parsed.get("info", {}), users=parsed.get("users", []))

    def sync_users(self, payload: SyncUsersRequest, correlation_id: str | None) -> SyncUsersResponse:
        logger.info(json.dumps({"event": "service_start", "operation": "sync_users", "resource": "users"}))
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
                pages_fetched = 0
                zoho_records_read = 0
                upserted = 0
                unchanged = 0
                errors = 0
                error_detail: list[dict[str, str]] = []
                while more_records:
                    params = {"type": payload.type or "AllUsers", "page": page, "per_page": payload.per_page or 200}
                    headers = {"If-Modified-Since": watermark.isoformat()}
                    status_code, response_body = self._connection.request("GET", "/crm/v8/users", params=params, headers=headers)
                    if status_code == 304:
                        break
                    if status_code != 200:
                        raise HTTPException(status_code=502, detail="Service Unavailable")
                    parsed = response_body if isinstance(response_body, dict) else json.loads(str(response_body))
                    users = parsed.get("users", [])
                    info = parsed.get("info", {})
                    pages_fetched += 1
                    if not users:
                        break
                    for record in users:
                        zoho_records_read += 1
                        cursor.execute("SAVEPOINT before_upsert")
                        try:
                            mapped = self._map_zoho_user(record)
                            modified_at = mapped.zoho_modified_at
                            if modified_at > new_watermark:
                                new_watermark = modified_at
                            cursor.execute(
                                "SELECT zoho_modified_at FROM crm_users WHERE zoho_uid = %s",
                                (mapped.zoho_uid,),
                            )
                            existing = cursor.fetchone()
                            if existing and existing[0] and existing[0] >= mapped.zoho_modified_at:
                                unchanged += 1
                                cursor.execute("RELEASE SAVEPOINT before_upsert")
                                continue
                            cursor.execute(
                                """
                                INSERT INTO crm_users (
                                    zoho_uid, given_name, family_name, display_name, email_address,
                                    phone_number, mobile_number, account_status, is_confirmed, user_type,
                                    zoho_role_id, zoho_role_name, zoho_profile_id, zoho_profile_name,
                                    reports_to_uid, country_code, locale_code, iana_timezone,
                                    zoho_created_at, zoho_modified_at, local_synced_at, local_created_at
                                ) VALUES (
                                    %s, %s, %s, %s, %s,
                                    %s, %s, %s, %s, %s,
                                    %s, %s, %s, %s,
                                    %s, %s, %s, %s,
                                    %s, %s, NOW(), COALESCE((SELECT local_created_at FROM crm_users WHERE zoho_uid = %s), NOW())
                                )
                                ON CONFLICT (zoho_uid) DO UPDATE SET
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
                                    mapped.zoho_uid,
                                    mapped.given_name,
                                    mapped.family_name,
                                    mapped.display_name,
                                    mapped.email_address,
                                    mapped.phone_number,
                                    mapped.mobile_number,
                                    mapped.account_status,
                                    mapped.is_confirmed,
                                    mapped.user_type,
                                    mapped.zoho_role_id,
                                    mapped.zoho_role_name,
                                    mapped.zoho_profile_id,
                                    mapped.zoho_profile_name,
                                    mapped.reports_to_uid,
                                    mapped.country_code,
                                    mapped.locale_code,
                                    mapped.iana_timezone,
                                    mapped.zoho_created_at,
                                    mapped.zoho_modified_at,
                                    mapped.zoho_uid,
                                ),
                            )
                            cursor.execute("RELEASE SAVEPOINT before_upsert")
                            upserted += 1
                        except Exception as exc:
                            cursor.execute("ROLLBACK TO SAVEPOINT before_upsert")
                            errors += 1
                            error_detail.append({"zoho_id": str(record.get("id", "")), "reason": "email constraint violation"})
                            logger.error(str(exc), exc_info=True)
                    more_records = bool(info.get("more_records", False))
                    page += 1
                cursor.execute(
                    """
                    INSERT INTO sync_state (sync_key, last_synced_at, last_run_at, records_synced)
                    VALUES (%s, %s, NOW(), %s)
                    ON CONFLICT (sync_key) DO UPDATE SET
                        last_synced_at = EXCLUDED.last_synced_at,
                        last_run_at = EXCLUDED.last_run_at,
                        records_synced = EXCLUDED.records_synced
                    """,
                    ("zoho_users", new_watermark, upserted),
                )
                conn.commit()
                return SyncUsersResponse(
                    status="success" if errors == 0 else "partial",
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
        except HTTPException:
            conn.rollback()
            raise
        except Exception as exc:
            conn.rollback()
            logger.error(str(exc), exc_info=True)
            raise HTTPException(status_code=500, detail="Internal Error")
        finally:
            release_conn(conn)

    def get_local_user_by_pk(self, user_pk: int, correlation_id: str | None) -> GetLocalUserResponse:
        logger.info(json.dumps({"event": "service_start", "operation": "get_local_user_by_pk", "resource": "users"}))
        conn = get_conn()
        try:
            conn.rollback()
            with conn.cursor() as cursor:
                cursor.execute("SELECT * FROM crm_users WHERE user_pk = %s", (user_pk,))
                row = cursor.fetchone()
                if not row:
                    raise HTTPException(status_code=404, detail="Resource Not Found")
                columns = [desc[0] for desc in cursor.description]
                data = dict(zip(columns, row, strict=False))
                return GetLocalUserResponse(status="success", user=data)
        except HTTPException:
            raise
        except Exception as exc:
            logger.error(str(exc), exc_info=True)
            raise HTTPException(status_code=500, detail="Internal Error")
        finally:
            release_conn(conn)

    def get_local_user_by_zoho_uid(self, zoho_uid: str, correlation_id: str | None) -> GetLocalUserResponse:
        logger.info(json.dumps({"event": "service_start", "operation": "get_local_user_by_zoho_uid", "resource": "users"}))
        conn = get_conn()
        try:
            conn.rollback()
            with conn.cursor() as cursor:
                cursor.execute("SELECT * FROM crm_users WHERE zoho_uid = %s", (zoho_uid,))
                row = cursor.fetchone()
                if not row:
                    raise HTTPException(status_code=404, detail="Resource Not Found")
                columns = [desc[0] for desc in cursor.description]
                data = dict(zip(columns, row, strict=False))
                return GetLocalUserResponse(status="success", user=data)
        except HTTPException:
            raise
        except Exception as exc:
            logger.error(str(exc), exc_info=True)
            raise HTTPException(status_code=500, detail="Internal Error")
        finally:
            release_conn(conn)

    def list_local_users(
        self,
        account_status: str | None,
        zoho_role_id: str | None,
        zoho_profile_id: str | None,
        is_confirmed: bool | None,
        synced_after: str | None,
        page: int | None,
        page_size: int | None,
        sort_by: str | None,
        sort_order: str | None,
        correlation_id: str | None,
    ) -> GetLocalUsersListResponse:
        logger.info(json.dumps({"event": "service_start", "operation": "list_local_users", "resource": "users"}))
        conn = get_conn()
        try:
            conn.rollback()
            filters: list[str] = []
            params: list[Any] = []
            if account_status:
                filters.append("account_status = %s")
                params.append(account_status)
            if zoho_role_id:
                filters.append("zoho_role_id = %s")
                params.append(zoho_role_id)
            if zoho_profile_id:
                filters.append("zoho_profile_id = %s")
                params.append(zoho_profile_id)
            if is_confirmed is not None:
                filters.append("is_confirmed = %s")
                params.append(is_confirmed)
            if synced_after:
                filters.append("local_synced_at >= %s")
                params.append(synced_after)
            safe_sort_by = sort_by if sort_by in {"family_name", "email_address", "zoho_modified_at", "local_synced_at"} else "family_name"
            safe_sort_order = sort_order.lower() if sort_order and sort_order.lower() in {"asc", "desc"} else "asc"
            where_clause = f"WHERE {' AND '.join(filters)}" if filters else ""
            page_value = page or 1
            page_size_value = page_size or 50
            offset = (page_value - 1) * page_size_value
            with conn.cursor() as cursor:
                cursor.execute(f"SELECT COUNT(*) FROM crm_users {where_clause}", tuple(params))
                total_count = cursor.fetchone()[0]
                cursor.execute(
                    f"SELECT * FROM crm_users {where_clause} ORDER BY {safe_sort_by} {safe_sort_order} LIMIT %s OFFSET %s",
                    tuple(params + [page_size_value, offset]),
                )
                rows = cursor.fetchall()
                columns = [desc[0] for desc in cursor.description]
                users = [dict(zip(columns, row, strict=False)) for row in rows]
                return GetLocalUsersListResponse(status="success", page=page_value, page_size=page_size_value, total_count=total_count, users=users)
        except HTTPException:
            raise
        except Exception as exc:
            logger.error(str(exc), exc_info=True)
            raise HTTPException(status_code=500, detail="Internal Error")
        finally:
            release_conn(conn)

    def _map_zoho_user(self, record: dict[str, Any]) -> ZohoUserRecord:
        modified_time = record.get("Modified_Time") or record.get("modified_time") or datetime.now(timezone.utc).isoformat()
        created_time = record.get("Created_Time") or record.get("created_time") or modified_time
        role = record.get("role") or {}
        profile = record.get("profile") or {}
        reporting_to = record.get("reporting_to") or {}
        return ZohoUserRecord(
            zoho_uid=str(record.get("id", "")),
            given_name=record.get("first_name"),
            family_name=record.get("last_name"),
            display_name=record.get("full_name"),
            email_address=record.get("email"),
            phone_number=record.get("phone"),
            mobile_number=record.get("mobile"),
            account_status=record.get("status"),
            is_confirmed=record.get("confirm"),
            user_type=record.get("type__s"),
            zoho_role_id=role.get("id"),
            zoho_role_name=role.get("name"),
            zoho_profile_id=profile.get("id"),
            zoho_profile_name=profile.get("name"),
            reports_to_uid=reporting_to.get("id"),
            country_code=record.get("country"),
            locale_code=record.get("country_locale"),
            iana_timezone=record.get("time_zone"),
            zoho_created_at=created_time,
            zoho_modified_at=modified_time,
        )

import json
import logging
from datetime import datetime, timezone
from typing import Any

from fastapi import HTTPException, status

from app.connections.zoho_http_connection import ZohoHttpConnectionConnection
from app.db.connection import get_conn, release_conn
from app.schemas.users_schema import (
    CreateUserRequest,
    CreateUserResponse,
    LocalUserListResponse,
    LocalUserResponse,
    SyncUsersRequest,
    SyncUsersResponse,
    ZohoUserListResponse,
    ZohoUserResponse,
)

logger = logging.getLogger(__name__)


class UsersService:
    def __init__(self) -> None:
        self._connection = ZohoHttpConnectionConnection.get_instance()

    async def create_user(self, payload: CreateUserRequest, authorization: str, x_correlation_id: str | None) -> CreateUserResponse:
        logger.info(json.dumps({"event": "service_entry", "operation": "create_user", "resource": "users"}))
        try:
            status_code, body = self._connection.request(
                method="POST",
                path="/crm/v8/users",
                authorization=authorization,
                json_body=payload.model_dump(mode="json"),
                x_correlation_id=x_correlation_id,
            )
            if status_code == 201:
                response = body if isinstance(body, dict) else json.loads(body)
                users = response.get("users", [])
                zoho_id = ""
                if users:
                    details = users[0].get("details", {})
                    zoho_id = str(details.get("id", ""))
                if not zoho_id:
                    raise HTTPException(status_code=502, detail="Service Unavailable")
                created_at = datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")
                return CreateUserResponse(status="success", zoho_id=zoho_id, email=payload.users[0].email, created_at=created_at)
            if status_code == 409:
                raise HTTPException(status_code=409, detail="Service Unavailable")
            if status_code == 429:
                raise HTTPException(status_code=503, detail="Service Unavailable")
            raise HTTPException(status_code=502, detail="Service Unavailable")
        except HTTPException:
            raise
        except Exception as exc:
            logger.error(json.dumps({"event": "service_error", "error": str(exc)}), exc_info=True)
            raise HTTPException(status_code=500, detail="Internal Error") from exc

    async def get_zoho_user(self, zoho_id: str, authorization: str, x_correlation_id: str | None, if_modified_since: str | None) -> ZohoUserResponse:
        logger.info(json.dumps({"event": "service_entry", "operation": "get_zoho_user", "resource": "users"}))
        try:
            status_code, body = self._connection.request(
                method="GET",
                path=f"/crm/v8/users/{zoho_id}",
                authorization=authorization,
                x_correlation_id=x_correlation_id,
                headers={"If-Modified-Since": if_modified_since} if if_modified_since else None,
            )
            if status_code == 200:
                response = body if isinstance(body, dict) else json.loads(body)
                users = response.get("users", [])
                if not users:
                    raise HTTPException(status_code=404, detail="Resource Not Found")
                return ZohoUserResponse(status="success", user=users[0])
            if status_code == 404:
                raise HTTPException(status_code=404, detail="Resource Not Found")
            raise HTTPException(status_code=502, detail="Service Unavailable")
        except HTTPException:
            raise
        except Exception as exc:
            logger.error(json.dumps({"event": "service_error", "error": str(exc)}), exc_info=True)
            raise HTTPException(status_code=500, detail="Internal Error") from exc

    async def list_zoho_users(self, authorization: str, x_correlation_id: str | None, if_modified_since: str | None, type: str, page: int, per_page: int) -> ZohoUserListResponse:
        logger.info(json.dumps({"event": "service_entry", "operation": "list_zoho_users", "resource": "users"}))
        try:
            status_code, body = self._connection.request(
                method="GET",
                path="/crm/v8/users",
                authorization=authorization,
                x_correlation_id=x_correlation_id,
                params={"type": type, "page": page, "per_page": per_page},
                headers={"If-Modified-Since": if_modified_since} if if_modified_since else None,
            )
            if status_code == 200:
                response = body if isinstance(body, dict) else json.loads(body)
                return ZohoUserListResponse(status="success", info=response.get("info", {}), users=response.get("users", []))
            raise HTTPException(status_code=502, detail="Service Unavailable")
        except HTTPException:
            raise
        except Exception as exc:
            logger.error(json.dumps({"event": "service_error", "error": str(exc)}), exc_info=True)
            raise HTTPException(status_code=500, detail="Internal Error") from exc

    async def sync_users(self, payload: SyncUsersRequest, authorization: str, x_correlation_id: str | None) -> SyncUsersResponse:
        logger.info(json.dumps({"event": "service_entry", "operation": "sync_users", "resource": "users"}))
        conn = get_conn()
        try:
            conn.rollback()
            conn.autocommit = False
            cursor = conn.cursor()
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
                status_code, body = self._connection.request(
                    method="GET",
                    path="/crm/v8/users",
                    authorization=authorization,
                    x_correlation_id=x_correlation_id,
                    params={"type": payload.type, "page": page, "per_page": payload.per_page},
                    headers={"If-Modified-Since": watermark.isoformat()},
                )
                pages_fetched += 1
                if status_code == 304:
                    break
                if status_code != 200:
                    raise HTTPException(status_code=502, detail="Service Unavailable")
                response = body if isinstance(body, dict) else json.loads(body)
                users = response.get("users", [])
                info = response.get("info", {})
                more_records = bool(info.get("more_records", False))
                if not users:
                    break
                for record in users:
                    zoho_records_read += 1
                    cursor.execute("SAVEPOINT before_upsert")
                    try:
                        modified_at_raw = record.get("Modified_Time") or record.get("modified_time") or record.get("modifiedTime")
                        created_at_raw = record.get("Created_Time") or record.get("created_time") or record.get("createdTime")
                        modified_at = datetime.fromisoformat(str(modified_at_raw).replace("Z", "+00:00")) if modified_at_raw else datetime.now(timezone.utc)
                        created_at = datetime.fromisoformat(str(created_at_raw).replace("Z", "+00:00")) if created_at_raw else None
                        if modified_at > new_watermark:
                            new_watermark = modified_at
                        cursor.execute(
                            "SELECT zoho_modified_at FROM crm_users WHERE zoho_uid = %s",
                            (str(record.get("id", "")),),
                        )
                        existing = cursor.fetchone()
                        existing_modified = existing[0] if existing and existing[0] else None
                        if existing_modified and modified_at <= existing_modified:
                            unchanged += 1
                            cursor.execute("RELEASE SAVEPOINT before_upsert")
                            continue
                        cursor.execute(
                            """
                            INSERT INTO crm_users (
                                zoho_uid, given_name, family_name, display_name, email_address, phone_number, mobile_number,
                                account_status, is_confirmed, user_type, zoho_role_id, zoho_role_name, zoho_profile_id,
                                zoho_profile_name, reports_to_uid, country_code, locale_code, iana_timezone, zoho_created_at,
                                zoho_modified_at, local_synced_at, local_created_at
                            ) VALUES (
                                %s, %s, %s, %s, %s, %s, %s,
                                %s, %s, %s, %s, %s, %s,
                                %s, %s, %s, %s, %s, %s,
                                %s, NOW(), COALESCE((SELECT local_created_at FROM crm_users WHERE zoho_uid = %s), NOW())
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
                                (record.get("role") or {}).get("id") if isinstance(record.get("role"), dict) else None,
                                (record.get("role") or {}).get("name") if isinstance(record.get("role"), dict) else None,
                                (record.get("profile") or {}).get("id") if isinstance(record.get("profile"), dict) else None,
                                (record.get("profile") or {}).get("name") if isinstance(record.get("profile"), dict) else None,
                                (record.get("reporting_to") or {}).get("id") if isinstance(record.get("reporting_to"), dict) else None,
                                record.get("country"),
                                record.get("country_locale"),
                                record.get("time_zone"),
                                created_at,
                                modified_at,
                                str(record.get("id", "")),
                            ),
                        )
                        upserted += 1
                        cursor.execute("RELEASE SAVEPOINT before_upsert")
                    except Exception as exc:
                        cursor.execute("ROLLBACK TO SAVEPOINT before_upsert")
                        errors += 1
                        error_detail.append({"zoho_id": str(record.get("id", "")), "reason": "email constraint violation"})
                        logger.error(json.dumps({"event": "sync_upsert_error", "error": str(exc)}), exc_info=True)
                page += 1
            if pages_fetched > 0:
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
            else:
                conn.rollback()
            return SyncUsersResponse(
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
        except HTTPException:
            conn.rollback()
            raise
        except Exception as exc:
            conn.rollback()
            logger.error(json.dumps({"event": "service_error", "error": str(exc)}), exc_info=True)
            raise HTTPException(status_code=500, detail="Internal Error") from exc
        finally:
            release_conn(conn)

    async def get_local_user_by_pk(self, user_pk: int, authorization: str, x_correlation_id: str | None) -> LocalUserResponse:
        logger.info(json.dumps({"event": "service_entry", "operation": "get_local_user_by_pk", "resource": "users"}))
        conn = get_conn()
        try:
            conn.rollback()
            cursor = conn.cursor()
            cursor.execute("SELECT * FROM crm_users WHERE user_pk = %s", (user_pk,))
            row = cursor.fetchone()
            if not row:
                raise HTTPException(status_code=404, detail="Resource Not Found")
            columns = [desc[0] for desc in cursor.description]
            user = dict(zip(columns, row, strict=False))
            return LocalUserResponse(status="success", user=user)
        except HTTPException:
            raise
        except Exception as exc:
            logger.error(json.dumps({"event": "service_error", "error": str(exc)}), exc_info=True)
            raise HTTPException(status_code=500, detail="Internal Error") from exc
        finally:
            release_conn(conn)

    async def get_local_user_by_zoho_uid(self, zoho_uid: str, authorization: str, x_correlation_id: str | None) -> LocalUserResponse:
        logger.info(json.dumps({"event": "service_entry", "operation": "get_local_user_by_zoho_uid", "resource": "users"}))
        conn = get_conn()
        try:
            conn.rollback()
            cursor = conn.cursor()
            cursor.execute("SELECT * FROM crm_users WHERE zoho_uid = %s", (zoho_uid,))
            row = cursor.fetchone()
            if not row:
                raise HTTPException(status_code=404, detail="Resource Not Found")
            columns = [desc[0] for desc in cursor.description]
            user = dict(zip(columns, row, strict=False))
            return LocalUserResponse(status="success", user=user)
        except HTTPException:
            raise
        except Exception as exc:
            logger.error(json.dumps({"event": "service_error", "error": str(exc)}), exc_info=True)
            raise HTTPException(status_code=500, detail="Internal Error") from exc
        finally:
            release_conn(conn)

    async def get_local_users(
        self,
        authorization: str,
        x_correlation_id: str | None,
        account_status: str | None,
        zoho_role_id: str | None,
        zoho_profile_id: str | None,
        is_confirmed: bool | None,
        synced_after: str | None,
        page: int,
        page_size: int,
        sort_by: str,
        sort_order: str,
    ) -> LocalUserListResponse:
        logger.info(json.dumps({"event": "service_entry", "operation": "get_local_users", "resource": "users"}))
        conn = get_conn()
        try:
            conn.rollback()
            cursor = conn.cursor()
            where_clauses = []
            params: list[Any] = []
            if account_status is not None:
                where_clauses.append("account_status = %s")
                params.append(account_status)
            if zoho_role_id is not None:
                where_clauses.append("zoho_role_id = %s")
                params.append(zoho_role_id)
            if zoho_profile_id is not None:
                where_clauses.append("zoho_profile_id = %s")
                params.append(zoho_profile_id)
            if is_confirmed is not None:
                where_clauses.append("is_confirmed = %s")
                params.append(is_confirmed)
            if synced_after is not None:
                where_clauses.append("local_synced_at >= %s")
                params.append(datetime.fromisoformat(synced_after.replace("Z", "+00:00")))
            where_sql = " WHERE " + " AND ".join(where_clauses) if where_clauses else ""
            count_sql = f"SELECT COUNT(*) FROM crm_users{where_sql}"
            cursor.execute(count_sql, tuple(params))
            total_count = cursor.fetchone()[0]
            order_sql = f" ORDER BY {sort_by} {sort_order.upper()}"
            data_sql = f"SELECT * FROM crm_users{where_sql}{order_sql} LIMIT %s OFFSET %s"
            cursor.execute(data_sql, tuple(params + [page_size, (page - 1) * page_size]))
            rows = cursor.fetchall()
            columns = [desc[0] for desc in cursor.description]
            users = [dict(zip(columns, row, strict=False)) for row in rows]
            return LocalUserListResponse(status="success", page=page, page_size=page_size, total_count=total_count, users=users)
        except HTTPException:
            raise
        except Exception as exc:
            logger.error(json.dumps({"event": "service_error", "error": str(exc)}), exc_info=True)
            raise HTTPException(status_code=500, detail="Internal Error") from exc
        finally:
            release_conn(conn)

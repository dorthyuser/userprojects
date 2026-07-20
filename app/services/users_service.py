import json
import logging
from dataclasses import asdict
from datetime import datetime, timezone
from typing import Any

import psycopg2
from fastapi import HTTPException
from psycopg2.extras import RealDictCursor

from app.connections.zoho_http_connection import ZohoHttpConnectionConnection
from app.db.connection import get_conn, release_conn
from app.models.users_model import LocalUserRecord, ZohoUserRecord
from app.schemas.users_schema import CreateUserRequest, DeltaSyncRequest

logger = logging.getLogger(__name__)


class UsersService:
    def __init__(self) -> None:
        self._connection = ZohoHttpConnectionConnection.instance()

    def create_user(self, payload: CreateUserRequest, correlation_id: str | None) -> tuple[int, dict[str, Any]]:
        logger.info(json.dumps({"event": "service_entry", "operation": "create_user", "resource": "users"}))
        try:
            self._validate_create_payload(payload)
            status_code, duplicate_body = self._connection.request(
                method="GET",
                path="/crm/v8/users",
                params={"type": "AllUsers", "page": 1, "per_page": 10},
                headers={"X-Correlation-Id": correlation_id} if correlation_id else None,
            )
            if status_code == 200 and self._email_exists(duplicate_body, payload.users[0].email):
                return 409, {"status": "error", "code": "DUPLICATE_EMAIL", "message": "A user with this email already exists in the Zoho org."}
            status_code, body = self._connection.request(
                method="POST",
                path="/crm/v8/users",
                json=payload.model_dump(mode="json"),
                headers={"X-Correlation-Id": correlation_id} if correlation_id else None,
            )
            if status_code not in (200, 201):
                return self._translate_zoho_error(status_code, body)
            zoho_id = self._extract_zoho_id(body)
            created_at = datetime.now(timezone.utc).isoformat().replace("+00:00", "Z")
            logger.info(json.dumps({"event": "zoho_user_created", "zoho_id": zoho_id}))
            return 201, {"status": "success", "zoho_id": zoho_id, "email": payload.users[0].email, "created_at": created_at}
        except HTTPException:
            raise
        except Exception as exc:
            logger.error(json.dumps({"event": "create_user_failed", "error": str(exc)}), exc_info=True)
            raise HTTPException(status_code=500, detail="Internal Error")

    def get_zoho_user(self, zoho_id: str, correlation_id: str | None) -> tuple[int, dict[str, Any]]:
        logger.info(json.dumps({"event": "service_entry", "operation": "get_zoho_user", "resource": "users"}))
        try:
            status_code, body = self._connection.request(
                method="GET",
                path=f"/crm/v8/users/{zoho_id}",
                headers={"X-Correlation-Id": correlation_id} if correlation_id else None,
            )
            if status_code == 404:
                return 404, {"status": "error", "code": "USER_NOT_FOUND", "message": "No Zoho user found for the given ID."}
            if status_code != 200:
                return self._translate_zoho_error(status_code, body)
            user = self._normalize_zoho_user(body)
            return 200, {"status": "success", "user": user}
        except HTTPException:
            raise
        except Exception as exc:
            logger.error(json.dumps({"event": "get_zoho_user_failed", "error": str(exc)}), exc_info=True)
            raise HTTPException(status_code=500, detail="Internal Error")

    def list_zoho_users(
        self,
        user_type: str,
        page: int,
        per_page: int,
        if_modified_since: str | None,
        correlation_id: str | None,
    ) -> tuple[int, dict[str, Any]]:
        logger.info(json.dumps({"event": "service_entry", "operation": "list_zoho_users", "resource": "users"}))
        try:
            params = {"type": user_type, "page": page, "per_page": per_page}
            headers = {"X-Correlation-Id": correlation_id} if correlation_id else None
            if if_modified_since:
                headers = headers or {}
                headers["If-Modified-Since"] = if_modified_since
            status_code, body = self._connection.request(method="GET", path="/crm/v8/users", params=params, headers=headers)
            if status_code != 200:
                return self._translate_zoho_error(status_code, body)
            users = [self._normalize_zoho_user(user) for user in body.get("users", [])]
            info = body.get("info", {"page": page, "per_page": per_page, "count": len(users), "more_records": False})
            return 200, {"status": "success", "info": info, "users": users}
        except HTTPException:
            raise
        except Exception as exc:
            logger.error(json.dumps({"event": "list_zoho_users_failed", "error": str(exc)}), exc_info=True)
            raise HTTPException(status_code=500, detail="Internal Error")

    def sync_users(self, payload: DeltaSyncRequest | None, correlation_id: str | None) -> tuple[int, dict[str, Any]]:
        logger.info(json.dumps({"event": "service_entry", "operation": "sync_users", "resource": "users"}))
        conn = get_conn()
        try:
            conn.rollback()
            conn.autocommit = False
            with conn.cursor(cursor_factory=RealDictCursor) as cursor:
                watermark = self._read_watermark(cursor)
                full_sync = bool(payload.full_sync) if payload else False
                if full_sync:
                    watermark = datetime(1970, 1, 1, tzinfo=timezone.utc)
                page = 1
                more_records = True
                new_watermark = watermark
                pages_fetched = 0
                zoho_records_read = 0
                upserted = 0
                unchanged = 0
                errors = 0
                error_detail: list[dict[str, Any]] = []
                while more_records:
                    headers = {"If-Modified-Since": watermark.isoformat()}
                    if correlation_id:
                        headers["X-Correlation-Id"] = correlation_id
                    status_code, body = self._connection.request(
                        method="GET",
                        path="/crm/v8/users",
                        params={"type": payload.type if payload and payload.type else "AllUsers", "page": page, "per_page": payload.per_page if payload and payload.per_page else 200},
                        headers=headers,
                    )
                    if status_code == 304:
                        break
                    if status_code != 200:
                        return self._translate_zoho_error(status_code, body)
                    pages_fetched += 1
                    users = body.get("users", [])
                    if not users:
                        break
                    zoho_records_read += len(users)
                    for record in users:
                        cursor.execute("SAVEPOINT before_upsert")
                        try:
                            record_model = self._map_zoho_to_local(record)
                            if record_model.zoho_modified_at > new_watermark:
                                new_watermark = record_model.zoho_modified_at
                            affected = self._upsert_local_user(cursor, record_model)
                            if affected:
                                upserted += 1
                            else:
                                unchanged += 1
                            cursor.execute("RELEASE SAVEPOINT before_upsert")
                        except Exception as exc:
                            cursor.execute("ROLLBACK TO SAVEPOINT before_upsert")
                            errors += 1
                            error_detail.append({"zoho_id": str(record.get("id") or record.get("ID") or ""), "reason": "upsert failed"})
                            logger.error(json.dumps({"event": "sync_upsert_failed", "error": str(exc)}))
                    more_records = bool(body.get("info", {}).get("more_records", False))
                    page += 1
                if pages_fetched > 0 and (errors == 0 or upserted > 0):
                    self._update_watermark(cursor, new_watermark, upserted + unchanged)
                    conn.commit()
                else:
                    conn.rollback()
                response: dict[str, Any] = {
                    "status": "success" if errors == 0 else "partial",
                    "watermark_used": watermark.isoformat(),
                    "new_watermark": new_watermark.isoformat(),
                    "pages_fetched": pages_fetched,
                    "zoho_records_read": zoho_records_read,
                    "upserted": upserted,
                    "unchanged": unchanged,
                    "errors": errors,
                    "sync_duration_ms": 0,
                }
                if error_detail:
                    response["error_detail"] = error_detail
                return (207 if errors else 200), response
        except HTTPException:
            conn.rollback()
            raise
        except psycopg2.Error as exc:
            conn.rollback()
            logger.error(json.dumps({"event": "database_error", "error": str(exc)}), exc_info=True)
            raise HTTPException(status_code=503, detail="Database Error")
        except Exception as exc:
            conn.rollback()
            logger.error(json.dumps({"event": "sync_users_failed", "error": str(exc)}), exc_info=True)
            raise HTTPException(status_code=500, detail="Internal Error")
        finally:
            release_conn(conn)

    def get_local_user_by_pk(self, user_pk: int, correlation_id: str | None) -> tuple[int, dict[str, Any]]:
        logger.info(json.dumps({"event": "service_entry", "operation": "get_local_user_by_pk", "resource": "users"}))
        return self._get_local_user("user_pk", user_pk, correlation_id)

    def get_local_user_by_zoho_uid(self, zoho_uid: str, correlation_id: str | None) -> tuple[int, dict[str, Any]]:
        logger.info(json.dumps({"event": "service_entry", "operation": "get_local_user_by_zoho_uid", "resource": "users"}))
        return self._get_local_user("zoho_uid", zoho_uid, correlation_id)

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
        logger.info(json.dumps({"event": "service_entry", "operation": "list_local_users", "resource": "users"}))
        conn = get_conn()
        try:
            conn.rollback()
            conn.autocommit = False
            with conn.cursor(cursor_factory=RealDictCursor) as cursor:
                where_clauses: list[str] = ["1=1"]
                params: list[Any] = []
                if account_status:
                    where_clauses.append("account_status = %s")
                    params.append(account_status)
                if zoho_role_id:
                    where_clauses.append("zoho_role_id = %s")
                    params.append(zoho_role_id)
                if zoho_profile_id:
                    where_clauses.append("zoho_profile_id = %s")
                    params.append(zoho_profile_id)
                if is_confirmed is not None:
                    where_clauses.append("is_confirmed = %s")
                    params.append(is_confirmed)
                if synced_after:
                    where_clauses.append("local_synced_at >= %s")
                    params.append(synced_after)
                safe_sort = sort_by if sort_by in {"family_name", "email_address", "zoho_modified_at", "local_synced_at"} else "family_name"
                safe_order = sort_order.lower() if sort_order.lower() in {"asc", "desc"} else "asc"
                offset = (page - 1) * page_size
                count_sql = f"SELECT COUNT(*) AS total_count FROM crm_users WHERE {' AND '.join(where_clauses)}"
                cursor.execute(count_sql, tuple(params))
                total_count = int(cursor.fetchone()["total_count"])
                sql = f"SELECT * FROM crm_users WHERE {' AND '.join(where_clauses)} ORDER BY {safe_sort} {safe_order} LIMIT %s OFFSET %s"
                cursor.execute(sql, tuple(params + [page_size, offset]))
                rows = cursor.fetchall()
                users = [self._row_to_local_response(row) for row in rows]
                conn.commit()
                return 200, {"status": "success", "page": page, "page_size": page_size, "total_count": total_count, "users": users}
        except psycopg2.Error as exc:
            conn.rollback()
            logger.error(json.dumps({"event": "database_error", "error": str(exc)}), exc_info=True)
            raise HTTPException(status_code=503, detail="Database Error")
        except Exception as exc:
            conn.rollback()
            logger.error(json.dumps({"event": "list_local_users_failed", "error": str(exc)}), exc_info=True)
            raise HTTPException(status_code=500, detail="Internal Error")
        finally:
            release_conn(conn)

    def _get_local_user(self, column: str, value: Any, correlation_id: str | None) -> tuple[int, dict[str, Any]]:
        conn = get_conn()
        try:
            conn.rollback()
            conn.autocommit = False
            with conn.cursor(cursor_factory=RealDictCursor) as cursor:
                logger.info(json.dumps({"event": "db_operation", "table": "crm_users", "operation": "SELECT"}))
                cursor.execute(f"SELECT * FROM crm_users WHERE {column} = %s", (value,))
                row = cursor.fetchone()
                if not row:
                    return 404, {"status": "error", "code": "USER_NOT_FOUND", "message": "No local user record found for the given identifier."}
                conn.commit()
                return 200, {"status": "success", "user": self._row_to_local_response(row)}
        except psycopg2.Error as exc:
            conn.rollback()
            logger.error(json.dumps({"event": "database_error", "error": str(exc)}), exc_info=True)
            raise HTTPException(status_code=503, detail="Database Error")
        except Exception as exc:
            conn.rollback()
            logger.error(json.dumps({"event": "get_local_user_failed", "error": str(exc)}), exc_info=True)
            raise HTTPException(status_code=500, detail="Internal Error")
        finally:
            release_conn(conn)

    def _validate_create_payload(self, payload: CreateUserRequest) -> None:
        if not payload.users or len(payload.users) != 1:
            raise HTTPException(status_code=422, detail="Validation Error")

    def _email_exists(self, body: dict[str, Any], email: str) -> bool:
        for user in body.get("users", []):
            details = user.get("details", {})
            if details.get("email") == email:
                return True
        return False

    def _extract_zoho_id(self, body: dict[str, Any]) -> str:
        users = body.get("users", [])
        if not users:
            raise HTTPException(status_code=500, detail="Internal Error")
        details = users[0].get("details", {})
        zoho_id = str(details.get("id", ""))
        if not zoho_id:
            raise HTTPException(status_code=500, detail="Internal Error")
        return zoho_id

    def _translate_zoho_error(self, status_code: int, body: Any) -> tuple[int, dict[str, Any]]:
        if status_code == 401:
            return 401, {"status": "error", "code": "UNAUTHORIZED", "message": "Authentication Error"}
        if status_code == 403:
            return 403, {"status": "error", "code": "FORBIDDEN", "message": "Authentication Error"}
        if status_code == 429:
            return 503, {"status": "error", "code": "RATE_LIMITED", "message": "Service Unavailable"}
        if status_code == 404:
            return 404, {"status": "error", "code": "USER_NOT_FOUND", "message": "No Zoho user found for the given ID."}
        return 502, {"status": "error", "code": "UPSTREAM_ERROR", "message": "Service Unavailable"}

    def _normalize_zoho_user(self, body: dict[str, Any]) -> dict[str, Any]:
        if "user" in body:
            return body["user"]
        if "users" in body and body["users"]:
            record = body["users"][0]
            return {
                "id": record.get("id") or record.get("ID"),
                "first_name": record.get("first_name") or record.get("firstName"),
                "last_name": record.get("last_name") or record.get("lastName"),
                "full_name": record.get("full_name") or record.get("fullName"),
                "email": record.get("email"),
                "phone": record.get("phone"),
                "mobile": record.get("mobile"),
                "status": record.get("status"),
                "confirm": record.get("confirm"),
                "type__s": record.get("type__s"),
                "role": record.get("role"),
                "profile": record.get("profile"),
                "reporting_to": record.get("reporting_to"),
                "country": record.get("country"),
                "country_locale": record.get("country_locale"),
                "time_zone": record.get("time_zone"),
                "language": record.get("language"),
                "created_time": record.get("created_time") or record.get("Created_Time"),
                "modified_time": record.get("modified_time") or record.get("Modified_Time"),
                "created_by": record.get("created_by"),
            }
        return body

    def _map_zoho_to_local(self, record: dict[str, Any]) -> ZohoUserRecord:
        return ZohoUserRecord(
            zoho_uid=str(record.get("id") or record.get("ID") or ""),
            given_name=record.get("first_name") or record.get("firstName"),
            family_name=record.get("last_name") or record.get("lastName"),
            display_name=record.get("full_name") or record.get("fullName"),
            email_address=record.get("email"),
            phone_number=record.get("phone"),
            mobile_number=record.get("mobile"),
            account_status=record.get("status"),
            is_confirmed=record.get("confirm"),
            user_type=record.get("type__s"),
            zoho_role_id=(record.get("role") or {}).get("id") if isinstance(record.get("role"), dict) else None,
            zoho_role_name=(record.get("role") or {}).get("name") if isinstance(record.get("role"), dict) else None,
            zoho_profile_id=(record.get("profile") or {}).get("id") if isinstance(record.get("profile"), dict) else None,
            zoho_profile_name=(record.get("profile") or {}).get("name") if isinstance(record.get("profile"), dict) else None,
            reports_to_uid=(record.get("reporting_to") or {}).get("id") if isinstance(record.get("reporting_to"), dict) else None,
            country_code=record.get("country"),
            locale_code=record.get("country_locale"),
            iana_timezone=record.get("time_zone"),
            zoho_created_at=record.get("created_time") or record.get("Created_Time"),
            zoho_modified_at=record.get("modified_time") or record.get("Modified_Time"),
        )

    def _upsert_local_user(self, cursor: Any, record: ZohoUserRecord) -> int:
        logger.info(json.dumps({"event": "db_operation", "table": "crm_users", "operation": "INSERT"}))
        sql = """
        INSERT INTO crm_users (
            zoho_uid, given_name, family_name, display_name, email_address, phone_number, mobile_number,
            account_status, is_confirmed, user_type, zoho_role_id, zoho_role_name, zoho_profile_id,
            zoho_profile_name, reports_to_uid, country_code, locale_code, iana_timezone, zoho_created_at,
            zoho_modified_at, local_synced_at, local_created_at
        ) VALUES (
            %s, %s, %s, %s, %s, %s, %s,
            %s, %s, %s, %s, %s, %s,
            %s, %s, %s, %s, %s, %s,
            %s, NOW(), NOW()
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
        """
        cursor.execute(
            sql,
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
            ),
        )
        return cursor.rowcount

    def _read_watermark(self, cursor: Any) -> datetime:
        logger.info(json.dumps({"event": "db_operation", "table": "sync_state", "operation": "SELECT"}))
        cursor.execute("SELECT last_synced_at FROM sync_state WHERE sync_key = %s", ("zoho_users",))
        row = cursor.fetchone()
        if not row or not row.get("last_synced_at"):
            return datetime(1970, 1, 1, tzinfo=timezone.utc)
        return row["last_synced_at"]

    def _update_watermark(self, cursor: Any, watermark: datetime, records_synced: int) -> None:
        logger.info(json.dumps({"event": "db_operation", "table": "sync_state", "operation": "UPDATE"}))
        cursor.execute(
            """
            INSERT INTO sync_state (sync_key, last_synced_at, last_run_at, records_synced)
            VALUES (%s, %s, NOW(), %s)
            ON CONFLICT (sync_key) DO UPDATE SET
                last_synced_at = EXCLUDED.last_synced_at,
                last_run_at = NOW(),
                records_synced = EXCLUDED.records_synced
            """,
            ("zoho_users", watermark, records_synced),
        )

    def _row_to_local_response(self, row: Any) -> dict[str, Any]:
        return {
            "user_pk": row["user_pk"],
            "zoho_uid": row["zoho_uid"],
            "given_name": row["given_name"],
            "family_name": row["family_name"],
            "display_name": row["display_name"],
            "email_address": row["email_address"],
            "phone_number": row["phone_number"],
            "mobile_number": row["mobile_number"],
            "account_status": row["account_status"],
            "is_confirmed": row["is_confirmed"],
            "user_type": row["user_type"],
            "zoho_role_id": row["zoho_role_id"],
            "zoho_role_name": row["zoho_role_name"],
            "zoho_profile_id": row["zoho_profile_id"],
            "zoho_profile_name": row["zoho_profile_name"],
            "reports_to_uid": row["reports_to_uid"],
            "country_code": row["country_code"],
            "locale_code": row["locale_code"],
            "iana_timezone": row["iana_timezone"],
            "zoho_created_at": row["zoho_created_at"].isoformat() if row["zoho_created_at"] else None,
            "zoho_modified_at": row["zoho_modified_at"].isoformat() if row["zoho_modified_at"] else None,
            "local_synced_at": row["local_synced_at"].isoformat() if row["local_synced_at"] else None,
        }

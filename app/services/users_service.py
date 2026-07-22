import json
import logging
from abc import ABC, abstractmethod
from dataclasses import asdict
from typing import Any

from app.connections.zoho_http_connection import ZohoHttpConnectionConnection
from app.db.connection import get_conn, release_conn
from app.models.users_model import LocalUserRecord, ZohoUserRecord
from app.schemas.users_schema import CreateUserRequest, DeltaSyncRequest

logger = logging.getLogger(__name__)


class IZohoHttpConnectionService(ABC):
    @abstractmethod
    def create_user(self, payload: CreateUserRequest, correlation_id: str | None) -> tuple[int, Any]:
        raise NotImplementedError

    @abstractmethod
    def get_zoho_user(self, zoho_id: str, correlation_id: str | None) -> tuple[int, Any]:
        raise NotImplementedError

    @abstractmethod
    def list_zoho_users(self, type: str, page: int, per_page: int, if_modified_since: str | None, correlation_id: str | None) -> tuple[int, Any]:
        raise NotImplementedError

    @abstractmethod
    def sync_users(self, payload: DeltaSyncRequest | None, correlation_id: str | None) -> tuple[int, Any]:
        raise NotImplementedError

    @abstractmethod
    def get_local_user_by_pk(self, user_pk: int, correlation_id: str | None) -> tuple[int, Any]:
        raise NotImplementedError

    @abstractmethod
    def get_local_user_by_zoho_uid(self, zoho_uid: str, correlation_id: str | None) -> tuple[int, Any]:
        raise NotImplementedError

    @abstractmethod
    def get_local_users(self, account_status: str | None, zoho_role_id: str | None, zoho_profile_id: str | None, is_confirmed: bool | None, synced_after: str | None, page: int, page_size: int, sort_by: str, sort_order: str, correlation_id: str | None) -> tuple[int, Any]:
        raise NotImplementedError


class ZohoHttpConnectionService(IZohoHttpConnectionService):
    def __init__(self, connection: ZohoHttpConnectionConnection) -> None:
        self._connection = connection

    def create_user(self, payload: CreateUserRequest, correlation_id: str | None) -> tuple[int, Any]:
        logger.info(json.dumps({"event": "service_entry", "operation": "create_user", "resource": "users"}))
        return self._connection.create_user(payload=payload, correlation_id=correlation_id)

    def get_zoho_user(self, zoho_id: str, correlation_id: str | None) -> tuple[int, Any]:
        logger.info(json.dumps({"event": "service_entry", "operation": "get_zoho_user", "resource": "users"}))
        return self._connection.get_zoho_user(zoho_id=zoho_id, correlation_id=correlation_id)

    def list_zoho_users(self, type: str, page: int, per_page: int, if_modified_since: str | None, correlation_id: str | None) -> tuple[int, Any]:
        logger.info(json.dumps({"event": "service_entry", "operation": "list_zoho_users", "resource": "users"}))
        return self._connection.list_zoho_users(type=type, page=page, per_page=per_page, if_modified_since=if_modified_since, correlation_id=correlation_id)

    def sync_users(self, payload: DeltaSyncRequest | None, correlation_id: str | None) -> tuple[int, Any]:
        logger.info(json.dumps({"event": "service_entry", "operation": "sync_users", "resource": "users"}))
        return self._sync_users(payload=payload, correlation_id=correlation_id)

    def get_local_user_by_pk(self, user_pk: int, correlation_id: str | None) -> tuple[int, Any]:
        logger.info(json.dumps({"event": "service_entry", "operation": "get_local_user_by_pk", "resource": "users"}))
        return self._get_local_single(where_clause="user_pk = %s", value=user_pk, correlation_id=correlation_id)

    def get_local_user_by_zoho_uid(self, zoho_uid: str, correlation_id: str | None) -> tuple[int, Any]:
        logger.info(json.dumps({"event": "service_entry", "operation": "get_local_user_by_zoho_uid", "resource": "users"}))
        return self._get_local_single(where_clause="zoho_uid = %s", value=zoho_uid, correlation_id=correlation_id)

    def get_local_users(self, account_status: str | None, zoho_role_id: str | None, zoho_profile_id: str | None, is_confirmed: bool | None, synced_after: str | None, page: int, page_size: int, sort_by: str, sort_order: str, correlation_id: str | None) -> tuple[int, Any]:
        logger.info(json.dumps({"event": "service_entry", "operation": "get_local_users", "resource": "users"}))
        conn = get_conn()
        try:
            conn.rollback()
            conn.autocommit = False
            with conn.cursor() as cursor:
                filters: list[str] = []
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
                where_sql = " WHERE " + " AND ".join(filters) if filters else ""
                allowed_sort = {"family_name", "email_address", "zoho_modified_at", "local_synced_at"}
                safe_sort = sort_by if sort_by in allowed_sort else "family_name"
                safe_order = sort_order.lower() if sort_order.lower() in {"asc", "desc"} else "asc"
                offset = (page - 1) * page_size
                cursor.execute(f"SELECT COUNT(*) FROM crm_users{where_sql}", tuple(params))
                total_count_row = cursor.fetchone()
                total_count = int(total_count_row[0]) if total_count_row else 0
                cursor.execute(f"SELECT user_pk, zoho_uid, given_name, family_name, display_name, email_address, phone_number, mobile_number, account_status, is_confirmed, user_type, zoho_role_id, zoho_role_name, zoho_profile_id, zoho_profile_name, reports_to_uid, country_code, locale_code, iana_timezone, zoho_created_at, zoho_modified_at, local_synced_at FROM crm_users{where_sql} ORDER BY {safe_sort} {safe_order} LIMIT %s OFFSET %s", tuple(params) + (page_size, offset))
                rows = cursor.fetchall()
                users = [self._row_to_local_user(row) for row in rows]
                body = {"status": "success", "page": page, "page_size": page_size, "total_count": total_count, "users": users}
                return 200, body
        except Exception as exc:
            logger.error(json.dumps({"event": "db_error", "error": str(exc)}), exc_info=True)
            return 503, {"status": "error", "code": "DATABASE_ERROR", "message": "Database Error"}
        finally:
            release_conn(conn)

    def _sync_users(self, payload: DeltaSyncRequest | None, correlation_id: str | None) -> tuple[int, Any]:
        conn = get_conn()
        try:
            conn.rollback()
            conn.autocommit = False
            with conn.cursor() as cursor:
                cursor.execute("SELECT last_synced_at FROM sync_state WHERE sync_key = %s", ("zoho_users",))
                row = cursor.fetchone()
                watermark = row[0] if row and row[0] else None
                if payload and payload.full_sync:
                    watermark = None
                pages_fetched = 0
                zoho_records_read = 0
                upserted = 0
                unchanged = 0
                errors = 0
                error_detail: list[dict[str, Any]] = []
                new_watermark = watermark
                page = 1
                more_records = True
                if watermark is None:
                    watermark = "1970-01-01T00:00:00Z"
                while more_records:
                    status_code, body = self._connection.list_zoho_users(type=payload.type if payload and payload.type else "AllUsers", page=page, per_page=payload.per_page if payload and payload.per_page else 200, if_modified_since=watermark, correlation_id=correlation_id)
                    if status_code == 304:
                        break
                    if status_code != 200:
                        return status_code, body
                    pages_fetched += 1
                    users = body.get("users", []) if isinstance(body, dict) else []
                    if not users:
                        break
                    info = body.get("info", {}) if isinstance(body, dict) else {}
                    more_records = bool(info.get("more_records", False))
                    for user in users:
                        zoho_records_read += 1
                        record = ZohoUserRecord.from_zoho(user)
                        cursor.execute("SAVEPOINT before_upsert")
                        try:
                            cursor.execute("SELECT zoho_modified_at FROM crm_users WHERE zoho_uid = %s", (record.zoho_uid,))
                            existing = cursor.fetchone()
                            if existing and existing[0] and record.zoho_modified_at <= existing[0]:
                                unchanged += 1
                                cursor.execute("RELEASE SAVEPOINT before_upsert")
                                continue
                            cursor.execute(
                                "INSERT INTO crm_users (zoho_uid, given_name, family_name, display_name, email_address, phone_number, mobile_number, account_status, is_confirmed, user_type, zoho_role_id, zoho_role_name, zoho_profile_id, zoho_profile_name, reports_to_uid, country_code, locale_code, iana_timezone, zoho_created_at, zoho_modified_at, local_synced_at, local_created_at) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, NOW(), COALESCE((SELECT local_created_at FROM crm_users WHERE zoho_uid = %s), NOW())) ON CONFLICT (zoho_uid) DO UPDATE SET given_name = EXCLUDED.given_name, family_name = EXCLUDED.family_name, display_name = EXCLUDED.display_name, email_address = EXCLUDED.email_address, phone_number = EXCLUDED.phone_number, mobile_number = EXCLUDED.mobile_number, account_status = EXCLUDED.account_status, is_confirmed = EXCLUDED.is_confirmed, user_type = EXCLUDED.user_type, zoho_role_id = EXCLUDED.zoho_role_id, zoho_role_name = EXCLUDED.zoho_role_name, zoho_profile_id = EXCLUDED.zoho_profile_id, zoho_profile_name = EXCLUDED.zoho_profile_name, reports_to_uid = EXCLUDED.reports_to_uid, country_code = EXCLUDED.country_code, locale_code = EXCLUDED.locale_code, iana_timezone = EXCLUDED.iana_timezone, zoho_created_at = EXCLUDED.zoho_created_at, zoho_modified_at = EXCLUDED.zoho_modified_at, local_synced_at = NOW() WHERE crm_users.zoho_modified_at < EXCLUDED.zoho_modified_at",
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
                            upserted += 1
                            if new_watermark is None or record.zoho_modified_at > new_watermark:
                                new_watermark = record.zoho_modified_at
                            cursor.execute("RELEASE SAVEPOINT before_upsert")
                        except Exception as exc:
                            cursor.execute("ROLLBACK TO SAVEPOINT before_upsert")
                            errors += 1
                            error_detail.append({"zoho_id": record.zoho_uid, "reason": "email constraint violation"})
                            logger.error(json.dumps({"event": "sync_upsert_failed", "error": str(exc)}))
                    page += 1
                if pages_fetched > 0 and errors == 0:
                    cursor.execute("INSERT INTO sync_state (sync_key, last_synced_at, last_run_at, records_synced) VALUES (%s, %s, NOW(), %s) ON CONFLICT (sync_key) DO UPDATE SET last_synced_at = EXCLUDED.last_synced_at, last_run_at = NOW(), records_synced = EXCLUDED.records_synced", ("zoho_users", new_watermark or watermark, upserted))
                    conn.commit()
                    return 200, {"status": "success", "watermark_used": watermark, "new_watermark": new_watermark or watermark, "pages_fetched": pages_fetched, "zoho_records_read": zoho_records_read, "upserted": upserted, "unchanged": unchanged, "errors": errors, "sync_duration_ms": 0}
                if pages_fetched > 0 and errors > 0:
                    cursor.execute("INSERT INTO sync_state (sync_key, last_synced_at, last_run_at, records_synced) VALUES (%s, %s, NOW(), %s) ON CONFLICT (sync_key) DO UPDATE SET last_synced_at = EXCLUDED.last_synced_at, last_run_at = NOW(), records_synced = EXCLUDED.records_synced", ("zoho_users", new_watermark or watermark, upserted))
                    conn.commit()
                    return 207, {"status": "partial", "upserted": upserted, "errors": errors, "error_detail": error_detail}
                conn.rollback()
                return 200, {"status": "success", "watermark_used": watermark, "new_watermark": watermark, "zoho_records_read": 0, "upserted": 0}
        except Exception as exc:
            conn.rollback()
            logger.error(json.dumps({"event": "sync_failed", "error": str(exc)}), exc_info=True)
            return 500, {"status": "error", "code": "INTERNAL_ERROR", "message": "Internal Error"}
        finally:
            release_conn(conn)

    def _get_local_single(self, where_clause: str, value: Any, correlation_id: str | None) -> tuple[int, Any]:
        conn = get_conn()
        try:
            conn.rollback()
            conn.autocommit = False
            with conn.cursor() as cursor:
                cursor.execute(f"SELECT user_pk, zoho_uid, given_name, family_name, display_name, email_address, phone_number, mobile_number, account_status, is_confirmed, user_type, zoho_role_id, zoho_role_name, zoho_profile_id, zoho_profile_name, reports_to_uid, country_code, locale_code, iana_timezone, zoho_created_at, zoho_modified_at, local_synced_at FROM crm_users WHERE {where_clause}", (value,))
                row = cursor.fetchone()
                if not row:
                    return 404, {"status": "error", "code": "USER_NOT_FOUND", "message": "No local user record found for the given identifier."}
                return 200, {"status": "success", "user": self._row_to_local_user(row)}
        except Exception as exc:
            logger.error(json.dumps({"event": "db_error", "error": str(exc)}), exc_info=True)
            return 503, {"status": "error", "code": "DATABASE_ERROR", "message": "Database Error"}
        finally:
            release_conn(conn)

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
            "zoho_created_at": row[19].isoformat() if row[19] else None,
            "zoho_modified_at": row[20].isoformat() if row[20] else None,
            "local_synced_at": row[21].isoformat() if row[21] else None,
        }

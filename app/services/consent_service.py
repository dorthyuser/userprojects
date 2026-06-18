import hashlib
import json
import logging
import secrets
from datetime import datetime, timezone
from typing import Any
from uuid import UUID, uuid4

import psycopg2
from fastapi import HTTPException, Request

from app.db.connection import get_conn, release_conn
from app.models.consent_model import AuditRecord, ConsentRecord
from app.schemas.consent_schema import (
    ConsentAuditHistoryResponse,
    ConsentGrantRequest,
    ConsentGrantResponse,
    ConsentGetAllResponse,
    ConsentGetOneResponse,
    ConsentWithdrawRequest,
    ConsentWithdrawResponse,
)

logger = logging.getLogger(__name__)

VALID_PURPOSES = {"treatment", "research", "marketing", "data_sharing"}
VALID_LEGAL_BASES = {"consent", "vital_interest", "legal_obligation"}
VALID_CHANNELS = {"web", "paper", "phone", "in_person"}
VALID_ROLES = {"patient", "clinician", "dpo", "system"}
VALID_ACTIONS = {"GRANT", "WITHDRAW", "UPDATE", "EXPIRE", "VIEW"}


def _utcnow() -> datetime:
    return datetime.now(timezone.utc)


def _parse_uuid_v4(value: str, field_name: str) -> UUID:
    try:
        parsed = UUID(value)
    except Exception as exc:
        logger.info(json.dumps({"event": "validation_failure", "rule": field_name}))
        raise HTTPException(status_code=422, detail="Validation Error") from exc
    if parsed.version != 4:
        logger.info(json.dumps({"event": "validation_failure", "rule": field_name}))
        raise HTTPException(status_code=422, detail="Validation Error")
    return parsed


def _require_env(event: Request, key: str) -> str:
    value = event.headers.get(key)
    if value is None or not value.strip():
        logger.info(json.dumps({"event": "validation_failure", "rule": key}))
        raise HTTPException(status_code=422, detail="Validation Error")
    return value


def _get_actor_context(request: Request) -> tuple[str, str, str, str]:
    headers = request.headers
    actor_id = headers.get("x-actor-id") or headers.get("actor_id") or "system"
    actor_role = headers.get("x-actor-role") or headers.get("actor_role") or "system"
    source_ip = request.client.host if request.client else ""
    user_agent = headers.get("user-agent", "")
    if actor_role not in VALID_ROLES:
        logger.info(json.dumps({"event": "validation_failure", "rule": "actor_role"}))
        raise HTTPException(status_code=422, detail="Validation Error")
    return actor_id, actor_role, source_ip, user_agent


def _hash_record(previous_state: Any, new_state: Any, event_id: UUID, actor_id: str, actor_role: str) -> str:
    payload = json.dumps(
        {
            "previous_state": previous_state,
            "new_state": new_state,
            "event_id": str(event_id),
            "actor_id": actor_id,
            "actor_role": actor_role,
        },
        sort_keys=True,
        default=str,
    )
    return hashlib.sha256(payload.encode("utf-8")).hexdigest()


def _audit_view(conn, patient_id: UUID, actor_id: str, actor_role: str, source_ip: str, user_agent: str) -> tuple[int, datetime]:
    logger.info(json.dumps({"event": "db_operation", "table": "consent_audit_log", "operation": "INSERT"}))
    event_id = uuid4()
    occurred_at = _utcnow()
    record_hash = _hash_record(None, None, event_id, actor_id, actor_role)
    with conn.cursor() as cursor:
        cursor.execute(
            """
            INSERT INTO consent_audit_log
            (event_id, consent_id, patient_id, action, previous_state, new_state, actor_id, actor_role, source_ip, user_agent, occurred_at, record_hash)
            VALUES (%s, NULL, %s, %s::varchar, NULL, NULL, %s, %s, %s, %s, %s, %s)
            RETURNING audit_id, occurred_at
            """,
            (event_id, patient_id, "VIEW", actor_id, actor_role, source_ip, user_agent, occurred_at, record_hash),
        )
        row = cursor.fetchone()
        if row is None:
            conn.rollback()
            raise HTTPException(status_code=500, detail="Internal Error")
        return int(row[0]), row[1]


def grant_consent_service(request: Request, payload: ConsentGrantRequest, idempotency_key: str) -> ConsentGrantResponse:
    logger.info(json.dumps({"event": "service_entry", "operation": "grant_consent", "resource": "consent"}))
    actor_id, actor_role, source_ip, user_agent = _get_actor_context(request)
    patient_id = _parse_uuid_v4(str(payload.patient_id), "patient_id")
    conn = get_conn()
    try:
        conn.rollback()
        conn.autocommit = False
        with conn.cursor() as cursor:
            logger.info(json.dumps({"event": "db_operation", "table": "patient_consent", "operation": "SELECT"}))
            cursor.execute("SELECT consent_id, status FROM patient_consent WHERE patient_id = %s AND purpose = %s", (patient_id, payload.purpose))
            existing = cursor.fetchone()
            if existing and existing[1] == "withdrawn":
                logger.info(json.dumps({"event": "validation_failure", "rule": "already_withdrawn"}))
                raise HTTPException(status_code=422, detail="Validation Error")
            event_id = uuid4()
            occurred_at = _utcnow()
            consent_id = existing[0] if existing else uuid4()
            previous_state = None
            new_state = {
                "consent_id": str(consent_id),
                "patient_id": str(patient_id),
                "purpose": payload.purpose,
                "status": "granted",
                "legal_basis": payload.legal_basis,
                "scope": payload.scope,
                "consent_version": payload.consent_version,
                "channel": payload.channel,
                "granted_at": occurred_at.isoformat().replace("+00:00", "Z"),
                "expires_at": payload.expires_at.isoformat().replace("+00:00", "Z") if payload.expires_at else None,
                "withdrawn_at": None,
            }
            logger.info(json.dumps({"event": "db_operation", "table": "patient_consent", "operation": "INSERT"}))
            cursor.execute(
                """
                INSERT INTO patient_consent
                (consent_id, patient_id, purpose, status, legal_basis, scope, consent_version, channel, granted_at, expires_at, withdrawn_at, created_at, updated_at)
                VALUES (%s, %s, %s::varchar, %s::varchar, %s::varchar, %s::jsonb, %s, %s::varchar, %s, %s, NULL, %s, %s)
                ON CONFLICT (patient_id, purpose)
                DO UPDATE SET
                    status = EXCLUDED.status,
                    legal_basis = EXCLUDED.legal_basis,
                    scope = EXCLUDED.scope,
                    consent_version = EXCLUDED.consent_version,
                    channel = EXCLUDED.channel,
                    granted_at = EXCLUDED.granted_at,
                    expires_at = EXCLUDED.expires_at,
                    withdrawn_at = EXCLUDED.withdrawn_at,
                    updated_at = EXCLUDED.updated_at
                RETURNING consent_id
                """,
                (consent_id, patient_id, payload.purpose, "granted", payload.legal_basis, json.dumps(payload.scope), payload.consent_version, payload.channel, occurred_at, payload.expires_at, occurred_at),
            )
            row = cursor.fetchone()
            if row is None:
                conn.rollback()
                raise HTTPException(status_code=500, detail="Internal Error")
            logger.info(json.dumps({"event": "db_operation", "table": "consent_audit_log", "operation": "INSERT"}))
            record_hash = _hash_record(previous_state, new_state, event_id, actor_id, actor_role)
            cursor.execute(
                """
                INSERT INTO consent_audit_log
                (event_id, consent_id, patient_id, action, previous_state, new_state, actor_id, actor_role, source_ip, user_agent, occurred_at, record_hash)
                VALUES (%s, %s, %s, %s::varchar, %s::jsonb, %s::jsonb, %s, %s, %s, %s, %s, %s)
                RETURNING audit_id, occurred_at
                """,
                (event_id, consent_id, patient_id, "GRANT", json.dumps(previous_state), json.dumps(new_state), actor_id, actor_role, source_ip, user_agent, occurred_at, record_hash),
            )
            audit_row = cursor.fetchone()
            if audit_row is None:
                conn.rollback()
                raise HTTPException(status_code=500, detail="Internal Error")
            conn.commit()
            return ConsentGrantResponse(consent_id=consent_id, status="granted", audit_id=int(audit_row[0]), occurred_at=audit_row[1])
    except HTTPException:
        conn.rollback()
        raise
    except psycopg2.Error as exc:
        conn.rollback()
        logger.error("Database error: %s", str(exc))
        raise HTTPException(status_code=503, detail="Database Error") from exc
    except Exception as exc:
        conn.rollback()
        logger.error("Unexpected error: %s", str(exc), exc_info=True)
        raise HTTPException(status_code=500, detail="Internal Error") from exc
    finally:
        release_conn(conn)


def withdraw_consent_service(request: Request, consent_id: str, payload: ConsentWithdrawRequest, idempotency_key: str) -> ConsentWithdrawResponse:
    logger.info(json.dumps({"event": "service_entry", "operation": "withdraw_consent", "resource": "consent"}))
    actor_id, actor_role, source_ip, user_agent = _get_actor_context(request)
    consent_uuid = _parse_uuid_v4(consent_id, "consent_id")
    conn = get_conn()
    try:
        conn.rollback()
        conn.autocommit = False
        with conn.cursor() as cursor:
            logger.info(json.dumps({"event": "db_operation", "table": "patient_consent", "operation": "SELECT"}))
            cursor.execute("SELECT consent_id, patient_id, status, purpose, legal_basis, scope, consent_version, channel, granted_at, expires_at, withdrawn_at FROM patient_consent WHERE consent_id = %s", (consent_uuid,))
            row = cursor.fetchone()
            if row is None:
                raise HTTPException(status_code=404, detail="Resource Not Found")
            if row[2] == "withdrawn":
                raise HTTPException(status_code=422, detail="Validation Error")
            occurred_at = _utcnow()
            logger.info(json.dumps({"event": "db_operation", "table": "patient_consent", "operation": "UPDATE"}))
            cursor.execute(
                """
                UPDATE patient_consent
                SET status = %s::varchar,
                    withdrawn_at = %s,
                    updated_at = %s
                WHERE consent_id = %s
                RETURNING consent_id, patient_id, purpose, legal_basis, scope, consent_version, channel, granted_at, expires_at, withdrawn_at
                """,
                ("withdrawn", occurred_at, occurred_at, consent_uuid),
            )
            updated = cursor.fetchone()
            if updated is None:
                conn.rollback()
                raise HTTPException(status_code=500, detail="Internal Error")
            event_id = uuid4()
            previous_state = {
                "consent_id": str(row[0]),
                "patient_id": str(row[1]),
                "status": row[2],
            }
            new_state = {
                "consent_id": str(updated[0]),
                "patient_id": str(updated[1]),
                "status": "withdrawn",
            }
            logger.info(json.dumps({"event": "db_operation", "table": "consent_audit_log", "operation": "INSERT"}))
            record_hash = _hash_record(previous_state, new_state, event_id, actor_id, actor_role)
            cursor.execute(
                """
                INSERT INTO consent_audit_log
                (event_id, consent_id, patient_id, action, previous_state, new_state, actor_id, actor_role, source_ip, user_agent, occurred_at, record_hash)
                VALUES (%s, %s, %s, %s::varchar, %s::jsonb, %s::jsonb, %s, %s, %s, %s, %s, %s)
                RETURNING audit_id
                """,
                (event_id, consent_uuid, UUID(str(row[1])), "WITHDRAW", json.dumps(previous_state), json.dumps(new_state), actor_id, actor_role, source_ip, user_agent, occurred_at, record_hash),
            )
            audit_row = cursor.fetchone()
            if audit_row is None:
                conn.rollback()
                raise HTTPException(status_code=500, detail="Internal Error")
            conn.commit()
            return ConsentWithdrawResponse(consent_id=consent_uuid, status="withdrawn", withdrawn_at=occurred_at, audit_id=int(audit_row[0]))
    except HTTPException:
        conn.rollback()
        raise
    except psycopg2.Error as exc:
        conn.rollback()
        logger.error("Database error: %s", str(exc))
        raise HTTPException(status_code=503, detail="Database Error") from exc
    except Exception as exc:
        conn.rollback()
        logger.error("Unexpected error: %s", str(exc), exc_info=True)
        raise HTTPException(status_code=500, detail="Internal Error") from exc
    finally:
        release_conn(conn)


def get_consent_all_service(request: Request, patient_id: str) -> ConsentGetAllResponse:
    logger.info(json.dumps({"event": "service_entry", "operation": "get_consent_all", "resource": "consent"}))
    actor_id, actor_role, source_ip, user_agent = _get_actor_context(request)
    patient_uuid = _parse_uuid_v4(patient_id, "patient_id")
    conn = get_conn()
    try:
        conn.rollback()
        conn.autocommit = False
        with conn.cursor() as cursor:
            logger.info(json.dumps({"event": "db_operation", "table": "patient_consent", "operation": "SELECT"}))
            cursor.execute("SELECT consent_id, patient_id, purpose, status, legal_basis, scope, consent_version, channel, granted_at, expires_at, withdrawn_at FROM patient_consent WHERE patient_id = %s ORDER BY created_at DESC", (patient_uuid,))
            rows = cursor.fetchall()
            logger.info(json.dumps({"event": "db_operation", "table": "consent_audit_log", "operation": "INSERT"}))
            _audit_view(conn, patient_uuid, actor_id, actor_role, source_ip, user_agent)
            conn.commit()
            consents = [ConsentRecord.from_row(row) for row in rows]
            return ConsentGetAllResponse(patient_id=patient_uuid, consents=consents)
    except HTTPException:
        conn.rollback()
        raise
    except psycopg2.Error as exc:
        conn.rollback()
        logger.error("Database error: %s", str(exc))
        raise HTTPException(status_code=503, detail="Database Error") from exc
    except Exception as exc:
        conn.rollback()
        logger.error("Unexpected error: %s", str(exc), exc_info=True)
        raise HTTPException(status_code=500, detail="Internal Error") from exc
    finally:
        release_conn(conn)


def get_consent_one_service(request: Request, patient_id: str, purpose: str) -> ConsentGetOneResponse:
    logger.info(json.dumps({"event": "service_entry", "operation": "get_consent_one", "resource": "consent"}))
    actor_id, actor_role, source_ip, user_agent = _get_actor_context(request)
    patient_uuid = _parse_uuid_v4(patient_id, "patient_id")
    if purpose not in VALID_PURPOSES:
        raise HTTPException(status_code=422, detail="Validation Error")
    conn = get_conn()
    try:
        conn.rollback()
        conn.autocommit = False
        with conn.cursor() as cursor:
            logger.info(json.dumps({"event": "db_operation", "table": "patient_consent", "operation": "SELECT"}))
            cursor.execute("SELECT consent_id, patient_id, purpose, status, legal_basis, scope, consent_version, channel, granted_at, expires_at, withdrawn_at FROM patient_consent WHERE patient_id = %s AND purpose = %s", (patient_uuid, purpose))
            row = cursor.fetchone()
            if row is None:
                raise HTTPException(status_code=404, detail="Resource Not Found")
            logger.info(json.dumps({"event": "db_operation", "table": "consent_audit_log", "operation": "INSERT"}))
            _audit_view(conn, patient_uuid, actor_id, actor_role, source_ip, user_agent)
            conn.commit()
            return ConsentGetOneResponse(**ConsentRecord.from_row(row).model_dump())
    except HTTPException:
        conn.rollback()
        raise
    except psycopg2.Error as exc:
        conn.rollback()
        logger.error("Database error: %s", str(exc))
        raise HTTPException(status_code=503, detail="Database Error") from exc
    except Exception as exc:
        conn.rollback()
        logger.error("Unexpected error: %s", str(exc), exc_info=True)
        raise HTTPException(status_code=500, detail="Internal Error") from exc
    finally:
        release_conn(conn)


def get_audit_history_service(request: Request, patient_id: str, limit: int, offset: int) -> ConsentAuditHistoryResponse:
    logger.info(json.dumps({"event": "service_entry", "operation": "get_audit_history", "resource": "consent"}))
    _get_actor_context(request)
    patient_uuid = _parse_uuid_v4(patient_id, "patient_id")
    conn = get_conn()
    try:
        conn.rollback()
        conn.autocommit = False
        with conn.cursor() as cursor:
            logger.info(json.dumps({"event": "db_operation", "table": "consent_audit_log", "operation": "SELECT"}))
            cursor.execute(
                "SELECT audit_id, action, actor_id, actor_role, occurred_at FROM consent_audit_log WHERE patient_id = %s ORDER BY occurred_at DESC LIMIT %s OFFSET %s",
                (patient_uuid, limit, offset),
            )
            rows = cursor.fetchall()
            conn.commit()
            entries = [AuditRecord.from_row(row) for row in rows]
            return ConsentAuditHistoryResponse(patient_id=patient_uuid, entries=entries)
    except HTTPException:
        conn.rollback()
        raise
    except psycopg2.Error as exc:
        conn.rollback()
        logger.error("Database error: %s", str(exc))
        raise HTTPException(status_code=503, detail="Database Error") from exc
    except Exception as exc:
        conn.rollback()
        logger.error("Unexpected error: %s", str(exc), exc_info=True)
        raise HTTPException(status_code=500, detail="Internal Error") from exc
    finally:
        release_conn(conn)

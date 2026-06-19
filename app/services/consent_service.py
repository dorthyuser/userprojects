from __future__ import annotations

import hashlib
import json
import logging
import os
import uuid as _uuid
from datetime import UTC, datetime
from typing import Any
from uuid import UUID

import boto3
import psycopg2
from fastapi import HTTPException, Request

from app.db.connection import get_conn, release_conn
from app.models.consent_model import AuditRecord, ConsentRecord
from app.schemas.consent_schema import (
    ConsentAuditHistoryResponse,
    ConsentGrantRequest,
    ConsentGrantResponse,
    ConsentListResponse,
    ConsentResponse,
    ConsentWithdrawRequest,
    ConsentWithdrawResponse,
)

logger = logging.getLogger(__name__)

_ALLOWED_PURPOSES = {"treatment", "research", "marketing", "data_sharing"}
_ALLOWED_ROLES = {"patient", "clinician", "dpo", "system"}
_ALLOWED_ACTIONS = {"GRANT", "WITHDRAW", "UPDATE", "EXPIRE", "VIEW"}


def _log(message: str) -> None:
    logger.info(message)


def _utcnow() -> datetime:
    return datetime.now(UTC)


def _parse_uuid(value: str, field_name: str) -> UUID:
    try:
        # Accept any valid UUID string regardless of version. Many clients may
        # use v1 or v4; insisting on version 4 caused valid requests to fail
        # with HTTP 422. We only validate format here and return the UUID.
        parsed = UUID(value)
    except Exception:
        _log(json.dumps({"event": "validation_failed", "rule": field_name}))
        raise HTTPException(status_code=422, detail="Validation Error")
    return parsed


def _get_actor_context(request: Request) -> tuple[str, str, str, str]:
    # Actor context injected by API Gateway authorizer at runtime.
    # Falls back to x-actor-* headers (direct testing / non-authorizer invocations).
    # Defaults to 'system' role when neither source is present.
    request_context = getattr(request, "scope", {}).get("aws.event", {}).get("requestContext", {})
    headers = request.headers
    actor_id = str(
        request_context.get("authorizer", {}).get("actor_id")
        or request_context.get("actor_id")
        or headers.get("x-actor-id")
        or "system"
    )
    actor_role = str(
        request_context.get("authorizer", {}).get("actor_role")
        or request_context.get("actor_role")
        or headers.get("x-actor-role")
        or "system"
    )
    source_ip = str(
        request_context.get("identity", {}).get("sourceIp")
        or request_context.get("http", {}).get("sourceIp")
        or headers.get("x-forwarded-for")
        or ""
    )
    user_agent = str(headers.get("user-agent", ""))
    if actor_role not in _ALLOWED_ROLES:
        _log(json.dumps({"event": "validation_failed", "rule": "actor_role", "value": actor_role}))
        raise HTTPException(status_code=422, detail="Validation Error")
    return actor_id, actor_role, source_ip, user_agent


def _sns_publish(message: dict[str, Any]) -> None:
    topic_arn = os.getenv("CONSENT_SNS_TOPIC_ARN")
    if not topic_arn:
        return
    region = os.getenv("AWS_REGION", "eu-west-2")
    client = boto3.client("sns", region_name=region)
    client.publish(TopicArn=topic_arn, Message=json.dumps(message, default=str))


def _hash_event(idempotency_key: str, action: str) -> str:
    """
    Produce a deterministic UUID for an idempotency/event key.

    The database expects event_id as a UUID. To maintain idempotency while
    keeping values valid for a uuid column, we generate a UUIDv5 using a
    well-known namespace and the composite name "{idempotency_key}:{action}".

    If for any reason UUID generation fails, fall back to a hex digest. The
    fallback will likely fail DB uuid constraints, but the primary path should
    always work for normal inputs.
    """
    try:
        name = f"{idempotency_key}:{action}"
        return str(_uuid.uuid5(_uuid.NAMESPACE_URL, name))
    except Exception:
        # Last-resort fallback: deterministic hash string (may not fit uuid column)
        return hashlib.sha256(f"{idempotency_key}:{action}".encode("utf-8")).hexdigest()


def _audit_row_to_model(row: tuple[Any, ...]) -> AuditRecord:
    return AuditRecord(
        audit_id=row[0],
        action=row[1],
        actor_id=row[2],
        actor_role=row[3],
        occurred_at=row[4],
    )


def _consent_row_to_model(row: tuple[Any, ...]) -> ConsentRecord:
    return ConsentRecord(
        consent_id=row[0],
        patient_id=row[1],
        purpose=row[2],
        status=row[3],
        legal_basis=row[4],
        scope=row[5],
        consent_version=row[6],
        channel=row[7],
        granted_at=row[8],
        expires_at=row[9],
        withdrawn_at=row[10],
    )


def grant_consent_service(request: Request, payload: ConsentGrantRequest, idempotency_key: str) -> ConsentGrantResponse:
    _log(json.dumps({"event": "service_start", "operation": "grant_consent", "resource": "consent"}))
    actor_id, actor_role, source_ip, user_agent = _get_actor_context(request)
    conn = get_conn()
    try:
        conn.rollback()
        conn.autocommit = False
        with conn.cursor() as cursor:
            _log(json.dumps({"event": "db_operation", "table": "patient_consent", "operation": "SELECT"}))
            cursor.execute(
                "SELECT consent_id, patient_id, purpose, status, legal_basis, scope, consent_version, channel, granted_at, expires_at, withdrawn_at FROM patient_consent WHERE patient_id = %s AND purpose = %s",
                (str(payload.patient_id), payload.purpose),
            )
            existing = cursor.fetchone()
            event_id = _hash_event(idempotency_key, "GRANT")
            _log(json.dumps({"event": "db_operation", "table": "consent_audit_log", "operation": "SELECT"}))
            cursor.execute("SELECT audit_id, new_state FROM consent_audit_log WHERE event_id = %s ORDER BY audit_id DESC LIMIT 1", (event_id,))
            replay = cursor.fetchone()
            if replay is not None:
                conn.rollback()
                state = replay[1]
                return ConsentGrantResponse(**state)
            now = _utcnow()
            if existing is None:
                _log(json.dumps({"event": "db_operation", "table": "patient_consent", "operation": "INSERT"}))
                cursor.execute(
                    "INSERT INTO patient_consent (patient_id, purpose, status, legal_basis, scope, consent_version, channel, granted_at, expires_at, withdrawn_at, created_at, updated_at) VALUES (%s, %s::varchar, %s::varchar, %s::varchar, %s::jsonb, %s, %s::varchar, %s, %s, %s, %s, %s) RETURNING consent_id",
                    (
                        str(payload.patient_id),
                        payload.purpose,
                        "granted",
                        payload.legal_basis,
                        json.dumps(payload.scope),
                        payload.consent_version,
                        payload.channel,
                        now,
                        payload.expires_at,
                        None,
                        now,
                        now,
                    ),
                )
                row = cursor.fetchone()
                if row is None:
                    conn.rollback()
                    raise HTTPException(status_code=500, detail="Internal Error")
                consent_id = row[0]
            else:
                consent_id = existing[0]
                _log(json.dumps({"event": "db_operation", "table": "patient_consent", "operation": "UPDATE"}))
                cursor.execute(
                    "UPDATE patient_consent SET status = %s::varchar, legal_basis = %s::varchar, scope = %s::jsonb, consent_version = %s, channel = %s::varchar, granted_at = %s, expires_at = %s, withdrawn_at = %s, updated_at = %s WHERE consent_id = %s RETURNING consent_id",
                    (
                        "granted",
                        payload.legal_basis,
                        json.dumps(payload.scope),
                        payload.consent_version,
                        payload.channel,
                        now,
                        payload.expires_at,
                        None,
                        now,
                        consent_id,
                    ),
                )
                row = cursor.fetchone()
                if row is None:
                    conn.rollback()
                    raise HTTPException(status_code=500, detail="Internal Error")
            new_state = {
                "consent_id": str(consent_id),
                "status": "granted",
                "audit_id": None,
                "occurred_at": now.isoformat().replace("+00:00", "Z"),
            }
            _log(json.dumps({"event": "db_operation", "table": "consent_audit_log", "operation": "INSERT"}))
            cursor.execute(
                "INSERT INTO consent_audit_log (event_id, consent_id, patient_id, action, previous_state, new_state, actor_id, actor_role, source_ip, user_agent, occurred_at, record_hash) VALUES (%s, %s, %s, %s::varchar, %s::jsonb, %s::jsonb, %s, %s, %s, %s, %s, %s) RETURNING audit_id",
                (
                    event_id,
                    consent_id,
                    str(payload.patient_id),
                    "GRANT",
                    json.dumps(existing[0] if existing else None, default=str),
                    json.dumps(new_state),
                    actor_id,
                    actor_role,
                    source_ip,
                    user_agent,
                    now,
                    hashlib.sha256(f"{event_id}:{consent_id}:{now.isoformat()}".encode("utf-8")).hexdigest(),
                ),
            )
            audit_row = cursor.fetchone()
            if audit_row is None:
                conn.rollback()
                raise HTTPException(status_code=500, detail="Internal Error")
            audit_id = audit_row[0]
            conn.commit()
            response = ConsentGrantResponse(consent_id=consent_id, status="granted", audit_id=audit_id, occurred_at=now)
            _sns_publish({"event": "CONSENT_GRANTED", "consent_id": str(consent_id), "patient_id": str(payload.patient_id)})
            return response
    except psycopg2.Error as exc:
        conn.rollback()
        logger.error("Database error: %s", str(exc), exc_info=True)
        raise HTTPException(status_code=503, detail="Database Error")
    except HTTPException:
        raise
    except Exception as exc:
        conn.rollback()
        logger.error("Unexpected error: %s", str(exc), exc_info=True)
        raise HTTPException(status_code=500, detail="Internal Error")
    finally:
        release_conn(conn)


def withdraw_consent_service(request: Request, consent_id: str, payload: ConsentWithdrawRequest, idempotency_key: str) -> ConsentWithdrawResponse:
    _log(json.dumps({"event": "service_start", "operation": "withdraw_consent", "resource": "consent"}))
    actor_id, actor_role, source_ip, user_agent = _get_actor_context(request)
    consent_uuid = _parse_uuid(consent_id, "consent_id")
    conn = get_conn()
    try:
        conn.rollback()
        conn.autocommit = False
        with conn.cursor() as cursor:
            event_id = _hash_event(idempotency_key, "WITHDRAW")
            _log(json.dumps({"event": "db_operation", "table": "consent_audit_log", "operation": "SELECT"}))
            cursor.execute("SELECT audit_id, new_state FROM consent_audit_log WHERE event_id = %s ORDER BY audit_id DESC LIMIT 1", (event_id,))
            replay = cursor.fetchone()
            if replay is not None:
                conn.rollback()
                state = replay[1]
                return ConsentWithdrawResponse(**state)
            _log(json.dumps({"event": "db_operation", "table": "patient_consent", "operation": "SELECT"}))
            cursor.execute(
                "SELECT consent_id, patient_id, purpose, status, legal_basis, scope, consent_version, channel, granted_at, expires_at, withdrawn_at FROM patient_consent WHERE consent_id = %s",
                (str(consent_uuid),),
            )
            row = cursor.fetchone()
            if row is None:
                conn.rollback()
                raise HTTPException(status_code=404, detail="Resource Not Found")
            if row[3] == "withdrawn":
                conn.rollback()
                raise HTTPException(status_code=422, detail="Validation Error")
            now = _utcnow()
            _log(json.dumps({"event": "db_operation", "table": "patient_consent", "operation": "UPDATE"}))
            cursor.execute(
                "UPDATE patient_consent SET status = %s::varchar, withdrawn_at = %s, updated_at = %s WHERE consent_id = %s RETURNING consent_id",
                ("withdrawn", now, now, str(consent_uuid)),
            )
            updated = cursor.fetchone()
            if updated is None:
                conn.rollback()
                raise HTTPException(status_code=500, detail="Internal Error")
            new_state = {"consent_id": str(consent_uuid), "status": "withdrawn", "withdrawn_at": now.isoformat().replace("+00:00", "Z"), "audit_id": None}
            _log(json.dumps({"event": "db_operation", "table": "consent_audit_log", "operation": "INSERT"}))
            cursor.execute(
                "INSERT INTO consent_audit_log (event_id, consent_id, patient_id, action, previous_state, new_state, actor_id, actor_role, source_ip, user_agent, occurred_at, record_hash) VALUES (%s, %s, %s, %s::varchar, %s::jsonb, %s::jsonb, %s, %s, %s, %s, %s, %s) RETURNING audit_id",
                (
                    event_id,
                    str(consent_uuid),
                    row[1],
                    "WITHDRAW",
                    json.dumps({"status": row[3]}, default=str),
                    json.dumps(new_state),
                    actor_id,
                    actor_role,
                    source_ip,
                    user_agent,
                    now,
                    hashlib.sha256(f"{event_id}:{consent_uuid}:{now.isoformat()}".encode("utf-8")).hexdigest(),
                ),
            )
            audit_row = cursor.fetchone()
            if audit_row is None:
                conn.rollback()
                raise HTTPException(status_code=500, detail="Internal Error")
            audit_id = audit_row[0]
            conn.commit()
            response = ConsentWithdrawResponse(consent_id=consent_uuid, status="withdrawn", withdrawn_at=now, audit_id=audit_id)
            _sns_publish({"event": "CONSENT_WITHDRAWN", "consent_id": str(consent_uuid), "patient_id": str(row[1])})
            return response
    except psycopg2.Error as exc:
        conn.rollback()
        logger.error("Database error: %s", str(exc), exc_info=True)
        raise HTTPException(status_code=503, detail="Database Error")
    except HTTPException:
        raise
    except Exception as exc:
        conn.rollback()
        logger.error("Unexpected error: %s", str(exc), exc_info=True)
        raise HTTPException(status_code=500, detail="Internal Error")
    finally:
        release_conn(conn)


def get_consent_all_service(request: Request, patient_id: str) -> ConsentListResponse:
    _log(json.dumps({"event": "service_start", "operation": "get_consent_all", "resource": "consent"}))
    actor_id, actor_role, source_ip, user_agent = _get_actor_context(request)
    patient_uuid = _parse_uuid(patient_id, "patient_id")
    conn = get_conn()
    try:
        conn.rollback()
        conn.autocommit = False
        with conn.cursor() as cursor:
            _log(json.dumps({"event": "db_operation", "table": "patient_consent", "operation": "SELECT"}))
            cursor.execute(
                "SELECT consent_id, patient_id, purpose, status, legal_basis, scope, consent_version, channel, granted_at, expires_at, withdrawn_at FROM patient_consent WHERE patient_id = %s ORDER BY created_at DESC",
                (str(patient_uuid),),
            )
            rows = cursor.fetchall()
            _log(json.dumps({"event": "db_operation", "table": "consent_audit_log", "operation": "INSERT"}))
            now = _utcnow()
            cursor.execute(
                "INSERT INTO consent_audit_log (event_id, consent_id, patient_id, action, previous_state, new_state, actor_id, actor_role, source_ip, user_agent, occurred_at, record_hash) VALUES (%s, %s, %s, %s::varchar, %s::jsonb, %s::jsonb, %s, %s, %s, %s, %s, %s)",
                (
                    _hash_event(str(patient_uuid), "VIEW_ALL"),
                    None,
                    str(patient_uuid),
                    "VIEW",
                    None,
                    None,
                    actor_id,
                    actor_role,
                    source_ip,
                    user_agent,
                    now,
                    hashlib.sha256(f"view:{patient_uuid}:{now.isoformat()}".encode("utf-8")).hexdigest(),
                ),
            )
            conn.commit()
            return ConsentListResponse(patient_id=patient_uuid, consents=[_consent_row_to_model(row) for row in rows])
    except psycopg2.Error as exc:
        conn.rollback()
        logger.error("Database error: %s", str(exc), exc_info=True)
        raise HTTPException(status_code=503, detail="Database Error")
    except HTTPException:
        raise
    except Exception as exc:
        conn.rollback()
        logger.error("Unexpected error: %s", str(exc), exc_info=True)
        raise HTTPException(status_code=500, detail="Internal Error")
    finally:
        release_conn(conn)


def get_consent_one_service(request: Request, patient_id: str, purpose: str) -> ConsentResponse:
    _log(json.dumps({"event": "service_start", "operation": "get_consent_one", "resource": "consent"}))
    actor_id, actor_role, source_ip, user_agent = _get_actor_context(request)
    patient_uuid = _parse_uuid(patient_id, "patient_id")
    if purpose not in _ALLOWED_PURPOSES:
        _log(json.dumps({"event": "validation_failed", "rule": "purpose"}))
        raise HTTPException(status_code=422, detail="Validation Error")
    conn = get_conn()
    try:
        conn.rollback()
        conn.autocommit = False
        with conn.cursor() as cursor:
            _log(json.dumps({"event": "db_operation", "table": "patient_consent", "operation": "SELECT"}))
            cursor.execute(
                "SELECT consent_id, patient_id, purpose, status, legal_basis, scope, consent_version, channel, granted_at, expires_at, withdrawn_at FROM patient_consent WHERE patient_id = %s AND purpose = %s",
                (str(patient_uuid), purpose),
            )
            row = cursor.fetchone()
            if row is None:
                conn.rollback()
                raise HTTPException(status_code=404, detail="Resource Not Found")
            _log(json.dumps({"event": "db_operation", "table": "consent_audit_log", "operation": "INSERT"}))
            now = _utcnow()
            cursor.execute(
                "INSERT INTO consent_audit_log (event_id, consent_id, patient_id, action, previous_state, new_state, actor_id, actor_role, source_ip, user_agent, occurred_at, record_hash) VALUES (%s, %s, %s, %s::varchar, %s::jsonb, %s::jsonb, %s, %s, %s, %s, %s, %s)",
                (
                    _hash_event(f"{patient_uuid}:{purpose}", "VIEW_ONE"),
                    row[0],
                    str(patient_uuid),
                    "VIEW",
                    None,
                    None,
                    actor_id,
                    actor_role,
                    source_ip,
                    user_agent,
                    now,
                    hashlib.sha256(f"view:{patient_uuid}:{purpose}:{now.isoformat()}".encode("utf-8")).hexdigest(),
                ),
            )
            conn.commit()
            return ConsentResponse(**_consent_row_to_model(row).__dict__)
    except psycopg2.Error as exc:
        conn.rollback()
        logger.error("Database error: %s", str(exc), exc_info=True)
        raise HTTPException(status_code=503, detail="Database Error")
    except HTTPException:
        raise
    except Exception as exc:
        conn.rollback()
        logger.error("Unexpected error: %s", str(exc), exc_info=True)
        raise HTTPException(status_code=500, detail="Internal Error")
    finally:
        release_conn(conn)


def get_audit_history_service(request: Request, patient_id: str, limit: int, offset: int) -> ConsentAuditHistoryResponse:
    _log(json.dumps({"event": "service_start", "operation": "get_audit_history", "resource": "consent"}))
    actor_id, actor_role, source_ip, user_agent = _get_actor_context(request)
    patient_uuid = _parse_uuid(patient_id, "patient_id")
    conn = get_conn()
    try:
        conn.rollback()
        conn.autocommit = False
        with conn.cursor() as cursor:
            _log(json.dumps({"event": "db_operation", "table": "consent_audit_log", "operation": "SELECT"}))
            cursor.execute(
                "SELECT audit_id, action, actor_id, actor_role, occurred_at FROM consent_audit_log WHERE patient_id = %s ORDER BY occurred_at DESC LIMIT %s OFFSET %s",
                (str(patient_uuid), limit, offset),
            )
            rows = cursor.fetchall()
            now = _utcnow()
            cursor.execute(
                "INSERT INTO consent_audit_log (event_id, consent_id, patient_id, action, previous_state, new_state, actor_id, actor_role, source_ip, user_agent, occurred_at, record_hash) VALUES (%s, %s, %s, %s::varchar, %s::jsonb, %s::jsonb, %s, %s, %s, %s, %s, %s)",
                (
                    _hash_event(f"{patient_uuid}:audit", "VIEW_HISTORY"),
                    None,
                    str(patient_uuid),
                    "VIEW",
                    None,
                    None,
                    actor_id,
                    actor_role,
                    source_ip,
                    user_agent,
                    now,
                    hashlib.sha256(f"audit:{patient_uuid}:{now.isoformat()}".encode("utf-8")).hexdigest(),
                ),
            )
            conn.commit()
            return ConsentAuditHistoryResponse(patient_id=patient_uuid, entries=[_audit_row_to_model(row) for row in rows])
    except psycopg2.Error as exc:
        conn.rollback()
        logger.error("Database error: %s", str(exc), exc_info=True)
        raise HTTPException(status_code=503, detail="Database Error")
    except HTTPException:
        raise
    except Exception as exc:
        conn.rollback()
        logger.error("Unexpected error: %s", str(exc), exc_info=True)
        raise HTTPException(status_code=500, detail="Internal Error")
    finally:
        release_conn(conn)

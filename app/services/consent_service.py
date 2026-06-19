from __future__ import annotations

import hashlib
import json
import logging
import os
from datetime import UTC, datetime
from typing import Any
from uuid import UUID, uuid4

import psycopg2
from fastapi import HTTPException
from psycopg2.extras import Json

from app.db.connection import get_conn, release_conn
from app.models.consent_model import AuditRecord, ConsentRecord
from app.schemas.consent_schema import (
    AuditRecordResponse,
    ConsentAuditHistoryResponse,
    ConsentGrantRequest,
    ConsentGrantResponse,
    ConsentRecordResponse,
    ConsentStateResponse,
    ConsentWithdrawRequest,
    ConsentWithdrawResponse,
)

logger = logging.getLogger(__name__)

_ALLOWED_PURPOSES = {"treatment", "research", "marketing", "data_sharing"}
_ALLOWED_ROLES = {"patient", "clinician", "dpo", "system"}
_ALLOWED_ACTIONS = {"GRANT", "WITHDRAW", "UPDATE", "EXPIRE", "VIEW"}


def _utcnow() -> datetime:
    return datetime.now(UTC)


def _parse_uuid_v4(value: str, field_name: str) -> UUID:
    try:
        parsed = UUID(value)
    except Exception:
        logger.info(json.dumps({"event": "validation_failure", "rule": field_name}))
        raise HTTPException(status_code=422, detail="Validation Error")
    if parsed.version != 4:
        logger.info(json.dumps({"event": "validation_failure", "rule": field_name}))
        raise HTTPException(status_code=422, detail="Validation Error")
    return parsed


def _require_header(headers: dict[str, Any], name: str) -> str:
    value = headers.get(name) or headers.get(name.lower())
    if not isinstance(value, str) or not value.strip():
        logger.info(json.dumps({"event": "validation_failure", "rule": f"missing_{name}"}))
        raise HTTPException(status_code=422, detail="Validation Error")
    return value.strip()


def _validate_idempotency_key(headers: dict[str, Any]) -> str:
    key = _require_header(headers, "Idempotency-Key")
    if len(key) > 128:
        logger.info(json.dumps({"event": "validation_failure", "rule": "idempotency_key_length"}))
        raise HTTPException(status_code=422, detail="Validation Error")
    return key


def _actor_context(headers: dict[str, Any], event: dict[str, Any]) -> tuple[str, str, str | None, str | None]:
    actor_id = str(event.get("requestContext", {}).get("authorizer", {}).get("actor_id") or headers.get("x-actor-id") or "system")
    actor_role = str(event.get("requestContext", {}).get("authorizer", {}).get("actor_role") or headers.get("x-actor-role") or "system")
    source_ip = event.get("requestContext", {}).get("identity", {}).get("sourceIp")
    user_agent = headers.get("user-agent") or headers.get("User-Agent")
    if actor_role not in _ALLOWED_ROLES:
        logger.info(json.dumps({"event": "validation_failure", "rule": "actor_role"}))
        raise HTTPException(status_code=422, detail="Validation Error")
    return actor_id, actor_role, source_ip, user_agent


def _hash_chain(previous_hash: str | None, payload: dict[str, Any]) -> str:
    digest = hashlib.sha256()
    digest.update((previous_hash or "").encode("utf-8"))
    digest.update(json.dumps(payload, sort_keys=True, default=str).encode("utf-8"))
    return digest.hexdigest()


def _row_to_consent(row: tuple[Any, ...]) -> ConsentRecord:
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


def _row_to_audit(row: tuple[Any, ...]) -> AuditRecord:
    return AuditRecord(
        audit_id=row[0],
        action=row[1],
        actor_id=row[2],
        actor_role=row[3],
        occurred_at=row[4],
    )


def _publish_sns(event_name: str, payload: dict[str, Any]) -> None:
    topic_arn = os.environ.get("CONSENT_SNS_TOPIC_ARN")
    if not topic_arn:
        return
    import boto3

    client = boto3.client("sns", region_name=os.environ.get("AWS_REGION", "eu-west-2"))
    client.publish(TopicArn=topic_arn, Message=json.dumps({"event": event_name, "payload": payload}, default=str))


def grant_consent_service(payload: ConsentGrantRequest, headers: dict[str, Any], event: dict[str, Any]) -> ConsentGrantResponse:
    logger.info(json.dumps({"event": "service_entry", "operation": "grant_consent", "resource": "consent"}))
    idempotency_key = _validate_idempotency_key(headers)
    actor_id, actor_role, source_ip, user_agent = _actor_context(headers, event)
    consent_id = uuid4()
    event_id = _parse_uuid_v4(idempotency_key, "Idempotency-Key")
    conn = get_conn()
    try:
        conn.rollback()
        conn.autocommit = False
        with conn.cursor() as cursor:
            logger.info(json.dumps({"event": "db_operation", "table": "patient_consent", "operation": "INSERT"}))
            cursor.execute(
                "SELECT response_json FROM idempotency_keys WHERE event_id = %s",
                (event_id,)
            )
            existing = cursor.fetchone()
            if existing is not None:
                raise HTTPException(status_code=409, detail="IDEMPOTENCY_CONFLICT")
            cursor.execute(
                "SELECT consent_id FROM patient_consent WHERE patient_id = %s AND purpose = %s",
                (payload.patient_id, payload.purpose)
            )
            existing_consent = cursor.fetchone()
            if existing_consent is not None:
                raise HTTPException(status_code=409, detail="IDEMPOTENCY_CONFLICT")
            now = _utcnow()
            cursor.execute(
                "INSERT INTO patient_consent (consent_id, patient_id, purpose, status, legal_basis, scope, consent_version, channel, granted_at, expires_at, withdrawn_at, created_at, updated_at) VALUES (%s, %s, %s::varchar, %s::varchar, %s::varchar, %s::jsonb, %s, %s::varchar, %s, %s, %s, %s, %s) RETURNING consent_id",
                (consent_id, payload.patient_id, payload.purpose, "granted", payload.legal_basis, Json(payload.scope), payload.consent_version, payload.channel, now, payload.expires_at, None, now, now)
            )
            returned = cursor.fetchone()
            if returned is None:
                conn.rollback()
                raise HTTPException(status_code=500, detail="Internal Error")
            cursor.execute(
                "SELECT audit_id, record_hash FROM consent_audit_log WHERE patient_id = %s ORDER BY audit_id DESC LIMIT 1",
                (payload.patient_id,)
            )
            prev = cursor.fetchone()
            previous_hash = prev[1] if prev else None
            record_hash = _hash_chain(previous_hash, {"action": "GRANT", "consent_id": str(consent_id), "patient_id": str(payload.patient_id)})
            logger.info(json.dumps({"event": "db_operation", "table": "consent_audit_log", "operation": "INSERT"}))
            cursor.execute(
                "INSERT INTO consent_audit_log (event_id, consent_id, patient_id, action, previous_state, new_state, actor_id, actor_role, source_ip, user_agent, occurred_at, record_hash) VALUES (%s, %s, %s, %s::varchar, %s::jsonb, %s::jsonb, %s, %s::varchar, %s, %s, %s, %s) RETURNING audit_id, occurred_at",
                (event_id, consent_id, payload.patient_id, "GRANT", None, Json({"consent_id": str(consent_id), "status": "granted"}), actor_id, actor_role, source_ip, user_agent, now, record_hash)
            )
            audit_row = cursor.fetchone()
            if audit_row is None:
                conn.rollback()
                raise HTTPException(status_code=500, detail="Internal Error")
            cursor.execute(
                "INSERT INTO idempotency_keys (event_id, response_json, created_at) VALUES (%s, %s::jsonb, %s)",
                (event_id, Json({"consent_id": str(consent_id), "status": "granted", "audit_id": audit_row[0], "occurred_at": audit_row[1].isoformat()}), now)
            )
        conn.commit()
        _publish_sns("CONSENT_GRANTED", {"consent_id": str(consent_id), "patient_id": str(payload.patient_id), "action": "GRANT"})
        return ConsentGrantResponse(consent_id=consent_id, status="granted", audit_id=audit_row[0], occurred_at=audit_row[1])
    except HTTPException:
        conn.rollback()
        raise
    except psycopg2.Error as e:
        conn.rollback()
        logger.error("Database error: %s", str(e))
        raise HTTPException(status_code=503, detail="Database Error")
    except Exception as e:
        conn.rollback()
        logger.error("Unexpected error: %s", str(e), exc_info=True)
        raise HTTPException(status_code=500, detail="Internal Error")
    finally:
        release_conn(conn)


def withdraw_consent_service(consent_id: str, payload: ConsentWithdrawRequest, headers: dict[str, Any], event: dict[str, Any]) -> ConsentWithdrawResponse:
    logger.info(json.dumps({"event": "service_entry", "operation": "withdraw_consent", "resource": "consent"}))
    idempotency_key = _validate_idempotency_key(headers)
    actor_id, actor_role, source_ip, user_agent = _actor_context(headers, event)
    consent_uuid = _parse_uuid_v4(consent_id, "consent_id")
    event_id = _parse_uuid_v4(idempotency_key, "Idempotency-Key")
    conn = get_conn()
    try:
        conn.rollback()
        conn.autocommit = False
        with conn.cursor() as cursor:
            logger.info(json.dumps({"event": "db_operation", "table": "patient_consent", "operation": "SELECT"}))
            cursor.execute(
                "SELECT consent_id, patient_id, purpose, status, legal_basis, scope, consent_version, channel, granted_at, expires_at, withdrawn_at FROM patient_consent WHERE consent_id = %s",
                (consent_uuid,)
            )
            row = cursor.fetchone()
            if row is None:
                raise HTTPException(status_code=404, detail="Resource Not Found")
            consent = _row_to_consent(row)
            if consent.status == "withdrawn":
                raise HTTPException(status_code=409, detail="ALREADY_WITHDRAWN")
            now = _utcnow()
            logger.info(json.dumps({"event": "db_operation", "table": "patient_consent", "operation": "UPDATE"}))
            cursor.execute(
                "UPDATE patient_consent SET status = %s::varchar, withdrawn_at = %s, updated_at = %s WHERE consent_id = %s",
                ("withdrawn", now, now, consent_uuid)
            )
            cursor.execute(
                "SELECT record_hash FROM consent_audit_log WHERE patient_id = %s ORDER BY audit_id DESC LIMIT 1",
                (consent.patient_id,)
            )
            prev = cursor.fetchone()
            record_hash = _hash_chain(prev[0] if prev else None, {"action": "WITHDRAW", "consent_id": str(consent_uuid), "patient_id": str(consent.patient_id)})
            logger.info(json.dumps({"event": "db_operation", "table": "consent_audit_log", "operation": "INSERT"}))
            cursor.execute(
                "INSERT INTO consent_audit_log (event_id, consent_id, patient_id, action, previous_state, new_state, actor_id, actor_role, source_ip, user_agent, occurred_at, record_hash) VALUES (%s, %s, %s, %s::varchar, %s::jsonb, %s::jsonb, %s, %s::varchar, %s, %s, %s, %s) RETURNING audit_id",
                (event_id, consent_uuid, consent.patient_id, "WITHDRAW", Json({"status": consent.status}), Json({"status": "withdrawn"}), actor_id, actor_role, source_ip, user_agent, now, record_hash)
            )
            audit_row = cursor.fetchone()
            if audit_row is None:
                conn.rollback()
                raise HTTPException(status_code=500, detail="Internal Error")
            cursor.execute(
                "INSERT INTO idempotency_keys (event_id, response_json, created_at) VALUES (%s, %s::jsonb, %s)",
                (event_id, Json({"consent_id": str(consent_uuid), "status": "withdrawn", "withdrawn_at": now.isoformat(), "audit_id": audit_row[0]}), now)
            )
        conn.commit()
        _publish_sns("CONSENT_WITHDRAWN", {"consent_id": str(consent_uuid), "patient_id": str(consent.patient_id), "action": "WITHDRAW"})
        return ConsentWithdrawResponse(consent_id=consent_uuid, status="withdrawn", withdrawn_at=now, audit_id=audit_row[0])
    except HTTPException:
        conn.rollback()
        raise
    except psycopg2.Error as e:
        conn.rollback()
        logger.error("Database error: %s", str(e))
        raise HTTPException(status_code=503, detail="Database Error")
    except Exception as e:
        conn.rollback()
        logger.error("Unexpected error: %s", str(e), exc_info=True)
        raise HTTPException(status_code=500, detail="Internal Error")
    finally:
        release_conn(conn)


def get_consent_all_service(patient_id: str, headers: dict[str, Any], event: dict[str, Any]) -> ConsentStateResponse:
    logger.info(json.dumps({"event": "service_entry", "operation": "get_consent_all", "resource": "consent"}))
    actor_id, actor_role, source_ip, user_agent = _actor_context(headers, event)
    patient_uuid = _parse_uuid_v4(patient_id, "patient_id")
    conn = get_conn()
    try:
        conn.rollback()
        conn.autocommit = False
        with conn.cursor() as cursor:
            logger.info(json.dumps({"event": "db_operation", "table": "patient_consent", "operation": "SELECT"}))
            cursor.execute(
                "SELECT consent_id, patient_id, purpose, status, legal_basis, scope, consent_version, channel, granted_at, expires_at, withdrawn_at FROM patient_consent WHERE patient_id = %s ORDER BY granted_at DESC",
                (patient_uuid,)
            )
            rows = cursor.fetchall()
            consents = [_row_to_consent(row) for row in rows]
            logger.info(json.dumps({"event": "db_operation", "table": "consent_audit_log", "operation": "INSERT"}))
            cursor.execute(
                "SELECT record_hash FROM consent_audit_log WHERE patient_id = %s ORDER BY audit_id DESC LIMIT 1",
                (patient_uuid,)
            )
            prev = cursor.fetchone()
            record_hash = _hash_chain(prev[0] if prev else None, {"action": "VIEW", "patient_id": str(patient_uuid)})
            cursor.execute(
                "INSERT INTO consent_audit_log (event_id, consent_id, patient_id, action, previous_state, new_state, actor_id, actor_role, source_ip, user_agent, occurred_at, record_hash) VALUES (%s, %s, %s, %s::varchar, %s::jsonb, %s::jsonb, %s, %s::varchar, %s, %s, %s, %s) RETURNING audit_id",
                (uuid4(), None, patient_uuid, "VIEW", None, None, actor_id, actor_role, source_ip, user_agent, _utcnow(), record_hash)
            )
        conn.commit()
        return ConsentStateResponse(patient_id=patient_uuid, consents=[ConsentRecordResponse.model_validate(consent.__dict__) for consent in consents])
    except HTTPException:
        conn.rollback()
        raise
    except psycopg2.Error as e:
        conn.rollback()
        logger.error("Database error: %s", str(e))
        raise HTTPException(status_code=503, detail="Database Error")
    except Exception as e:
        conn.rollback()
        logger.error("Unexpected error: %s", str(e), exc_info=True)
        raise HTTPException(status_code=500, detail="Internal Error")
    finally:
        release_conn(conn)


def get_consent_one_service(patient_id: str, purpose: str, headers: dict[str, Any], event: dict[str, Any]) -> ConsentRecordResponse:
    logger.info(json.dumps({"event": "service_entry", "operation": "get_consent_one", "resource": "consent"}))
    actor_id, actor_role, source_ip, user_agent = _actor_context(headers, event)
    patient_uuid = _parse_uuid_v4(patient_id, "patient_id")
    if purpose not in _ALLOWED_PURPOSES:
        logger.info(json.dumps({"event": "validation_failure", "rule": "purpose"}))
        raise HTTPException(status_code=422, detail="Validation Error")
    conn = get_conn()
    try:
        conn.rollback()
        conn.autocommit = False
        with conn.cursor() as cursor:
            logger.info(json.dumps({"event": "db_operation", "table": "patient_consent", "operation": "SELECT"}))
            cursor.execute(
                "SELECT consent_id, patient_id, purpose, status, legal_basis, scope, consent_version, channel, granted_at, expires_at, withdrawn_at FROM patient_consent WHERE patient_id = %s AND purpose = %s",
                (patient_uuid, purpose)
            )
            row = cursor.fetchone()
            if row is None:
                raise HTTPException(status_code=404, detail="Resource Not Found")
            consent = _row_to_consent(row)
            logger.info(json.dumps({"event": "db_operation", "table": "consent_audit_log", "operation": "INSERT"}))
            cursor.execute(
                "INSERT INTO consent_audit_log (event_id, consent_id, patient_id, action, previous_state, new_state, actor_id, actor_role, source_ip, user_agent, occurred_at, record_hash) VALUES (%s, %s, %s, %s::varchar, %s::jsonb, %s::jsonb, %s, %s::varchar, %s, %s, %s, %s) RETURNING audit_id",
                (uuid4(), consent.consent_id, patient_uuid, "VIEW", None, None, actor_id, actor_role, source_ip, user_agent, _utcnow(), _hash_chain(None, {"action": "VIEW", "consent_id": str(consent.consent_id)}))
            )
        conn.commit()
        return ConsentRecordResponse.model_validate(consent.__dict__)
    except HTTPException:
        conn.rollback()
        raise
    except psycopg2.Error as e:
        conn.rollback()
        logger.error("Database error: %s", str(e))
        raise HTTPException(status_code=503, detail="Database Error")
    except Exception as e:
        conn.rollback()
        logger.error("Unexpected error: %s", str(e), exc_info=True)
        raise HTTPException(status_code=500, detail="Internal Error")
    finally:
        release_conn(conn)


def get_audit_history_service(patient_id: str, limit: int, offset: int, headers: dict[str, Any], event: dict[str, Any]) -> ConsentAuditHistoryResponse:
    logger.info(json.dumps({"event": "service_entry", "operation": "get_audit_history", "resource": "consent"}))
    actor_id, actor_role, source_ip, user_agent = _actor_context(headers, event)
    patient_uuid = _parse_uuid_v4(patient_id, "patient_id")
    conn = get_conn()
    try:
        conn.rollback()
        conn.autocommit = False
        with conn.cursor() as cursor:
            logger.info(json.dumps({"event": "db_operation", "table": "consent_audit_log", "operation": "SELECT"}))
            cursor.execute(
                "SELECT audit_id, action, actor_id, actor_role, occurred_at FROM consent_audit_log WHERE patient_id = %s ORDER BY occurred_at DESC LIMIT %s OFFSET %s",
                (patient_uuid, limit, offset)
            )
            rows = cursor.fetchall()
            entries = [_row_to_audit(row) for row in rows]
            logger.info(json.dumps({"event": "db_operation", "table": "consent_audit_log", "operation": "INSERT"}))
            cursor.execute(
                "INSERT INTO consent_audit_log (event_id, consent_id, patient_id, action, previous_state, new_state, actor_id, actor_role, source_ip, user_agent, occurred_at, record_hash) VALUES (%s, %s, %s, %s::varchar, %s::jsonb, %s::jsonb, %s, %s::varchar, %s, %s, %s, %s) RETURNING audit_id",
                (uuid4(), None, patient_uuid, "VIEW", None, None, actor_id, actor_role, source_ip, user_agent, _utcnow(), _hash_chain(None, {"action": "VIEW", "patient_id": str(patient_uuid), "audit": True}))
            )
        conn.commit()
        return ConsentAuditHistoryResponse(patient_id=patient_uuid, entries=[AuditRecordResponse.model_validate(entry.__dict__) for entry in entries])
    except HTTPException:
        conn.rollback()
        raise
    except psycopg2.Error as e:
        conn.rollback()
        logger.error("Database error: %s", str(e))
        raise HTTPException(status_code=503, detail="Database Error")
    except Exception as e:
        conn.rollback()
        logger.error("Unexpected error: %s", str(e), exc_info=True)
        raise HTTPException(status_code=500, detail="Internal Error")
    finally:
        release_conn(conn)

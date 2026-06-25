from __future__ import annotations

import hashlib
import json
import logging
import os
from datetime import datetime, timezone
from typing import Any
from uuid import UUID, uuid4

import psycopg2
from fastapi import HTTPException

from app.db.connection import get_conn, release_conn
from app.schemas.consent_model import AuditEntry, ConsentRecord
from app.schemas.consent_schema import (
    ConsentAuditHistoryResponse,
    ConsentGrantRequest,
    ConsentGrantResponse,
    ConsentListResponse,
    ConsentRecordResponse,
    ConsentWithdrawRequest,
    ConsentWithdrawResponse,
)

logger = logging.getLogger(__name__)

_ALLOWED_PURPOSES = {"treatment", "research", "marketing", "data_sharing"}
_ALLOWED_LEGAL_BASES = {"consent", "vital_interest", "legal_obligation"}
_ALLOWED_CHANNELS = {"web", "paper", "phone", "in_person"}
_ALLOWED_ROLES = {"patient", "clinician", "dpo", "system"}
_ALLOWED_ACTIONS = {"GRANT", "WITHDRAW", "UPDATE", "EXPIRE", "VIEW"}


def _utc_now() -> datetime:
    return datetime.now(timezone.utc)


def _log_json(message: dict[str, Any]) -> None:
    logger.info(json.dumps(message, default=str))


def _log_error(message: str) -> None:
    logger.error(message)


def _require_env(name: str) -> str:
    value = os.environ.get(name)
    if not value:
        _log_error(f"Missing required environment variable: {name}")
        raise RuntimeError(f"Missing required environment variable: {name}")
    return value


def _get_actor_context(headers: dict[str, Any], event: dict[str, Any]) -> tuple[str, str, str | None, str | None]:
    request_context = event.get("requestContext", {}) if isinstance(event, dict) else {}
    identity = request_context.get("identity", {}) if isinstance(request_context, dict) else {}
    actor_id = str(request_context.get("authorizer", {}).get("actor_id") or headers.get("x-actor-id") or "system")
    actor_role = str(request_context.get("authorizer", {}).get("actor_role") or headers.get("x-actor-role") or "system")
    source_ip = identity.get("sourceIp") or headers.get("x-source-ip")
    user_agent = headers.get("user-agent") or headers.get("User-Agent")
    if actor_role not in _ALLOWED_ROLES:
        raise HTTPException(status_code=422, detail="Validation Error")
    return actor_id, actor_role, source_ip, user_agent


def _require_idempotency_key(headers: dict[str, Any]) -> str:
    key = headers.get("Idempotency-Key") or headers.get("idempotency-key")
    if not isinstance(key, str) or not key or len(key) > 128:
        _log_error("Validation failed: Idempotency-Key")
        raise HTTPException(status_code=422, detail="Validation Error")
    return key


def _parse_uuid(value: str, field_name: str) -> UUID:
    try:
        parsed = UUID(value)
    except Exception:
        _log_error(f"Validation failed: {field_name}")
        raise HTTPException(status_code=422, detail="Validation Error")
    if parsed.version != 4:
        _log_error(f"Validation failed: {field_name}")
        raise HTTPException(status_code=422, detail="Validation Error")
    return parsed


def _row_to_consent_record(row: tuple[Any, ...]) -> ConsentRecord:
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


def _row_to_audit_entry(row: tuple[Any, ...]) -> AuditEntry:
    return AuditEntry(
        audit_id=row[0],
        action=row[1],
        actor_id=row[2],
        actor_role=row[3],
        occurred_at=row[4],
    )


def _hash_chain(previous_hash: str | None, payload: dict[str, Any]) -> str:
    digest = hashlib.sha256()
    digest.update((previous_hash or "").encode("utf-8"))
    digest.update(json.dumps(payload, sort_keys=True, default=str).encode("utf-8"))
    return digest.hexdigest()


def _publish_sns_event(action: str, consent_id: str, patient_id: str) -> None:
    topic_arn = os.environ.get("CONSENT_SNS_TOPIC_ARN")
    if not topic_arn:
        return
    import boto3

    client = boto3.client("sns", region_name=os.environ.get("AWS_REGION", "eu-west-2"))
    client.publish(
        TopicArn=topic_arn,
        Message=json.dumps({"action": action, "consent_id": consent_id, "patient_id": patient_id}),
        Subject="patient-consent-change",
    )


def grant_consent_service(payload: ConsentGrantRequest, headers: dict[str, Any], event: dict[str, Any]) -> ConsentGrantResponse:
    _log_json({"operation": "grant_consent", "resource": "consent"})
    idempotency_key = _require_idempotency_key(headers)
    actor_id, actor_role, source_ip, user_agent = _get_actor_context(headers, event)
    if payload.purpose not in _ALLOWED_PURPOSES or payload.legal_basis not in _ALLOWED_LEGAL_BASES or payload.channel not in _ALLOWED_CHANNELS:
        _log_error("Validation failed: enum")
        raise HTTPException(status_code=422, detail="Validation Error")
    conn = get_conn()
    try:
        conn.rollback()
        conn.autocommit = False
        with conn.cursor() as cursor:
            _log_json({"operation": "SELECT", "table": "patient_consent"})
            cursor.execute("SELECT consent_id, patient_id, purpose, status, legal_basis, scope, consent_version, channel, granted_at, expires_at, withdrawn_at FROM patient_consent WHERE patient_id = %s AND purpose = %s", (str(payload.patient_id), payload.purpose))
            existing = cursor.fetchone()
            if existing is not None:
                consent_id = existing[0]
                _log_json({"operation": "SELECT", "table": "consent_audit_log"})
                cursor.execute("SELECT audit_id, action, actor_id, actor_role, occurred_at FROM consent_audit_log WHERE event_id = %s AND action = 'GRANT' ORDER BY audit_id DESC LIMIT 1", (idempotency_key,))
                replay = cursor.fetchone()
                if replay is not None:
                    conn.rollback()
                    return ConsentGrantResponse(consent_id=consent_id, status="granted", audit_id=replay[0], occurred_at=replay[4])
            consent_id = uuid4()
            occurred_at = _utc_now()
            previous_state = None
            new_state = {
                "consent_id": str(consent_id),
                "patient_id": str(payload.patient_id),
                "purpose": payload.purpose,
                "status": "granted",
                "legal_basis": payload.legal_basis,
                "scope": payload.scope,
                "consent_version": payload.consent_version,
                "channel": payload.channel,
                "granted_at": occurred_at,
                "expires_at": payload.expires_at,
                "withdrawn_at": None,
            }
            _log_json({"operation": "INSERT", "table": "patient_consent"})
            cursor.execute(
                "INSERT INTO patient_consent (consent_id, patient_id, purpose, status, legal_basis, scope, consent_version, channel, granted_at, expires_at, withdrawn_at, created_at, updated_at) VALUES (%s::uuid, %s::uuid, %s, %s, %s, %s::jsonb, %s, %s, %s, %s, %s, %s, %s) ON CONFLICT (patient_id, purpose) DO UPDATE SET status = EXCLUDED.status, legal_basis = EXCLUDED.legal_basis, scope = EXCLUDED.scope, consent_version = EXCLUDED.consent_version, channel = EXCLUDED.channel, granted_at = EXCLUDED.granted_at, expires_at = EXCLUDED.expires_at, withdrawn_at = EXCLUDED.withdrawn_at, updated_at = EXCLUDED.updated_at RETURNING consent_id",
                (str(consent_id), str(payload.patient_id), payload.purpose, "granted", payload.legal_basis, json.dumps(payload.scope), payload.consent_version, payload.channel, occurred_at, payload.expires_at, None, occurred_at, occurred_at),
            )
            returned = cursor.fetchone()
            if returned is None:
                conn.rollback()
                raise HTTPException(status_code=500, detail="Internal Error")
            consent_id = returned[0]
            record_hash = _hash_chain(None, {"event_id": idempotency_key, "action": "GRANT", "consent_id": str(consent_id)})
            _log_json({"operation": "INSERT", "table": "consent_audit_log"})
            cursor.execute(
                "INSERT INTO consent_audit_log (event_id, consent_id, patient_id, action, previous_state, new_state, actor_id, actor_role, source_ip, user_agent, occurred_at, record_hash) VALUES (%s::uuid, %s::uuid, %s::uuid, %s, %s::jsonb, %s::jsonb, %s, %s, %s, %s, %s, %s) RETURNING audit_id, occurred_at",
                (idempotency_key, str(consent_id), str(payload.patient_id), "GRANT", json.dumps(previous_state), json.dumps(new_state), actor_id, actor_role, source_ip, user_agent, occurred_at, record_hash),
            )
            audit_row = cursor.fetchone()
            if audit_row is None:
                conn.rollback()
                raise HTTPException(status_code=500, detail="Internal Error")
            conn.commit()
            _publish_sns_event("GRANT", str(consent_id), str(payload.patient_id))
            return ConsentGrantResponse(consent_id=consent_id, status="granted", audit_id=audit_row[0], occurred_at=audit_row[1])
    except psycopg2.Error as e:
        conn.rollback()
        _log_error(f"Database error: {str(e)}")
        raise HTTPException(status_code=503, detail="Database Error")
    except HTTPException:
        conn.rollback()
        raise
    except Exception as e:
        conn.rollback()
        _log_error(f"Unexpected error: {str(e)}")
        raise HTTPException(status_code=500, detail="Internal Error")
    finally:
        release_conn(conn)


def withdraw_consent_service(consent_id: str, payload: ConsentWithdrawRequest, headers: dict[str, Any], event: dict[str, Any]) -> ConsentWithdrawResponse:
    _log_json({"operation": "withdraw_consent", "resource": "consent"})
    idempotency_key = _require_idempotency_key(headers)
    actor_id, actor_role, source_ip, user_agent = _get_actor_context(headers, event)
    consent_uuid = _parse_uuid(consent_id, "consent_id")
    conn = get_conn()
    try:
        conn.rollback()
        conn.autocommit = False
        with conn.cursor() as cursor:
            _log_json({"operation": "SELECT", "table": "patient_consent"})
            cursor.execute("SELECT consent_id, patient_id, purpose, status, legal_basis, scope, consent_version, channel, granted_at, expires_at, withdrawn_at FROM patient_consent WHERE consent_id = %s", (str(consent_uuid),))
            row = cursor.fetchone()
            if row is None:
                conn.rollback()
                raise HTTPException(status_code=404, detail="Resource Not Found")
            current = _row_to_consent_record(row)
            if current.status == "withdrawn":
                conn.rollback()
                raise HTTPException(status_code=409, detail="ALREADY_WITHDRAWN")
            occurred_at = _utc_now()
            _log_json({"operation": "UPDATE", "table": "patient_consent"})
            cursor.execute("UPDATE patient_consent SET status = %s, withdrawn_at = %s, updated_at = %s WHERE consent_id = %s", ("withdrawn", occurred_at, occurred_at, str(consent_uuid)))
            previous_state = {
                "consent_id": str(current.consent_id),
                "patient_id": str(current.patient_id),
                "purpose": current.purpose,
                "status": current.status,
            }
            new_state = {
                "consent_id": str(current.consent_id),
                "patient_id": str(current.patient_id),
                "purpose": current.purpose,
                "status": "withdrawn",
                "withdrawn_at": occurred_at,
            }
            record_hash = _hash_chain(None, {"event_id": idempotency_key, "action": "WITHDRAW", "consent_id": str(consent_uuid)})
            _log_json({"operation": "INSERT", "table": "consent_audit_log"})
            cursor.execute(
                "INSERT INTO consent_audit_log (event_id, consent_id, patient_id, action, previous_state, new_state, actor_id, actor_role, source_ip, user_agent, occurred_at, record_hash) VALUES (%s::uuid, %s::uuid, %s::uuid, %s, %s::jsonb, %s::jsonb, %s, %s, %s, %s, %s, %s) RETURNING audit_id",
                (idempotency_key, str(consent_uuid), str(current.patient_id), "WITHDRAW", json.dumps(previous_state), json.dumps(new_state), actor_id, actor_role, source_ip, user_agent, occurred_at, record_hash),
            )
            audit_row = cursor.fetchone()
            if audit_row is None:
                conn.rollback()
                raise HTTPException(status_code=500, detail="Internal Error")
            conn.commit()
            _publish_sns_event("WITHDRAW", str(consent_uuid), str(current.patient_id))
            return ConsentWithdrawResponse(consent_id=consent_uuid, status="withdrawn", withdrawn_at=occurred_at, audit_id=audit_row[0])
    except psycopg2.Error as e:
        conn.rollback()
        _log_error(f"Database error: {str(e)}")
        raise HTTPException(status_code=503, detail="Database Error")
    except HTTPException:
        conn.rollback()
        raise
    except Exception as e:
        conn.rollback()
        _log_error(f"Unexpected error: {str(e)}")
        raise HTTPException(status_code=500, detail="Internal Error")
    finally:
        release_conn(conn)


def get_consent_all_service(patient_id: str, headers: dict[str, Any], event: dict[str, Any]) -> ConsentListResponse:
    _log_json({"operation": "get_consent_all", "resource": "consent"})
    actor_id, actor_role, source_ip, user_agent = _get_actor_context(headers, event)
    patient_uuid = _parse_uuid(patient_id, "patient_id")
    conn = get_conn()
    try:
        conn.rollback()
        conn.autocommit = False
        with conn.cursor() as cursor:
            _log_json({"operation": "SELECT", "table": "patient_consent"})
            cursor.execute("SELECT consent_id, patient_id, purpose, status, legal_basis, scope, consent_version, channel, granted_at, expires_at, withdrawn_at FROM patient_consent WHERE patient_id = %s ORDER BY purpose", (str(patient_uuid),))
            rows = cursor.fetchall()
            consents = [ConsentRecordResponse.model_validate(_row_to_consent_record(row).__dict__) for row in rows]
            _log_json({"operation": "INSERT", "table": "consent_audit_log"})
            cursor.execute("INSERT INTO consent_audit_log (event_id, consent_id, patient_id, action, previous_state, new_state, actor_id, actor_role, source_ip, user_agent, occurred_at, record_hash) VALUES (%s::uuid, %s::uuid, %s::uuid, %s, %s::jsonb, %s::jsonb, %s, %s, %s, %s, %s, %s) RETURNING audit_id", (str(uuid4()), None, str(patient_uuid), "VIEW", None, None, actor_id, actor_role, source_ip, user_agent, _utc_now(), _hash_chain(None, {"action": "VIEW", "patient_id": str(patient_uuid)})))
            audit_row = cursor.fetchone()
            if audit_row is None:
                conn.rollback()
                raise HTTPException(status_code=500, detail="Internal Error")
            conn.commit()
            return ConsentListResponse(patient_id=patient_uuid, consents=consents)
    except psycopg2.Error as e:
        conn.rollback()
        _log_error(f"Database error: {str(e)}")
        raise HTTPException(status_code=503, detail="Database Error")
    except HTTPException:
        conn.rollback()
        raise
    except Exception as e:
        conn.rollback()
        _log_error(f"Unexpected error: {str(e)}")
        raise HTTPException(status_code=500, detail="Internal Error")
    finally:
        release_conn(conn)


def get_consent_one_service(patient_id: str, purpose: str, headers: dict[str, Any], event: dict[str, Any]) -> ConsentRecordResponse:
    _log_json({"operation": "get_consent_one", "resource": "consent"})
    actor_id, actor_role, source_ip, user_agent = _get_actor_context(headers, event)
    patient_uuid = _parse_uuid(patient_id, "patient_id")
    if purpose not in _ALLOWED_PURPOSES:
        _log_error("Validation failed: purpose")
        raise HTTPException(status_code=422, detail="Validation Error")
    conn = get_conn()
    try:
        conn.rollback()
        conn.autocommit = False
        with conn.cursor() as cursor:
            _log_json({"operation": "SELECT", "table": "patient_consent"})
            cursor.execute("SELECT consent_id, patient_id, purpose, status, legal_basis, scope, consent_version, channel, granted_at, expires_at, withdrawn_at FROM patient_consent WHERE patient_id = %s AND purpose = %s", (str(patient_uuid), purpose))
            row = cursor.fetchone()
            if row is None:
                conn.rollback()
                raise HTTPException(status_code=404, detail="Resource Not Found")
            record = _row_to_consent_record(row)
            _log_json({"operation": "INSERT", "table": "consent_audit_log"})
            cursor.execute("INSERT INTO consent_audit_log (event_id, consent_id, patient_id, action, previous_state, new_state, actor_id, actor_role, source_ip, user_agent, occurred_at, record_hash) VALUES (%s::uuid, %s::uuid, %s::uuid, %s, %s::jsonb, %s::jsonb, %s, %s, %s, %s, %s, %s) RETURNING audit_id", (str(uuid4()), str(record.consent_id), str(patient_uuid), "VIEW", None, None, actor_id, actor_role, source_ip, user_agent, _utc_now(), _hash_chain(None, {"action": "VIEW", "consent_id": str(record.consent_id)})))
            audit_row = cursor.fetchone()
            if audit_row is None:
                conn.rollback()
                raise HTTPException(status_code=500, detail="Internal Error")
            conn.commit()
            return ConsentRecordResponse.model_validate(record.__dict__)
    except psycopg2.Error as e:
        conn.rollback()
        _log_error(f"Database error: {str(e)}")
        raise HTTPException(status_code=503, detail="Database Error")
    except HTTPException:
        conn.rollback()
        raise
    except Exception as e:
        conn.rollback()
        _log_error(f"Unexpected error: {str(e)}")
        raise HTTPException(status_code=500, detail="Internal Error")
    finally:
        release_conn(conn)


def get_audit_history_service(patient_id: str, headers: dict[str, Any], event: dict[str, Any]) -> ConsentAuditHistoryResponse:
    _log_json({"operation": "get_audit_history", "resource": "consent"})
    actor_id, actor_role, source_ip, user_agent = _get_actor_context(headers, event)
    patient_uuid = _parse_uuid(patient_id, "patient_id")
    conn = get_conn()
    try:
        conn.rollback()
        conn.autocommit = False
        with conn.cursor() as cursor:
            _log_json({"operation": "SELECT", "table": "consent_audit_log"})
            cursor.execute("SELECT audit_id, action, actor_id, actor_role, occurred_at FROM consent_audit_log WHERE patient_id = %s ORDER BY occurred_at DESC LIMIT %s OFFSET %s", (str(patient_uuid), 100, 0))
            rows = cursor.fetchall()
            entries = []
            for row in rows:
                entry = _row_to_audit_entry(row)
                if entry.action not in _ALLOWED_ACTIONS:
                    continue
                entries.append(AuditEntryResponse.model_validate(entry.__dict__))
            _log_json({"operation": "INSERT", "table": "consent_audit_log"})
            cursor.execute("INSERT INTO consent_audit_log (event_id, consent_id, patient_id, action, previous_state, new_state, actor_id, actor_role, source_ip, user_agent, occurred_at, record_hash) VALUES (%s::uuid, %s::uuid, %s::uuid, %s, %s::jsonb, %s::jsonb, %s, %s, %s, %s, %s, %s) RETURNING audit_id", (str(uuid4()), None, str(patient_uuid), "VIEW", None, None, actor_id, actor_role, source_ip, user_agent, _utc_now(), _hash_chain(None, {"action": "VIEW", "patient_id": str(patient_uuid), "resource": "audit"})))
            audit_row = cursor.fetchone()
            if audit_row is None:
                conn.rollback()
                raise HTTPException(status_code=500, detail="Internal Error")
            conn.commit()
            return ConsentAuditHistoryResponse(patient_id=patient_uuid, entries=entries)
    except psycopg2.Error as e:
        conn.rollback()
        _log_error(f"Database error: {str(e)}")
        raise HTTPException(status_code=503, detail="Database Error")
    except HTTPException:
        conn.rollback()
        raise
    except Exception as e:
        conn.rollback()
        _log_error(f"Unexpected error: {str(e)}")
        raise HTTPException(status_code=500, detail="Internal Error")
    finally:
        release_conn(conn)

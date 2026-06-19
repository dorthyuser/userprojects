from __future__ import annotations

import hashlib
import json
import logging
from datetime import datetime, timezone
from uuid import UUID, uuid4

import psycopg2
from fastapi import HTTPException

from app.db.connection import get_conn, release_conn
from app.models.consent_model import ConsentModel
from app.schemas.consent_schema import (
    AuditHistoryResponse,
    AuditRecord,
    ConsentByPatientResponse,
    ConsentRecord,
    ConsentRecordResponse,
    GrantConsentRequest,
    GrantConsentResponse,
    WithdrawConsentRequest,
    WithdrawConsentResponse,
)

logger = logging.getLogger(__name__)

_ALLOWED_PURPOSES = {"treatment", "research", "marketing", "data_sharing"}
_ALLOWED_ROLES = {"patient", "clinician", "dpo", "system"}
_ALLOWED_ACTIONS = {"GRANT", "WITHDRAW", "UPDATE", "EXPIRE", "VIEW"}


def _utcnow() -> datetime:
    return datetime.now(timezone.utc)


def _log_json(message: str, **payload: object) -> None:
    logger.info(json.dumps({"message": message, **payload}, default=str))


def _ensure_env(name: str) -> str:
    import os

    value = os.environ.get(name)
    if not value:
        logger.error(json.dumps({"message": "Missing required environment variable", "variable": name}))
        raise RuntimeError(f"Missing required environment variable: {name}")
    return value


def _headers_get(headers: dict[str, str], name: str) -> str:
    for key, value in headers.items():
        if key.lower() == name.lower():
            return value
    return ""


def _require_idempotency_key(headers: dict[str, str]) -> str:
    value = _headers_get(headers, "Idempotency-Key")
    if not value or len(value) > 128:
        logger.error(json.dumps({"message": "Validation failed", "rule": "Idempotency-Key required and max length 128"}))
        raise HTTPException(status_code=422, detail="Validation Error")
    return value


def _parse_uuid(value: str, field_name: str) -> UUID:
    try:
        parsed = UUID(value)
    except Exception:
        logger.error(json.dumps({"message": "Validation failed", "rule": f"{field_name} must be UUID v4"}))
        raise HTTPException(status_code=422, detail="Validation Error")
    if parsed.version != 4:
        logger.error(json.dumps({"message": "Validation failed", "rule": f"{field_name} must be UUID v4"}))
        raise HTTPException(status_code=422, detail="Validation Error")
    return parsed


def _parse_role(value: object) -> str:
    role = str(value) if value is not None else "system"
    if role not in _ALLOWED_ROLES:
        return "system"
    return role


def _request_context(event: dict[str, object], headers: dict[str, str]) -> tuple[str, str, str | None, str | None]:
    rc = event.get("requestContext", {}) if isinstance(event, dict) else {}
    identity = rc.get("identity", {}) if isinstance(rc, dict) else {}
    actor_id = str(rc.get("authorizer", {}).get("actor_id", "system")) if isinstance(rc, dict) else "system"
    actor_role = _parse_role(rc.get("authorizer", {}).get("actor_role", "system")) if isinstance(rc, dict) else "system"
    source_ip = str(identity.get("sourceIp")) if identity.get("sourceIp") else None
    user_agent = _headers_get(headers, "user-agent") or None
    return actor_id, actor_role, source_ip, user_agent


def _state_from_row(row: tuple[object, ...]) -> ConsentModel:
    return ConsentModel(
        consent_id=row[0],
        patient_id=row[1],
        purpose=row[2],
        status=row[3],
        legal_basis=row[4],
        scope=list(row[5]),
        consent_version=row[6],
        channel=row[7],
        granted_at=row[8],
        expires_at=row[9],
        withdrawn_at=row[10]
    )


def _audit_payload(record: ConsentModel | None) -> dict[str, object] | None:
    if record is None:
        return None
    return {
        "consent_id": str(record.consent_id),
        "patient_id": str(record.patient_id),
        "purpose": record.purpose,
        "status": record.status,
        "legal_basis": record.legal_basis,
        "scope": record.scope,
        "consent_version": record.consent_version,
        "channel": record.channel,
        "granted_at": record.granted_at.isoformat(),
        "expires_at": record.expires_at.isoformat() if record.expires_at else None,
        "withdrawn_at": record.withdrawn_at.isoformat() if record.withdrawn_at else None,
    }


def _record_hash(previous_state: dict[str, object] | None, new_state: dict[str, object] | None, event_id: UUID, occurred_at: datetime) -> str:
    payload = json.dumps({
        "previous_state": previous_state,
        "new_state": new_state,
        "event_id": str(event_id),
        "occurred_at": occurred_at.isoformat()
    }, sort_keys=True, default=str).encode("utf-8")
    return hashlib.sha256(payload).hexdigest()


def _fetch_one(cursor) -> tuple[object, ...] | None:
    row = cursor.fetchone()
    return row


def grant_consent_service(payload: GrantConsentRequest, headers: dict, event: dict) -> GrantConsentResponse:
    _log_json("service_start", operation="grant_consent", resource="consent")
    idempotency_key = _require_idempotency_key(headers)
    patient_id = payload.patient_id
    actor_id, actor_role, source_ip, user_agent = _request_context(event, headers)
    conn = None
    try:
        conn = get_conn()
        conn.rollback()
        conn.autocommit = False
        with conn.cursor() as cursor:
            _log_json("db_operation", table="patient_consent", operation="SELECT")
            cursor.execute("SELECT consent_id, status, granted_at FROM patient_consent WHERE patient_id = %s AND purpose = %s", (patient_id, payload.purpose))
            existing = cursor.fetchone()
            if existing and str(existing[1]) == "withdrawn":
                logger.info(json.dumps({"message": "idempotent replay or withdrawn state", "operation": "grant_consent"}))
            event_id = uuid4()
            now = _utcnow()
            consent_sql = (
                "INSERT INTO patient_consent (patient_id, purpose, status, legal_basis, scope, consent_version, channel, granted_at, expires_at, withdrawn_at, created_at, updated_at) "
                "VALUES (%s, %s::varchar(64), %s::varchar(16), %s::varchar(32), %s::jsonb, %s::varchar(32), %s::varchar(16), %s, %s, %s, now(), now()) "
                "ON CONFLICT (patient_id, purpose) DO UPDATE SET status = EXCLUDED.status, legal_basis = EXCLUDED.legal_basis, scope = EXCLUDED.scope, consent_version = EXCLUDED.consent_version, channel = EXCLUDED.channel, granted_at = EXCLUDED.granted_at, expires_at = EXCLUDED.expires_at, withdrawn_at = EXCLUDED.withdrawn_at, updated_at = now() RETURNING consent_id"
            )
            _log_json("db_operation", table="patient_consent", operation="INSERT")
            cursor.execute(consent_sql, (str(patient_id), payload.purpose, "granted", payload.legal_basis, json.dumps(payload.scope), payload.consent_version, payload.channel, now, payload.expires_at, None))
            consent_row = _fetch_one(cursor)
            if consent_row is None:
                conn.rollback()
                raise HTTPException(status_code=500, detail="Internal Error")
            consent_id = consent_row[0]
            previous_state = None
            new_state = _audit_payload(ConsentModel(consent_id=consent_id, patient_id=patient_id, purpose=payload.purpose, status="granted", legal_basis=payload.legal_basis, scope=payload.scope, consent_version=payload.consent_version, channel=payload.channel, granted_at=now, expires_at=payload.expires_at, withdrawn_at=None))
            record_hash = _record_hash(previous_state, new_state, event_id, now)
            audit_sql = (
                "INSERT INTO consent_audit_log (event_id, consent_id, patient_id, action, previous_state, new_state, actor_id, actor_role, source_ip, user_agent, occurred_at, record_hash) "
                "VALUES (%s, %s, %s, %s::varchar(16), %s::jsonb, %s::jsonb, %s, %s::varchar(32), %s, %s, %s, %s) RETURNING audit_id"
            )
            _log_json("db_operation", table="consent_audit_log", operation="INSERT")
            cursor.execute(audit_sql, (event_id, consent_id, patient_id, "GRANT", json.dumps(previous_state) if previous_state is not None else None, json.dumps(new_state), actor_id, actor_role, source_ip, user_agent, now, record_hash))
            audit_row = _fetch_one(cursor)
            if audit_row is None:
                conn.rollback()
                raise HTTPException(status_code=500, detail="Internal Error")
            conn.commit()
        return GrantConsentResponse(consent_id=consent_id, status="granted", audit_id=int(audit_row[0]), occurred_at=now)
    except HTTPException:
        if conn:
            conn.rollback()
        raise
    except psycopg2.Error as e:
        if conn:
            conn.rollback()
        logger.error(f"Database error: {str(e)}")
        raise HTTPException(status_code=503, detail="Database Error")
    except Exception as e:
        if conn:
            conn.rollback()
        logger.error(f"Unexpected error: {str(e)}", exc_info=True)
        raise HTTPException(status_code=500, detail="Internal Error")
    finally:
        if conn is not None:
            release_conn(conn)


def withdraw_consent_service(consent_id: str, payload: WithdrawConsentRequest, headers: dict, event: dict) -> WithdrawConsentResponse:
    _log_json("service_start", operation="withdraw_consent", resource="consent")
    _require_idempotency_key(headers)
    consent_uuid = _parse_uuid(consent_id, "consent_id")
    actor_id, actor_role, source_ip, user_agent = _request_context(event, headers)
    conn = None
    try:
        conn = get_conn()
        conn.rollback()
        conn.autocommit = False
        with conn.cursor() as cursor:
            _log_json("db_operation", table="patient_consent", operation="SELECT")
            cursor.execute("SELECT consent_id, patient_id, purpose, status, legal_basis, scope, consent_version, channel, granted_at, expires_at, withdrawn_at FROM patient_consent WHERE consent_id = %s", (consent_uuid,))
            row = cursor.fetchone()
            if row is None:
                conn.rollback()
                raise HTTPException(status_code=404, detail="Resource Not Found")
            if str(row[3]) == "withdrawn":
                conn.rollback()
                raise HTTPException(status_code=400, detail="Parsing Error")
            now = _utcnow()
            previous = ConsentModel(
                consent_id=row[0], patient_id=row[1], purpose=row[2], status=row[3], legal_basis=row[4], scope=list(row[5]), consent_version=row[6], channel=row[7], granted_at=row[8], expires_at=row[9], withdrawn_at=row[10]
            )
            _log_json("db_operation", table="patient_consent", operation="UPDATE")
            cursor.execute("UPDATE patient_consent SET status = %s::varchar(16), withdrawn_at = %s, updated_at = now() WHERE consent_id = %s", ("withdrawn", now, consent_uuid))
            event_id = uuid4()
            new_state = _audit_payload(ConsentModel(consent_id=row[0], patient_id=row[1], purpose=row[2], status="withdrawn", legal_basis=row[4], scope=list(row[5]), consent_version=row[6], channel=row[7], granted_at=row[8], expires_at=row[9], withdrawn_at=now))
            record_hash = _record_hash(_audit_payload(previous), new_state, event_id, now)
            _log_json("db_operation", table="consent_audit_log", operation="INSERT")
            cursor.execute(
                "INSERT INTO consent_audit_log (event_id, consent_id, patient_id, action, previous_state, new_state, actor_id, actor_role, source_ip, user_agent, occurred_at, record_hash) VALUES (%s, %s, %s, %s::varchar(16), %s::jsonb, %s::jsonb, %s, %s::varchar(32), %s, %s, %s, %s) RETURNING audit_id",
                (event_id, consent_uuid, previous.patient_id, "WITHDRAW", json.dumps(_audit_payload(previous)), json.dumps(new_state), actor_id, actor_role, source_ip, user_agent, now, record_hash)
            )
            audit_row = cursor.fetchone()
            if audit_row is None:
                conn.rollback()
                raise HTTPException(status_code=500, detail="Internal Error")
            conn.commit()
        return WithdrawConsentResponse(consent_id=consent_uuid, status="withdrawn", withdrawn_at=now, audit_id=int(audit_row[0]))
    except HTTPException:
        if conn:
            conn.rollback()
        raise
    except psycopg2.Error as e:
        if conn:
            conn.rollback()
        logger.error(f"Database error: {str(e)}")
        raise HTTPException(status_code=503, detail="Database Error")
    except Exception as e:
        if conn:
            conn.rollback()
        logger.error(f"Unexpected error: {str(e)}", exc_info=True)
        raise HTTPException(status_code=500, detail="Internal Error")
    finally:
        if conn is not None:
            release_conn(conn)


def get_consent_all_service(patient_id: str, headers: dict, event: dict) -> ConsentByPatientResponse:
    _log_json("service_start", operation="get_consent_all", resource="consent")
    patient_uuid = _parse_uuid(patient_id, "patient_id")
    actor_id, actor_role, source_ip, user_agent = _request_context(event, headers)
    conn = None
    try:
        conn = get_conn()
        conn.rollback()
        conn.autocommit = False
        with conn.cursor() as cursor:
            _log_json("db_operation", table="patient_consent", operation="SELECT")
            cursor.execute("SELECT consent_id, patient_id, purpose, status, legal_basis, scope, consent_version, channel, granted_at, expires_at, withdrawn_at FROM patient_consent WHERE patient_id = %s ORDER BY created_at ASC", (patient_uuid,))
            rows = cursor.fetchall()
            consents: list[ConsentRecord] = []
            now = _utcnow()
            for row in rows:
                consents.append(ConsentRecord(consent_id=row[0], patient_id=row[1], purpose=row[2], status=row[3], legal_basis=row[4], scope=list(row[5]), consent_version=row[6], channel=row[7], granted_at=row[8], expires_at=row[9], withdrawn_at=row[10]))
            event_id = uuid4()
            _log_json("db_operation", table="consent_audit_log", operation="INSERT")
            cursor.execute(
                "INSERT INTO consent_audit_log (event_id, consent_id, patient_id, action, previous_state, new_state, actor_id, actor_role, source_ip, user_agent, occurred_at, record_hash) VALUES (%s, %s, %s, %s::varchar(16), %s::jsonb, %s::jsonb, %s, %s::varchar(32), %s, %s, %s, %s) RETURNING audit_id",
                (event_id, None, patient_uuid, "VIEW", None, json.dumps({"patient_id": str(patient_uuid), "count": len(consents)}), actor_id, actor_role, source_ip, user_agent, now, _record_hash(None, {"patient_id": str(patient_uuid), "count": len(consents)}, event_id, now))
            )
            audit_row = cursor.fetchone()
            if audit_row is None:
                conn.rollback()
                raise HTTPException(status_code=500, detail="Internal Error")
            conn.commit()
        return ConsentByPatientResponse(patient_id=patient_uuid, consents=consents)
    except HTTPException:
        if conn:
            conn.rollback()
        raise
    except psycopg2.Error as e:
        if conn:
            conn.rollback()
        logger.error(f"Database error: {str(e)}")
        raise HTTPException(status_code=503, detail="Database Error")
    except Exception as e:
        if conn:
            conn.rollback()
        logger.error(f"Unexpected error: {str(e)}", exc_info=True)
        raise HTTPException(status_code=500, detail="Internal Error")
    finally:
        if conn is not None:
            release_conn(conn)


def get_consent_one_service(patient_id: str, purpose: str, headers: dict, event: dict) -> ConsentRecordResponse:
    _log_json("service_start", operation="get_consent_one", resource="consent")
    patient_uuid = _parse_uuid(patient_id, "patient_id")
    if purpose not in _ALLOWED_PURPOSES:
        logger.error(json.dumps({"message": "Validation failed", "rule": "purpose enum"}))
        raise HTTPException(status_code=422, detail="Validation Error")
    actor_id, actor_role, source_ip, user_agent = _request_context(event, headers)
    conn = None
    try:
        conn = get_conn()
        conn.rollback()
        conn.autocommit = False
        with conn.cursor() as cursor:
            _log_json("db_operation", table="patient_consent", operation="SELECT")
            cursor.execute("SELECT consent_id, patient_id, purpose, status, legal_basis, scope, consent_version, channel, granted_at, expires_at, withdrawn_at FROM patient_consent WHERE patient_id = %s AND purpose = %s", (patient_uuid, purpose))
            row = cursor.fetchone()
            if row is None:
                conn.rollback()
                raise HTTPException(status_code=404, detail="Resource Not Found")
            now = _utcnow()
            event_id = uuid4()
            _log_json("db_operation", table="consent_audit_log", operation="INSERT")
            cursor.execute(
                "INSERT INTO consent_audit_log (event_id, consent_id, patient_id, action, previous_state, new_state, actor_id, actor_role, source_ip, user_agent, occurred_at, record_hash) VALUES (%s, %s, %s, %s::varchar(16), %s::jsonb, %s::jsonb, %s, %s::varchar(32), %s, %s, %s, %s) RETURNING audit_id",
                (event_id, row[0], patient_uuid, "VIEW", None, json.dumps({"consent_id": str(row[0])}), actor_id, actor_role, source_ip, user_agent, now, _record_hash(None, {"consent_id": str(row[0])}, event_id, now))
            )
            audit_row = cursor.fetchone()
            if audit_row is None:
                conn.rollback()
                raise HTTPException(status_code=500, detail="Internal Error")
            conn.commit()
        return ConsentRecordResponse(consent_id=row[0], patient_id=row[1], purpose=row[2], status=row[3], legal_basis=row[4], scope=list(row[5]), consent_version=row[6], channel=row[7], granted_at=row[8], expires_at=row[9], withdrawn_at=row[10])
    except HTTPException:
        if conn:
            conn.rollback()
        raise
    except psycopg2.Error as e:
        if conn:
            conn.rollback()
        logger.error(f"Database error: {str(e)}")
        raise HTTPException(status_code=503, detail="Database Error")
    except Exception as e:
        if conn:
            conn.rollback()
        logger.error(f"Unexpected error: {str(e)}", exc_info=True)
        raise HTTPException(status_code=500, detail="Internal Error")
    finally:
        if conn is not None:
            release_conn(conn)


def get_audit_history_service(patient_id: str, headers: dict, event: dict) -> AuditHistoryResponse:
    _log_json("service_start", operation="get_audit_history", resource="consent")
    patient_uuid = _parse_uuid(patient_id, "patient_id")
    actor_id, actor_role, source_ip, user_agent = _request_context(event, headers)
    conn = None
    try:
        conn = get_conn()
        conn.rollback()
        conn.autocommit = False
        with conn.cursor() as cursor:
            _log_json("db_operation", table="consent_audit_log", operation="SELECT")
            cursor.execute("SELECT audit_id, action, actor_id, actor_role, occurred_at FROM consent_audit_log WHERE patient_id = %s ORDER BY occurred_at DESC LIMIT 100", (patient_uuid,))
            rows = cursor.fetchall()
            entries = [AuditRecord(audit_id=row[0], action=row[1], actor_id=row[2], actor_role=row[3], occurred_at=row[4]) for row in rows]
            now = _utcnow()
            event_id = uuid4()
            _log_json("db_operation", table="consent_audit_log", operation="INSERT")
            cursor.execute(
                "INSERT INTO consent_audit_log (event_id, consent_id, patient_id, action, previous_state, new_state, actor_id, actor_role, source_ip, user_agent, occurred_at, record_hash) VALUES (%s, %s, %s, %s::varchar(16), %s::jsonb, %s::jsonb, %s, %s::varchar(32), %s, %s, %s, %s) RETURNING audit_id",
                (event_id, None, patient_uuid, "VIEW", None, json.dumps({"patient_id": str(patient_uuid), "audit_count": len(entries)}), actor_id, actor_role, source_ip, user_agent, now, _record_hash(None, {"patient_id": str(patient_uuid), "audit_count": len(entries)}, event_id, now))
            )
            audit_row = cursor.fetchone()
            if audit_row is None:
                conn.rollback()
                raise HTTPException(status_code=500, detail="Internal Error")
            conn.commit()
        return AuditHistoryResponse(patient_id=patient_uuid, entries=entries)
    except HTTPException:
        if conn:
            conn.rollback()
        raise
    except psycopg2.Error as e:
        if conn:
            conn.rollback()
        logger.error(f"Database error: {str(e)}")
        raise HTTPException(status_code=503, detail="Database Error")
    except Exception as e:
        if conn:
            conn.rollback()
        logger.error(f"Unexpected error: {str(e)}", exc_info=True)
        raise HTTPException(status_code=500, detail="Internal Error")
    finally:
        if conn is not None:
            release_conn(conn)

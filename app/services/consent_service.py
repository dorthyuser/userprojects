from __future__ import annotations

import hashlib
import json
import logging
import os
from datetime import UTC, datetime
from uuid import UUID, uuid5, NAMESPACE_URL

import boto3
import psycopg2
from fastapi import HTTPException

from app.db.connection import get_conn, release_conn
from app.models.consent_model import ConsentAuditEntry, ConsentState
from app.schemas.consent_schema import (
    AuditRecord,
    ConsentAuditHistoryResponse,
    ConsentGetAllResponse,
    ConsentGetOneResponse,
    ConsentGrantRequest,
    ConsentGrantResponse,
    ConsentRecord,
    ConsentWithdrawRequest,
    ConsentWithdrawResponse,
)

logger = logging.getLogger(__name__)

_ALLOWED_PURPOSES = {"treatment", "research", "marketing", "data_sharing"}
_ALLOWED_STATUSES = {"granted", "withdrawn", "expired"}
_ALLOWED_BASIS = {"consent", "vital_interest", "legal_obligation"}
_ALLOWED_CHANNELS = {"web", "paper", "phone", "in_person"}
_ALLOWED_ACTIONS = {"GRANT", "WITHDRAW", "UPDATE", "EXPIRE", "VIEW"}


def _now_utc() -> datetime:
    return datetime.now(tz=UTC)


def _json_log(message: str, **fields: object) -> None:
    payload = {"message": message}
    payload.update(fields)
    logger.info(json.dumps(payload, default=str, separators=(",", ":")))


def _env(name: str) -> str:
    value = os.getenv(name)
    if value is None or not value.strip():
        raise RuntimeError(f"Missing required environment variable: {name}")
    return value


def _sns_client():
    region = os.getenv("AWS_REGION", "eu-west-2")
    return boto3.client("sns", region_name=region)


def _publish_event(event_name: str, payload: dict[str, object]) -> None:
    topic_arn = os.getenv("CONSENT_EVENTS_TOPIC_ARN")
    if not topic_arn:
        return
    sns = _sns_client()
    sns.publish(TopicArn=topic_arn, Message=json.dumps({"event": event_name, **payload}, default=str))


def _stable_event_id(idempotency_key: str, action: str) -> UUID:
    return uuid5(NAMESPACE_URL, f"patient-consent-management1005:{action}:{idempotency_key}")


def _hash_chain(previous_hash: str | None, row_data: dict[str, object]) -> str:
    digest = hashlib.sha256()
    digest.update((previous_hash or "").encode("utf-8"))
    digest.update(json.dumps(row_data, sort_keys=True, default=str).encode("utf-8"))
    return digest.hexdigest()


def _mask_user_agent(user_agent: str | None) -> str | None:
    if user_agent is None:
        return None
    return user_agent[:512]


def _extract_actor(event: dict[str, object]) -> tuple[str, str, str | None, str | None]:
    request_context = event.get("requestContext") or {}
    headers = event.get("headers") or {}
    authorizer = request_context.get("authorizer") or {}
    identity = request_context.get("identity") or {}
    actor_id = str(authorizer.get("actor_id") or authorizer.get("principalId") or "system")
    actor_role = str(authorizer.get("actor_role") or "system")
    source_ip = identity.get("sourceIp") or headers.get("X-Forwarded-For")
    user_agent = headers.get("User-Agent") or headers.get("user-agent")
    return actor_id, actor_role, str(source_ip) if source_ip else None, str(user_agent) if user_agent else None


def _validate_idempotency_key(headers: dict[str, object]) -> str:
    key = headers.get("Idempotency-Key") or headers.get("idempotency-key")
    if not isinstance(key, str) or not key.strip() or len(key) > 128:
        _json_log("validation_failure", rule="Idempotency-Key missing or invalid")
        raise HTTPException(status_code=422, detail="Validation Error")
    return key


def _fetch_audit_hash(cur, consent_id: UUID) -> str | None:
    cur.execute(
        "SELECT record_hash FROM consent_audit_log WHERE consent_id = %s ORDER BY occurred_at DESC, audit_id DESC LIMIT 1",
        (consent_id,),
    )
    row = cur.fetchone()
    return row[0] if row else None


def _row_to_record(row: tuple[object, ...]) -> ConsentRecord:
    return ConsentRecord(
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
        withdrawn_at=row[10],
    )


def grant_consent_service(payload: ConsentGrantRequest, headers: dict[str, object], event: dict[str, object]) -> ConsentGrantResponse:
    _json_log("service_start", operation="grant_consent", resource="consent")
    idempotency_key = _validate_idempotency_key(headers)
    actor_id, actor_role, source_ip, user_agent = _extract_actor(event)
    event_id = _stable_event_id(idempotency_key, "GRANT")
    conn = get_conn()
    try:
        conn.rollback()
        conn.autocommit = False
        with conn.cursor() as cur:
            _json_log("db_operation", table="patient_consent", operation="UPSERT")
            cur.execute(
                "SELECT consent_id, status, granted_at, withdrawn_at FROM patient_consent WHERE patient_id = %s AND purpose = %s",
                (payload.patient_id, payload.purpose),
            )
            existing = cur.fetchone()
            if existing and existing[1] == "withdrawn":
                conn.rollback()
                raise HTTPException(status_code=409, detail="Consent already withdrawn")
            now = _now_utc()
            expires_at = payload.expires_at
            if existing is None:
                _json_log("db_operation", table="patient_consent", operation="INSERT")
                cur.execute(
                    """
                    INSERT INTO patient_consent
                    (patient_id, purpose, status, legal_basis, scope, consent_version, channel, granted_at, expires_at, withdrawn_at, created_at, updated_at)
                    VALUES (%s, %s::varchar(64), %s::varchar(16), %s::varchar(32), %s::jsonb, %s, %s::varchar(16), %s, %s, %s, %s, %s)
                    RETURNING consent_id, granted_at
                    """,
                    (
                        payload.patient_id,
                        payload.purpose,
                        "granted",
                        payload.legal_basis,
                        json.dumps(payload.scope),
                        payload.consent_version,
                        payload.channel,
                        now,
                        expires_at,
                        None,
                        now,
                        now,
                    ),
                )
                inserted = cur.fetchone()
                if inserted is None:
                    conn.rollback()
                    raise HTTPException(status_code=500, detail="Internal Error")
                consent_id, granted_at = inserted
                previous_state = None
            else:
                consent_id = existing[0]
                granted_at = existing[2]
                previous_state = {
                    "status": existing[1],
                    "withdrawn_at": existing[3],
                }
                _json_log("db_operation", table="patient_consent", operation="UPDATE")
                cur.execute(
                    """
                    UPDATE patient_consent
                    SET status = %s::varchar(16), legal_basis = %s::varchar(32), scope = %s::jsonb,
                        consent_version = %s, channel = %s::varchar(16), granted_at = %s, expires_at = %s,
                        withdrawn_at = NULL, updated_at = %s
                    WHERE consent_id = %s
                    RETURNING consent_id, granted_at
                    """,
                    (
                        "granted",
                        payload.legal_basis,
                        json.dumps(payload.scope),
                        payload.consent_version,
                        payload.channel,
                        now,
                        expires_at,
                        now,
                        consent_id,
                    ),
                )
                updated = cur.fetchone()
                if updated is None:
                    conn.rollback()
                    raise HTTPException(status_code=500, detail="Internal Error")
                consent_id, granted_at = updated
            prev_hash = _fetch_audit_hash(cur, consent_id)
            record = ConsentState(
                consent_id=consent_id,
                patient_id=payload.patient_id,
                purpose=payload.purpose,
                status="granted",
                legal_basis=payload.legal_basis,
                scope=payload.scope,
                consent_version=payload.consent_version,
                channel=payload.channel,
                granted_at=granted_at,
                expires_at=expires_at,
                withdrawn_at=None,
            )
            new_state = record.__dict__.copy()
            record_hash = _hash_chain(prev_hash, {"action": "GRANT", "consent_id": str(consent_id), "patient_id": str(payload.patient_id)})
            _json_log("db_operation", table="consent_audit_log", operation="INSERT")
            cur.execute(
                """
                INSERT INTO consent_audit_log
                (event_id, consent_id, patient_id, action, previous_state, new_state, actor_id, actor_role, source_ip, user_agent, occurred_at, record_hash)
                VALUES (%s, %s, %s, %s::varchar(16), %s::jsonb, %s::jsonb, %s, %s::varchar(32), %s, %s, %s, %s)
                RETURNING audit_id, occurred_at
                """,
                (
                    event_id,
                    consent_id,
                    payload.patient_id,
                    "GRANT",
                    json.dumps(previous_state) if previous_state is not None else None,
                    json.dumps(new_state, default=str),
                    actor_id,
                    actor_role,
                    source_ip,
                    _mask_user_agent(user_agent),
                    now,
                    record_hash,
                ),
            )
            audit_row = cur.fetchone()
            if audit_row is None:
                conn.rollback()
                raise HTTPException(status_code=500, detail="Internal Error")
            audit_id, occurred_at = audit_row
            conn.commit()
        _publish_event("CONSENT_GRANTED", {"consent_id": str(consent_id), "patient_id": str(payload.patient_id), "audit_id": audit_id})
        return ConsentGrantResponse(consent_id=consent_id, status="granted", audit_id=audit_id, occurred_at=occurred_at)
    except HTTPException:
        raise
    except psycopg2.Error as exc:
        conn.rollback()
        _json_log("database_error", error=str(exc))
        raise HTTPException(status_code=503, detail="Database Error")
    except Exception as exc:
        conn.rollback()
        _json_log("unexpected_error", error=str(exc))
        raise HTTPException(status_code=500, detail="Internal Error")
    finally:
        release_conn(conn)


def withdraw_consent_service(consent_id: UUID, payload: ConsentWithdrawRequest, headers: dict[str, object], event: dict[str, object]) -> ConsentWithdrawResponse:
    _json_log("service_start", operation="withdraw_consent", resource="consent")
    idempotency_key = _validate_idempotency_key(headers)
    actor_id, actor_role, source_ip, user_agent = _extract_actor(event)
    event_id = _stable_event_id(idempotency_key, "WITHDRAW")
    conn = get_conn()
    try:
        conn.rollback()
        conn.autocommit = False
        with conn.cursor() as cur:
            _json_log("db_operation", table="patient_consent", operation="SELECT")
            cur.execute("SELECT consent_id, patient_id, status, legal_basis, scope, consent_version, channel, granted_at, expires_at, withdrawn_at FROM patient_consent WHERE consent_id = %s", (consent_id,))
            row = cur.fetchone()
            if row is None:
                conn.rollback()
                raise HTTPException(status_code=404, detail="Resource Not Found")
            if row[2] == "withdrawn":
                conn.rollback()
                raise HTTPException(status_code=409, detail="Consent already withdrawn")
            now = _now_utc()
            _json_log("db_operation", table="patient_consent", operation="UPDATE")
            cur.execute(
                "UPDATE patient_consent SET status = %s::varchar(16), withdrawn_at = %s, updated_at = %s WHERE consent_id = %s RETURNING patient_id",
                ("withdrawn", now, now, consent_id),
            )
            updated = cur.fetchone()
            if updated is None:
                conn.rollback()
                raise HTTPException(status_code=500, detail="Internal Error")
            patient_id = updated[0]
            prev_hash = _fetch_audit_hash(cur, consent_id)
            previous_state = {"status": row[2], "withdrawn_at": row[9]}
            new_state = {"status": "withdrawn", "withdrawn_at": now.isoformat()}
            record_hash = _hash_chain(prev_hash, {"action": "WITHDRAW", "consent_id": str(consent_id), "patient_id": str(patient_id)})
            _json_log("db_operation", table="consent_audit_log", operation="INSERT")
            cur.execute(
                """
                INSERT INTO consent_audit_log
                (event_id, consent_id, patient_id, action, previous_state, new_state, actor_id, actor_role, source_ip, user_agent, occurred_at, record_hash)
                VALUES (%s, %s, %s, %s::varchar(16), %s::jsonb, %s::jsonb, %s, %s::varchar(32), %s, %s, %s, %s)
                RETURNING audit_id, occurred_at
                """,
                (
                    event_id,
                    consent_id,
                    patient_id,
                    "WITHDRAW",
                    json.dumps(previous_state, default=str),
                    json.dumps(new_state, default=str),
                    actor_id,
                    actor_role,
                    source_ip,
                    _mask_user_agent(user_agent),
                    now,
                    record_hash,
                ),
            )
            audit_row = cur.fetchone()
            if audit_row is None:
                conn.rollback()
                raise HTTPException(status_code=500, detail="Internal Error")
            audit_id, occurred_at = audit_row
            conn.commit()
        _publish_event("CONSENT_WITHDRAWN", {"consent_id": str(consent_id), "audit_id": audit_id})
        return ConsentWithdrawResponse(consent_id=consent_id, status="withdrawn", withdrawn_at=occurred_at, audit_id=audit_id)
    except HTTPException:
        raise
    except psycopg2.Error as exc:
        conn.rollback()
        _json_log("database_error", error=str(exc))
        raise HTTPException(status_code=503, detail="Database Error")
    except Exception as exc:
        conn.rollback()
        _json_log("unexpected_error", error=str(exc))
        raise HTTPException(status_code=500, detail="Internal Error")
    finally:
        release_conn(conn)


def get_consent_all_service(patient_id: UUID, event: dict[str, object]) -> ConsentGetAllResponse:
    _json_log("service_start", operation="get_consent_all", resource="consent")
    actor_id, actor_role, source_ip, user_agent = _extract_actor(event)
    conn = get_conn()
    try:
        conn.rollback()
        conn.autocommit = False
        with conn.cursor() as cur:
            _json_log("db_operation", table="patient_consent", operation="SELECT")
            cur.execute("SELECT consent_id, patient_id, purpose, status, legal_basis, scope, consent_version, channel, granted_at, expires_at, withdrawn_at FROM patient_consent WHERE patient_id = %s ORDER BY granted_at DESC", (patient_id,))
            rows = cur.fetchall()
            records = [_row_to_record(row) for row in rows]
            now = _now_utc()
            _json_log("db_operation", table="consent_audit_log", operation="INSERT")
            cur.execute(
                """
                INSERT INTO consent_audit_log
                (event_id, consent_id, patient_id, action, previous_state, new_state, actor_id, actor_role, source_ip, user_agent, occurred_at, record_hash)
                VALUES (%s, NULL, %s, %s::varchar(16), NULL, NULL, %s, %s::varchar(32), %s, %s, %s, %s)
                RETURNING audit_id, occurred_at
                """,
                (
                    uuid5(NAMESPACE_URL, f"patient-consent-management1005:VIEW:{patient_id}:{now.isoformat()}"),
                    patient_id,
                    "VIEW",
                    actor_id,
                    actor_role,
                    source_ip,
                    _mask_user_agent(user_agent),
                    now,
                    None,
                ),
            )
            cur.fetchone()
            conn.commit()
        return ConsentGetAllResponse(patient_id=patient_id, consents=records)
    except HTTPException:
        raise
    except psycopg2.Error as exc:
        conn.rollback()
        _json_log("database_error", error=str(exc))
        raise HTTPException(status_code=503, detail="Database Error")
    except Exception as exc:
        conn.rollback()
        _json_log("unexpected_error", error=str(exc))
        raise HTTPException(status_code=500, detail="Internal Error")
    finally:
        release_conn(conn)


def get_consent_one_service(patient_id: UUID, purpose: str, event: dict[str, object]) -> ConsentGetOneResponse:
    _json_log("service_start", operation="get_consent_one", resource="consent")
    actor_id, actor_role, source_ip, user_agent = _extract_actor(event)
    conn = get_conn()
    try:
        conn.rollback()
        conn.autocommit = False
        with conn.cursor() as cur:
            _json_log("db_operation", table="patient_consent", operation="SELECT")
            cur.execute("SELECT consent_id, patient_id, purpose, status, legal_basis, scope, consent_version, channel, granted_at, expires_at, withdrawn_at FROM patient_consent WHERE patient_id = %s AND purpose = %s", (patient_id, purpose))
            row = cur.fetchone()
            if row is None:
                conn.rollback()
                raise HTTPException(status_code=404, detail="Resource Not Found")
            now = _now_utc()
            _json_log("db_operation", table="consent_audit_log", operation="INSERT")
            cur.execute(
                """
                INSERT INTO consent_audit_log
                (event_id, consent_id, patient_id, action, previous_state, new_state, actor_id, actor_role, source_ip, user_agent, occurred_at, record_hash)
                VALUES (%s, %s, %s, %s::varchar(16), NULL, NULL, %s, %s::varchar(32), %s, %s, %s, %s)
                RETURNING audit_id, occurred_at
                """,
                (
                    uuid5(NAMESPACE_URL, f"patient-consent-management1005:VIEW:{patient_id}:{purpose}:{now.isoformat()}"),
                    row[0],
                    patient_id,
                    "VIEW",
                    actor_id,
                    actor_role,
                    source_ip,
                    _mask_user_agent(user_agent),
                    now,
                    None,
                ),
            )
            cur.fetchone()
            conn.commit()
        return ConsentGetOneResponse(
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
            withdrawn_at=row[10],
        )
    except HTTPException:
        raise
    except psycopg2.Error as exc:
        conn.rollback()
        _json_log("database_error", error=str(exc))
        raise HTTPException(status_code=503, detail="Database Error")
    except Exception as exc:
        conn.rollback()
        _json_log("unexpected_error", error=str(exc))
        raise HTTPException(status_code=500, detail="Internal Error")
    finally:
        release_conn(conn)


def get_audit_history_service(patient_id: UUID, limit: int, offset: int, event: dict[str, object]) -> ConsentAuditHistoryResponse:
    _json_log("service_start", operation="get_audit_history", resource="consent")
    actor_id, actor_role, source_ip, user_agent = _extract_actor(event)
    conn = get_conn()
    try:
        conn.rollback()
        conn.autocommit = False
        with conn.cursor() as cur:
            _json_log("db_operation", table="consent_audit_log", operation="SELECT")
            cur.execute(
                "SELECT audit_id, action, actor_id, actor_role, occurred_at FROM consent_audit_log WHERE patient_id = %s ORDER BY occurred_at DESC, audit_id DESC LIMIT %s OFFSET %s",
                (patient_id, limit, offset),
            )
            rows = cur.fetchall()
            entries = [AuditRecord(audit_id=row[0], action=row[1], actor_id=row[2], actor_role=row[3], occurred_at=row[4]) for row in rows]
            now = _now_utc()
            _json_log("db_operation", table="consent_audit_log", operation="INSERT")
            cur.execute(
                """
                INSERT INTO consent_audit_log
                (event_id, consent_id, patient_id, action, previous_state, new_state, actor_id, actor_role, source_ip, user_agent, occurred_at, record_hash)
                VALUES (%s, NULL, %s, %s::varchar(16), NULL, NULL, %s, %s::varchar(32), %s, %s, %s, %s)
                RETURNING audit_id, occurred_at
                """,
                (
                    uuid5(NAMESPACE_URL, f"patient-consent-management1005:VIEW-AUDIT:{patient_id}:{now.isoformat()}"),
                    patient_id,
                    "VIEW",
                    actor_id,
                    actor_role,
                    source_ip,
                    _mask_user_agent(user_agent),
                    now,
                    None,
                ),
            )
            cur.fetchone()
            conn.commit()
        return ConsentAuditHistoryResponse(patient_id=patient_id, entries=entries)
    except HTTPException:
        raise
    except psycopg2.Error as exc:
        conn.rollback()
        _json_log("database_error", error=str(exc))
        raise HTTPException(status_code=503, detail="Database Error")
    except Exception as exc:
        conn.rollback()
        _json_log("unexpected_error", error=str(exc))
        raise HTTPException(status_code=500, detail="Internal Error")
    finally:
        release_conn(conn)

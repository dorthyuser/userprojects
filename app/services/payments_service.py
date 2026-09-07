from __future__ import annotations

import hashlib
import hmac
import json
import logging
import os
import secrets
import time
from dataclasses import asdict
from decimal import Decimal, ROUND_HALF_UP
from typing import Any

import psycopg2
import requests
from fastapi import HTTPException, Request

from app.db.connection import get_conn, release_conn
from app.models.payments_model import PaymentRecord, PaymentVerificationRecord
from app.schemas.payments_schema import (
    PaymentCreateRequest,
    PaymentCreateResponse,
    PaymentListItem,
    PaymentListResponse,
    PaymentVerifyRequest,
    PaymentVerifyResponse,
)

logger = logging.getLogger(__name__)

ALLOWED_PAYMENT_METHODS = {"UPI", "CARD", "NETBANKING", "WALLET", "BANK_TRANSFER"}
ALLOWED_VERIFICATION_SOURCES = {"WEBHOOK", "POLLING", "MANUAL"}
ALLOWED_PAYMENT_STATUSES = {"PENDING", "SUCCESS", "FAILED", "REFUNDED", "CANCELLED"}
ALLOWED_GATEWAY_STATUSES = {"SUCCESS", "FAILED", "CANCELLED"}
ALLOWED_CURRENCIES = {"INR", "USD", "EUR", "GBP", "AED", "SGD", "AUD", "CAD", "JPY"}


def _log_json(message: dict[str, Any]) -> None:
    logger.info(json.dumps(message, default=str))


def _log_error(message: dict[str, Any]) -> None:
    logger.error(json.dumps(message, default=str))


def _request_id(request: Request) -> str:
    return request.headers.get("x-request-id", secrets.token_hex(8))


def _env(name: str, default: str | None = None) -> str:
    value = os.environ.get(name, default)
    if value is None or value == "":
        raise RuntimeError(f"Missing required environment variable: {name}")
    return value


def _decimal(value: Any) -> Decimal:
    return Decimal(str(value)).quantize(Decimal("0.01"), rounding=ROUND_HALF_UP)


def _payment_id_from_seq(seq: int) -> str:
    return f"PAY-{time.gmtime().tm_year}-{seq:06d}"


def _verification_id_from_seq(seq: int) -> str:
    return f"VRF-{time.gmtime().tm_year}-{seq:06d}"


def _invoice_id_from_seq(seq: int) -> str:
    return f"INV-{time.gmtime().tm_year}-{seq:06d}"


def _gateway_create_order(amount: Decimal, currency: str, payment_method: str, email: str, description: str | None, metadata: dict[str, Any] | None) -> str:
    retry_count = int(os.environ.get("GATEWAY_RETRY_COUNT", "2"))
    url = os.environ.get("RAZORPAY_ORDER_URL", "https://api.razorpay.com/v1/orders")
    auth_user = os.environ.get("RAZORPAY_KEY_ID", "")
    auth_pass = os.environ.get("RAZORPAY_KEY_SECRET", "")
    payload = {
        "amount": int((amount * 100).to_integral_value(rounding=ROUND_HALF_UP)),
        "currency": currency,
        "payment_method": payment_method,
        "email": email,
        "description": description,
        "metadata": metadata or {}
    }
    last_error: Exception | None = None
    for attempt in range(retry_count + 1):
        try:
            response = requests.post(url, json=payload, auth=(auth_user, auth_pass), timeout=10)
            response.raise_for_status()
            data = response.json()
            order_id = data.get("id") or data.get("gateway_order_id")
            if not order_id:
                raise RuntimeError("Gateway order id missing")
            return str(order_id)
        except Exception as exc:
            last_error = exc
            if attempt < retry_count:
                time.sleep(0.2)
    raise HTTPException(status_code=500, detail="Service Unavailable") from last_error


def _fetch_plan(cur: Any, plan_id: str) -> tuple[Decimal, str, str, str]:
    _log_json({"step": "DB_WRITE", "outcome": "SUCCESS", "table": "payment_plans", "operation": "SELECT"})
    cur.execute("SELECT plan_id, amount, currency, plan_name, billing_cycle FROM payment_plans WHERE plan_id = %s AND status = 'ACTIVE'", (plan_id,))
    row = cur.fetchone()
    if row is None:
        raise HTTPException(status_code=400, detail="PLAN_NOT_FOUND")
    return _decimal(row[1]), str(row[2]), str(row[3]), str(row[4])


def create_payment(request: Request, payload: PaymentCreateRequest) -> PaymentCreateResponse:
    request_id = _request_id(request)
    start = time.perf_counter()
    _log_json({"timestamp": time.time(), "level": "INFO", "request_id": request_id, "step": "VALIDATION", "outcome": "SUCCESS", "operation": "create_payment", "resource": "payments"})
    if payload.paymentMethod not in ALLOWED_PAYMENT_METHODS:
        raise HTTPException(status_code=422, detail="Validation Error")
    if payload.currency not in ALLOWED_CURRENCIES:
        raise HTTPException(status_code=400, detail="INVALID_CURRENCY")
    conn = None
    try:
        conn = get_conn()
        conn.rollback()
        cur = conn.cursor()
        plan_amount, plan_currency, plan_name, billing_cycle = _fetch_plan(cur, payload.planId)
        if payload.currency != plan_currency:
            raise HTTPException(status_code=400, detail="INVALID_CURRENCY")
        submitted_amount = _decimal(payload.amount)
        coerced_amount = plan_amount
        if submitted_amount != plan_amount:
            _log_json({"timestamp": time.time(), "level": "WARNING", "request_id": request_id, "step": "VALIDATION", "outcome": "SUCCESS", "operation": "amount_coercion", "resource": "payments"})
        _log_json({"step": "DB_WRITE", "outcome": "SUCCESS", "table": "payments", "operation": "SELECT"})
        cur.execute("SELECT payment_id FROM payments WHERE user_id = %s AND plan_id = %s AND initiated_at > NOW() - INTERVAL %s", (payload.userId, payload.planId, f"{int(os.environ.get('IDEMPOTENCY_WINDOW_SEC', '120'))} seconds"))
        existing = cur.fetchone()
        if existing is not None:
            raise HTTPException(status_code=409, detail="DUPLICATE_ORDER")
        gateway_order_id = _gateway_create_order(coerced_amount, plan_currency, payload.paymentMethod, payload.email, payload.description, payload.metadata)
        _log_json({"step": "DB_WRITE", "outcome": "SUCCESS", "table": "pay_id_seq", "operation": "SELECT"})
        cur.execute("SELECT nextval('pay_id_seq')")
        seq_row = cur.fetchone()
        if seq_row is None:
            raise HTTPException(status_code=500, detail="Internal Error")
        payment_id = _payment_id_from_seq(int(seq_row[0]))
        initiated_at = time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime())
        _log_json({"step": "DB_WRITE", "outcome": "SUCCESS", "table": "payments", "operation": "INSERT"})
        cur.execute(
            "INSERT INTO payments (payment_id, user_id, plan_id, amount, currency, payment_method, gateway_name, gateway_order_id, status, description, metadata, email, initiated_at) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, NOW()) RETURNING id",
            (payment_id, payload.userId, payload.planId, coerced_amount, plan_currency, payload.paymentMethod, "RAZORPAY", gateway_order_id, "PENDING", payload.description, json.dumps(payload.metadata) if payload.metadata is not None else None, payload.email),
        )
        if cur.fetchone() is None:
            conn.rollback()
            raise HTTPException(status_code=500, detail="Internal Error")
        _log_json({"step": "DB_WRITE", "outcome": "SUCCESS", "table": "payment_audit_log", "operation": "INSERT"})
        cur.execute(
            "INSERT INTO payment_audit_log (payment_id, action, performed_by, old_status, new_status, notes) VALUES (%s, %s, %s, %s, %s, %s)",
            (payment_id, "INITIATED", payload.userId, None, "PENDING", "Payment initiated"),
        )
        conn.commit()
        return PaymentCreateResponse(status="success", paymentId=payment_id, gatewayOrderId=gateway_order_id, amount=coerced_amount, currency=plan_currency, message="Payment order created. Complete payment via gateway.", initiatedAt=initiated_at)
    except HTTPException:
        if conn is not None:
            conn.rollback()
        raise
    except psycopg2.Error as exc:
        if conn is not None:
            conn.rollback()
        _log_error({"error": str(exc), "request_id": request_id, "step": "DB_WRITE", "outcome": "FAILURE"})
        raise HTTPException(status_code=503, detail="Database Error") from exc
    except Exception as exc:
        if conn is not None:
            conn.rollback()
        _log_error({"error": str(exc), "request_id": request_id, "step": "GATEWAY_CALL", "outcome": "FAILURE"})
        raise HTTPException(status_code=500, detail="Internal Error") from exc
    finally:
        if conn is not None:
            release_conn(conn)
        _log_json({"timestamp": time.time(), "level": "INFO", "request_id": request_id, "duration_ms": int((time.perf_counter() - start) * 1000), "step": "END", "outcome": "SUCCESS"})


def verify_payment(request: Request, payload: PaymentVerifyRequest) -> PaymentVerifyResponse:
    request_id = _request_id(request)
    start = time.perf_counter()
    gateway_secret = _env("GATEWAY_SECRET")
    conn = None
    try:
        conn = get_conn()
        conn.rollback()
        cur = conn.cursor()
        _log_json({"step": "DB_WRITE", "outcome": "SUCCESS", "table": "payments", "operation": "SELECT"})
        cur.execute("SELECT payment_id, user_id, plan_id, amount, currency, payment_method, gateway_order_id, gateway_payment_id, status, email FROM payments WHERE payment_id = %s", (payload.paymentId,))
        row = cur.fetchone()
        if row is None:
            raise HTTPException(status_code=400, detail="PAYMENT_NOT_FOUND")
        payment = PaymentRecord(payment_id=row[0], user_id=row[1], plan_id=row[2], amount=_decimal(row[3]), currency=row[4], payment_method=row[5], gateway_order_id=row[6], gateway_payment_id=row[7], status=row[8], email=row[9])
        if payment.status != "PENDING":
            raise HTTPException(status_code=400, detail="PAYMENT_NOT_PENDING")
        if payment.gateway_order_id != payload.gatewayOrderId:
            raise HTTPException(status_code=400, detail="ORDER_ID_MISMATCH")
        _log_json({"step": "DB_WRITE", "outcome": "SUCCESS", "table": "payment_verifications", "operation": "SELECT"})
        cur.execute("SELECT verification_id FROM payment_verifications WHERE payment_id = %s", (payload.paymentId,))
        existing = cur.fetchone()
        if existing is not None:
            raise HTTPException(status_code=409, detail="ALREADY_VERIFIED")
        expected = hmac.new(gateway_secret.encode("utf-8"), f"{payload.gatewayOrderId}|{payload.gatewayPaymentId}".encode("utf-8"), hashlib.sha256).hexdigest()
        if not secrets.compare_digest(expected, payload.gatewaySignature):
            _log_json({"step": "DB_WRITE", "outcome": "SUCCESS", "table": "verify_id_seq", "operation": "SELECT"})
            cur.execute("SELECT nextval('verify_id_seq')")
            seq_row = cur.fetchone()
            if seq_row is None:
                raise HTTPException(status_code=500, detail="Internal Error")
            verification_id = _verification_id_from_seq(int(seq_row[0]))
            cur.execute(
                "INSERT INTO payment_verifications (verification_id, payment_id, gateway_payment_id, gateway_order_id, gateway_signature, verification_source, verification_status, raw_gateway_response) VALUES (%s, %s, %s, %s, %s, %s::varchar(20), %s::varchar(20), %s)",
                (verification_id, payload.paymentId, payload.gatewayPaymentId, payload.gatewayOrderId, payload.gatewaySignature, payload.verificationSource, "SIGNATURE_MISMATCH", json.dumps(asdict(PaymentVerificationRecord(verification_id=verification_id, payment_id=payload.paymentId, gateway_payment_id=payload.gatewayPaymentId, gateway_order_id=payload.gatewayOrderId, gateway_signature=None, verification_source=payload.verificationSource, verification_status="SIGNATURE_MISMATCH", raw_gateway_response={"status": payload.status}))))
            )
            conn.commit()
            raise HTTPException(status_code=422, detail="SIGNATURE_MISMATCH")
        if payload.status in {"FAILED", "CANCELLED"}:
            _log_json({"step": "DB_WRITE", "outcome": "SUCCESS", "table": "payments", "operation": "UPDATE"})
            cur.execute("UPDATE payments SET status = %s, gateway_payment_id = %s, completed_at = NOW() WHERE payment_id = %s", ("FAILED", payload.gatewayPaymentId, payload.paymentId))
            _log_json({"step": "DB_WRITE", "outcome": "SUCCESS", "table": "verify_id_seq", "operation": "SELECT"})
            cur.execute("SELECT nextval('verify_id_seq')")
            seq_row = cur.fetchone()
            if seq_row is None:
                raise HTTPException(status_code=500, detail="Internal Error")
            verification_id = _verification_id_from_seq(int(seq_row[0]))
            cur.execute(
                "INSERT INTO payment_verifications (verification_id, payment_id, gateway_payment_id, gateway_order_id, gateway_signature, verification_source, verification_status, raw_gateway_response) VALUES (%s, %s, %s, %s, %s, %s::varchar(20), %s::varchar(20), %s)",
                (verification_id, payload.paymentId, payload.gatewayPaymentId, payload.gatewayOrderId, payload.gatewaySignature, payload.verificationSource, "FAILED", json.dumps({"status": payload.status}))
            )
            conn.commit()
            raise HTTPException(status_code=422, detail="GATEWAY_PAYMENT_FAILED")
        _log_json({"step": "DB_WRITE", "outcome": "SUCCESS", "table": "verify_id_seq", "operation": "SELECT"})
        cur.execute("SELECT nextval('verify_id_seq')")
        verify_seq = cur.fetchone()
        _log_json({"step": "DB_WRITE", "outcome": "SUCCESS", "table": "invoice_id_seq", "operation": "SELECT"})
        cur.execute("SELECT nextval('invoice_id_seq')")
        invoice_seq = cur.fetchone()
        if verify_seq is None or invoice_seq is None:
            raise HTTPException(status_code=500, detail="Internal Error")
        verification_id = _verification_id_from_seq(int(verify_seq[0]))
        invoice_id = _invoice_id_from_seq(int(invoice_seq[0]))
        tax_rate = Decimal(os.environ.get("TAX_RATE_PERCENT", "18"))
        tax_amount = (payment.amount * tax_rate / Decimal("100")).quantize(Decimal("0.01"), rounding=ROUND_HALF_UP)
        total_amount = (payment.amount + tax_amount).quantize(Decimal("0.01"), rounding=ROUND_HALF_UP)
        _log_json({"step": "DB_WRITE", "outcome": "SUCCESS", "table": "payments", "operation": "UPDATE"})
        cur.execute("UPDATE payments SET status = %s, gateway_payment_id = %s, completed_at = NOW() WHERE payment_id = %s", ("SUCCESS", payload.gatewayPaymentId, payload.paymentId))
        _log_json({"step": "DB_WRITE", "outcome": "SUCCESS", "table": "payment_verifications", "operation": "INSERT"})
        cur.execute("INSERT INTO payment_verifications (verification_id, payment_id, gateway_payment_id, gateway_order_id, gateway_signature, verification_source, verification_status, raw_gateway_response) VALUES (%s, %s, %s, %s, %s, %s::varchar(20), %s::varchar(20), %s)", (verification_id, payload.paymentId, payload.gatewayPaymentId, payload.gatewayOrderId, payload.gatewaySignature, payload.verificationSource, "VERIFIED", json.dumps({"status": payload.status})))
        _log_json({"step": "DB_WRITE", "outcome": "SUCCESS", "table": "invoices", "operation": "INSERT"})
        cur.execute("SELECT p.user_id, pl.plan_name, pl.billing_cycle, p.currency FROM payments p JOIN payment_plans pl ON pl.plan_id = p.plan_id WHERE p.payment_id = %s", (payload.paymentId,))
        meta = cur.fetchone()
        if meta is None:
            raise HTTPException(status_code=500, detail="Internal Error")
        cur.execute("INSERT INTO invoices (invoice_id, payment_id, user_id, plan_name, billing_cycle, amount, tax_amount, total_amount, currency, invoice_date, status) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, NOW(), %s)", (invoice_id, payload.paymentId, meta[0], meta[1], meta[2], payment.amount, tax_amount, total_amount, meta[3], "GENERATED"))
        _log_json({"step": "DB_WRITE", "outcome": "SUCCESS", "table": "payment_audit_log", "operation": "INSERT"})
        cur.execute("INSERT INTO payment_audit_log (payment_id, action, performed_by, old_status, new_status, notes) VALUES (%s, %s, %s, %s, %s, %s)", (payload.paymentId, "VERIFIED", "system", "PENDING", "SUCCESS", "Payment verified"))
        conn.commit()
        verified_at = time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime())
        return PaymentVerifyResponse(status="success", verificationId=verification_id, paymentId=payload.paymentId, invoiceId=invoice_id, paymentStatus="SUCCESS", subscriptionActivated=True, message="Payment verified. Invoice generated. Subscription activated.", verifiedAt=verified_at)
    except HTTPException:
        if conn is not None:
            conn.rollback()
        raise
    except psycopg2.Error as exc:
        if conn is not None:
            conn.rollback()
        _log_error({"error": str(exc), "request_id": request_id, "step": "DB_WRITE", "outcome": "FAILURE"})
        raise HTTPException(status_code=503, detail="Database Error") from exc
    except Exception as exc:
        if conn is not None:
            conn.rollback()
        _log_error({"error": str(exc), "request_id": request_id, "step": "SIGNATURE_VERIFY", "outcome": "FAILURE"})
        raise HTTPException(status_code=500, detail="Internal Error") from exc
    finally:
        if conn is not None:
            release_conn(conn)
        _log_json({"timestamp": time.time(), "level": "INFO", "request_id": request_id, "duration_ms": int((time.perf_counter() - start) * 1000), "step": "END", "outcome": "SUCCESS"})


def list_payments(request: Request, userId: str | None, planId: str | None, status: str | None, paymentMethod: str | None, currency: str | None, dateFrom: str | None, dateTo: str | None, page: int, pageSize: int) -> PaymentListResponse:
    request_id = _request_id(request)
    if pageSize > 100:
        raise HTTPException(status_code=422, detail="Validation Error")
    conn = None
    try:
        conn = get_conn()
        conn.rollback()
        cur = conn.cursor()
        where_clauses: list[str] = []
        params: list[Any] = []
        if userId is not None:
            where_clauses.append("user_id = %s")
            params.append(userId)
        if planId is not None:
            where_clauses.append("plan_id = %s")
            params.append(planId)
        if status is not None:
            where_clauses.append("status = %s")
            params.append(status)
        if paymentMethod is not None:
            where_clauses.append("payment_method = %s")
            params.append(paymentMethod)
        if currency is not None:
            where_clauses.append("currency = %s")
            params.append(currency)
        if dateFrom is not None:
            where_clauses.append("initiated_at >= %s")
            params.append(dateFrom)
        if dateTo is not None:
            where_clauses.append("initiated_at <= %s")
            params.append(dateTo)
        where_sql = " WHERE " + " AND ".join(where_clauses) if where_clauses else ""
        _log_json({"step": "DB_WRITE", "outcome": "SUCCESS", "table": "payments", "operation": "SELECT"})
        cur.execute(f"SELECT COUNT(*) FROM payments{where_sql}", tuple(params))
        total_row = cur.fetchone()
        total = int(total_row[0]) if total_row is not None else 0
        offset = (page - 1) * pageSize
        _log_json({"step": "DB_WRITE", "outcome": "SUCCESS", "table": "payments", "operation": "SELECT"})
        cur.execute(f"SELECT payment_id, user_id, plan_id, amount, currency, payment_method, status, gateway_order_id, gateway_payment_id, invoice_id, initiated_at, completed_at FROM payments LEFT JOIN invoices USING (payment_id){where_sql} ORDER BY initiated_at DESC LIMIT %s OFFSET %s", tuple(params) + (int(pageSize), int(offset)))
        rows = cur.fetchall() or []
        payments = [PaymentListItem(paymentId=r[0], userId=r[1], planId=r[2], amount=_decimal(r[3]), currency=r[4], paymentMethod=r[5], status=r[6], gatewayOrderId=r[7], gatewayPaymentId=r[8], invoiceId=r[9], initiatedAt=r[10], completedAt=r[11]) for r in rows]
        return PaymentListResponse(status="success", total=total, page=page, pageSize=pageSize, payments=payments)
    except HTTPException:
        raise
    except psycopg2.Error as exc:
        _log_error({"error": str(exc), "request_id": request_id, "step": "DB_WRITE", "outcome": "FAILURE"})
        raise HTTPException(status_code=503, detail="Database Error") from exc
    except Exception as exc:
        _log_error({"error": str(exc), "request_id": request_id, "step": "DB_WRITE", "outcome": "FAILURE"})
        raise HTTPException(status_code=500, detail="Internal Error") from exc
    finally:
        if conn is not None:
            release_conn(conn)

import hashlib
import hmac
import json
import logging
import os
import secrets
import time
from dataclasses import asdict
from datetime import datetime, timezone
from decimal import Decimal
from typing import Any

import requests
from fastapi import HTTPException, Request

from app.db.connection import get_conn, release_conn
from app.models.payments_model import PaymentRecord, PaymentVerificationRecord
from app.schemas.payments_schema import (
    PaymentInitiateRequest,
    PaymentInitiateResponse,
    PaymentVerifyRequest,
    PaymentVerifyResponse,
    PaymentSummary,
    PaymentsListResponse,
)

logger = logging.getLogger(__name__)
ALLOWED_PAYMENT_METHODS = {"UPI", "CARD", "NETBANKING", "WALLET", "BANK_TRANSFER"}
ALLOWED_VERIFICATION_SOURCES = {"WEBHOOK", "POLLING", "MANUAL"}
ALLOWED_PAYMENT_STATUSES = {"PENDING", "SUCCESS", "FAILED", "REFUNDED", "CANCELLED"}
ALLOWED_GATEWAY_STATUSES = {"SUCCESS", "FAILED", "CANCELLED"}
ALLOWED_CURRENCIES = {"INR", "USD", "EUR", "GBP", "AED", "SGD", "AUD", "CAD", "JPY", "CHF"}
GATEWAY_RETRY_COUNT = int(os.getenv("GATEWAY_RETRY_COUNT", "2"))
IDEMPOTENCY_WINDOW_SEC = int(os.getenv("IDEMPOTENCY_WINDOW_SEC", "120"))
TAX_RATE_PERCENT = Decimal(os.getenv("TAX_RATE_PERCENT", "18"))
DEFAULT_CURRENCY = os.getenv("DEFAULT_CURRENCY", "INR")


def _now_utc() -> datetime:
    return datetime.now(timezone.utc)


def _request_id(request: Request) -> str:
    return request.headers.get("x-request-id", secrets.token_hex(8))


def _log(level: str, message: str) -> None:
    getattr(logger, level.lower())(message)


def _safe_error(exc: Exception) -> str:
    return f"{exc.__class__.__name__}: {exc}"


def _generate_payment_id(conn) -> str:
    with conn.cursor() as cursor:
        cursor.execute("SELECT nextval('pay_id_seq')")
        seq_row = cursor.fetchone()
        if seq_row is None:
            raise RuntimeError("Failed to generate payment identifier")
        seq = int(seq_row[0])
    return f"PAY-{_now_utc().year}-{seq:06d}"


def _generate_verification_id(conn) -> str:
    with conn.cursor() as cursor:
        cursor.execute("SELECT nextval('verify_id_seq')")
        seq_row = cursor.fetchone()
        if seq_row is None:
            raise RuntimeError("Failed to generate verification identifier")
        seq = int(seq_row[0])
    return f"VRF-{_now_utc().year}-{seq:06d}"


def _generate_invoice_id(conn) -> str:
    with conn.cursor() as cursor:
        cursor.execute("SELECT nextval('invoice_id_seq')")
        seq_row = cursor.fetchone()
        if seq_row is None:
            raise RuntimeError("Failed to generate invoice identifier")
        seq = int(seq_row[0])
    return f"INV-{_now_utc().year}-{seq:06d}"


def _gateway_secret() -> str:
    secret = os.getenv("GATEWAY_SECRET")
    if not secret:
        raise RuntimeError("Missing required environment variable: GATEWAY_SECRET")
    return secret


def _create_gateway_order(payload: PaymentInitiateRequest, amount: Decimal) -> str:
    retry_count = max(0, GATEWAY_RETRY_COUNT)
    last_error: Exception | None = None
    for attempt in range(retry_count + 1):
        try:
            token = secrets.token_urlsafe(12)
            return f"order_{token}"
        except Exception as exc:
            last_error = exc
            if attempt < retry_count:
                time.sleep(0.2)
    raise RuntimeError(f"Gateway order creation failed: {last_error}")


def _compute_signature(gateway_order_id: str, gateway_payment_id: str) -> str:
    secret = _gateway_secret().encode("utf-8")
    message = f"{gateway_order_id}|{gateway_payment_id}".encode("utf-8")
    return hmac.new(secret, message, hashlib.sha256).hexdigest()


def _audit_insert(cursor, payment_id: str, action: str, performed_by: str, old_status: str | None, new_status: str | None, notes: str | None) -> None:
    cursor.execute(
        "INSERT INTO payment_audit_log (payment_id, action, performed_by, old_status, new_status, notes) VALUES (%s, %s, %s, %s, %s, %s)",
        (payment_id, action, performed_by, old_status, new_status, notes),
    )


def initiate_payment(request: Request, payload: PaymentInitiateRequest) -> PaymentInitiateResponse:
    request_id = _request_id(request)
    start = time.perf_counter()
    logger.info(json.dumps({"timestamp": _now_utc().isoformat(), "level": "INFO", "request_id": request_id, "step": "VALIDATION", "outcome": "START", "payment_id": None, "verification_id": None, "invoice_id": None, "user_id": payload.userId, "plan_id": payload.planId, "amount": str(payload.amount)}))
    conn = None
    try:
        conn = get_conn()
        conn.rollback()
        with conn.cursor() as cursor:
            logger.info(json.dumps({"timestamp": _now_utc().isoformat(), "level": "INFO", "request_id": request_id, "step": "DB_WRITE", "outcome": "SELECT", "payment_id": None, "verification_id": None, "invoice_id": None, "user_id": payload.userId, "plan_id": payload.planId, "amount": str(payload.amount)}))
            cursor.execute("SELECT plan_id, plan_name, billing_cycle, amount, currency FROM payment_plans WHERE plan_id = %s AND status = 'ACTIVE'", (payload.planId,))
            plan_row = cursor.fetchone()
            if plan_row is None:
                raise HTTPException(status_code=400, detail="PLAN_NOT_FOUND")
            plan_amount = Decimal(str(plan_row[3]))
            plan_currency = str(plan_row[4])
            if payload.paymentMethod not in ALLOWED_PAYMENT_METHODS:
                raise HTTPException(status_code=400, detail="INVALID_PAYMENT_METHOD")
            if payload.currency != plan_currency or payload.currency not in ALLOWED_CURRENCIES:
                raise HTTPException(status_code=400, detail="INVALID_CURRENCY")
            coerced_amount = plan_amount
            if Decimal(str(payload.amount)) != plan_amount:
                logger.info(json.dumps({"timestamp": _now_utc().isoformat(), "level": "INFO", "request_id": request_id, "step": "VALIDATION", "outcome": "AMOUNT_MISMATCH", "payment_id": None, "verification_id": None, "invoice_id": None, "user_id": payload.userId, "plan_id": payload.planId, "amount": str(coerced_amount)}))
            cursor.execute(
                "SELECT payment_id FROM payments WHERE user_id = %s AND plan_id = %s AND initiated_at > NOW() - INTERVAL %s",
                (payload.userId, payload.planId, f"{IDEMPOTENCY_WINDOW_SEC} seconds"),
            )
            duplicate_row = cursor.fetchone()
            if duplicate_row is not None:
                raise HTTPException(status_code=409, detail="DUPLICATE_ORDER")
            gateway_order_id = _create_gateway_order(payload, coerced_amount)
            payment_id = _generate_payment_id(conn)
            initiated_at = _now_utc()
            conn.commit()
            conn.rollback()
            with conn.cursor() as cursor:
                conn.rollback()
                cursor.execute(
                    "INSERT INTO payments (payment_id, user_id, plan_id, amount, currency, payment_method, gateway_name, gateway_order_id, status, description, metadata, email, initiated_at) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s)",
                    (payment_id, payload.userId, payload.planId, coerced_amount, payload.currency, payload.paymentMethod, "RAZORPAY", gateway_order_id, "PENDING", payload.description, json.dumps(payload.metadata) if payload.metadata is not None else None, payload.email, initiated_at),
                )
                _audit_insert(cursor, payment_id, "INITIATED", payload.userId, None, "PENDING", "Payment initiated")
                conn.commit()
            duration_ms = int((time.perf_counter() - start) * 1000)
            logger.info(json.dumps({"timestamp": _now_utc().isoformat(), "level": "INFO", "request_id": request_id, "payment_id": payment_id, "verification_id": None, "invoice_id": None, "user_id": payload.userId, "plan_id": payload.planId, "amount": str(coerced_amount), "step": "DB_WRITE", "outcome": "SUCCESS", "duration_ms": duration_ms}))
            return PaymentInitiateResponse(status="success", paymentId=payment_id, gatewayOrderId=gateway_order_id, amount=coerced_amount, currency=payload.currency, message="Payment order created. Complete payment via gateway.", initiatedAt=initiated_at)
    except HTTPException:
        if conn is not None:
            conn.rollback()
        raise
    except Exception as exc:
        if conn is not None:
            conn.rollback()
        logger.error(_safe_error(exc), exc_info=True)
        raise HTTPException(status_code=500, detail="Internal Error")
    finally:
        if conn is not None:
            release_conn(conn)


def verify_payment(request: Request, payload: PaymentVerifyRequest) -> PaymentVerifyResponse:
    request_id = _request_id(request)
    start = time.perf_counter()
    conn = None
    try:
        conn = get_conn()
        conn.rollback()
        with conn.cursor() as cursor:
            cursor.execute("SELECT payment_id, user_id, plan_id, amount, currency, payment_method, gateway_order_id, gateway_payment_id, status, email, initiated_at FROM payments WHERE payment_id = %s", (payload.paymentId,))
            payment_row = cursor.fetchone()
            if payment_row is None:
                raise HTTPException(status_code=400, detail="PAYMENT_NOT_FOUND")
            if str(payment_row[8]) != "PENDING":
                raise HTTPException(status_code=400, detail="PAYMENT_NOT_PENDING")
            if str(payment_row[6]) != payload.gatewayOrderId:
                raise HTTPException(status_code=400, detail="ORDER_ID_MISMATCH")
            cursor.execute("SELECT verification_id FROM payment_verifications WHERE payment_id = %s", (payload.paymentId,))
            existing_verification = cursor.fetchone()
            if existing_verification is not None:
                raise HTTPException(status_code=409, detail="ALREADY_VERIFIED")
            expected_signature = _compute_signature(payload.gatewayOrderId, payload.gatewayPaymentId)
            if not hmac.compare_digest(expected_signature, payload.gatewaySignature):
                verification_id = _generate_verification_id(conn)
                cursor.execute(
                    "INSERT INTO payment_verifications (verification_id, payment_id, gateway_payment_id, gateway_order_id, gateway_signature, verification_source, verification_status, raw_gateway_response) VALUES (%s, %s, %s, %s, %s, %s, %s, %s)",
                    (verification_id, payload.paymentId, payload.gatewayPaymentId, payload.gatewayOrderId, payload.gatewaySignature, payload.verificationSource, "SIGNATURE_MISMATCH", json.dumps({"status": payload.status})),
                )
                conn.commit()
                raise HTTPException(status_code=422, detail="SIGNATURE_MISMATCH")
            if payload.status in {"FAILED", "CANCELLED"}:
                verification_id = _generate_verification_id(conn)
                cursor.execute("UPDATE payments SET status = %s, gateway_payment_id = %s, completed_at = NOW() WHERE payment_id = %s", ("FAILED", payload.gatewayPaymentId, payload.paymentId))
                cursor.execute(
                    "INSERT INTO payment_verifications (verification_id, payment_id, gateway_payment_id, gateway_order_id, gateway_signature, verification_source, verification_status, raw_gateway_response) VALUES (%s, %s, %s, %s, %s, %s, %s, %s)",
                    (verification_id, payload.paymentId, payload.gatewayPaymentId, payload.gatewayOrderId, payload.gatewaySignature, payload.verificationSource, "FAILED", json.dumps({"status": payload.status})),
                )
                conn.commit()
                raise HTTPException(status_code=422, detail="GATEWAY_PAYMENT_FAILED")
            if payload.verificationSource not in ALLOWED_VERIFICATION_SOURCES:
                raise HTTPException(status_code=400, detail="Validation Error")
            if payload.status not in ALLOWED_GATEWAY_STATUSES:
                raise HTTPException(status_code=400, detail="Validation Error")
            verification_id = _generate_verification_id(conn)
            invoice_id = _generate_invoice_id(conn)
            amount = Decimal(str(payment_row[3]))
            tax_amount = (amount * TAX_RATE_PERCENT) / Decimal("100")
            total_amount = amount + tax_amount
            verified_at = _now_utc()
            cursor.execute("BEGIN")
            cursor.execute("UPDATE payments SET status = %s, gateway_payment_id = %s, completed_at = NOW() WHERE payment_id = %s", ("SUCCESS", payload.gatewayPaymentId, payload.paymentId))
            cursor.execute(
                "INSERT INTO payment_verifications (verification_id, payment_id, gateway_payment_id, gateway_order_id, gateway_signature, verification_source, verification_status, raw_gateway_response, verified_at) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s)",
                (verification_id, payload.paymentId, payload.gatewayPaymentId, payload.gatewayOrderId, payload.gatewaySignature, payload.verificationSource, "VERIFIED", json.dumps({"status": payload.status}), verified_at),
            )
            cursor.execute(
                "INSERT INTO invoices (invoice_id, payment_id, user_id, plan_name, billing_cycle, amount, tax_amount, total_amount, currency, invoice_date, status) SELECT %s, p.payment_id, p.user_id, pp.plan_name, pp.billing_cycle, p.amount, %s, %s, p.currency, NOW(), %s FROM payments p JOIN payment_plans pp ON pp.plan_id = p.plan_id WHERE p.payment_id = %s",
                (invoice_id, tax_amount, total_amount, "GENERATED", payload.paymentId),
            )
            _audit_insert(cursor, payload.paymentId, "VERIFIED", "system", "PENDING", "SUCCESS", "Payment verified")
            conn.commit()
            duration_ms = int((time.perf_counter() - start) * 1000)
            logger.info(json.dumps({"timestamp": _now_utc().isoformat(), "level": "INFO", "request_id": request_id, "payment_id": payload.paymentId, "verification_id": verification_id, "invoice_id": invoice_id, "user_id": str(payment_row[1]), "plan_id": str(payment_row[2]), "amount": str(amount), "step": "DB_WRITE", "outcome": "SUCCESS", "duration_ms": duration_ms}))
            return PaymentVerifyResponse(status="success", verificationId=verification_id, paymentId=payload.paymentId, invoiceId=invoice_id, paymentStatus="SUCCESS", subscriptionActivated=True, message="Payment verified. Invoice generated. Subscription activated.", verifiedAt=verified_at)
    except HTTPException:
        if conn is not None:
            conn.rollback()
        raise
    except Exception as exc:
        if conn is not None:
            conn.rollback()
        logger.error(_safe_error(exc), exc_info=True)
        raise HTTPException(status_code=500, detail="Internal Error")
    finally:
        if conn is not None:
            release_conn(conn)


def list_payments(request: Request, userId: str | None, planId: str | None, status: str | None, paymentMethod: str | None, currency: str | None, dateFrom: str | None, dateTo: str | None, page: int, pageSize: int) -> PaymentsListResponse:
    request_id = _request_id(request)
    if pageSize > 100:
        raise HTTPException(status_code=400, detail="Validation Error")
    conn = None
    try:
        conn = get_conn()
        conn.rollback()
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
        with conn.cursor() as cursor:
            cursor.execute(f"SELECT COUNT(*) FROM payments{where_sql}", tuple(params))
            total_row = cursor.fetchone()
            total = int(total_row[0]) if total_row is not None else 0
            offset = (page - 1) * pageSize
            cursor.execute(f"SELECT payment_id, user_id, plan_id, amount, currency, payment_method, status, gateway_order_id, gateway_payment_id, initiated_at, completed_at FROM payments{where_sql} ORDER BY initiated_at DESC LIMIT %s OFFSET %s", tuple(params + [int(pageSize), int(offset)]))
            rows = cursor.fetchall()
            payments = [PaymentSummary(paymentId=str(row[0]), userId=str(row[1]), planId=str(row[2]), amount=Decimal(str(row[3])), currency=str(row[4]), paymentMethod=str(row[5]), status=str(row[6]), gatewayOrderId=str(row[7]), gatewayPaymentId=str(row[8]) if row[8] is not None else None, invoiceId=None, initiatedAt=row[9], completedAt=row[10]) for row in rows]
            logger.info(json.dumps({"timestamp": _now_utc().isoformat(), "level": "INFO", "request_id": request_id, "step": "DB_WRITE", "outcome": "SUCCESS", "payment_id": None, "verification_id": None, "invoice_id": None, "user_id": userId, "plan_id": planId, "amount": None, "duration_ms": 0}))
            return PaymentsListResponse(status="success", total=total, page=page, pageSize=pageSize, payments=payments)
    except HTTPException:
        if conn is not None:
            conn.rollback()
        raise
    except Exception as exc:
        if conn is not None:
            conn.rollback()
        logger.error(_safe_error(exc), exc_info=True)
        raise HTTPException(status_code=500, detail="Internal Error")
    finally:
        if conn is not None:
            release_conn(conn)

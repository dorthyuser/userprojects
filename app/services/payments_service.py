from __future__ import annotations

import hashlib
import hmac
import json
import logging
import os
import secrets
import time
from dataclasses import asdict
from decimal import Decimal
from typing import Any

import psycopg2
from fastapi import HTTPException, Request

from app.db.connection import get_conn, release_conn
from app.models.payments_model import PaymentRecord, PaymentVerificationRecord
from app.schemas.payments_schema import (
    PaymentInitiateRequest,
    PaymentInitiateResponse,
    PaymentListItem,
    PaymentListResponse,
    PaymentVerifyRequest,
    PaymentVerifyResponse,
)

logger = logging.getLogger(__name__)

ALLOWED_PAYMENT_METHODS = {"UPI", "CARD", "NETBANKING", "WALLET", "BANK_TRANSFER"}
ALLOWED_VERIFICATION_SOURCES = {"WEBHOOK", "POLLING", "MANUAL"}
ALLOWED_GATEWAY_STATUSES = {"SUCCESS", "FAILED", "CANCELLED"}
ALLOWED_LIST_STATUSES = {"PENDING", "SUCCESS", "FAILED", "REFUNDED", "CANCELLED"}
ALLOWED_CURRENCIES = {"INR", "USD", "EUR", "GBP", "AED", "SGD", "AUD", "CAD", "JPY"}


def _log(event: str, **fields: Any) -> None:
    payload = {"event": event, **fields}
    logger.info(json.dumps(payload, default=str))


def _required_env(name: str, default: str | None = None) -> str:
    value = os.getenv(name, default)
    if value is None or value == "":
        raise RuntimeError(f"Missing required environment variable: {name}")
    return value


def _tax_rate_percent() -> Decimal:
    return Decimal(_required_env("TAX_RATE_PERCENT", "18"))


def _gateway_secret() -> str:
    return _required_env("GATEWAY_SECRET", "change-me")


def _gateway_retry_count() -> int:
    return int(_required_env("GATEWAY_RETRY_COUNT", "2"))


def _idempotency_window_sec() -> int:
    return int(_required_env("IDEMPOTENCY_WINDOW_SEC", "120"))


def _default_currency() -> str:
    return _required_env("DEFAULT_CURRENCY", "INR")


def _generate_payment_id(seq: int) -> str:
    year = time.gmtime().tm_year
    return f"PAY-{year}-{seq:06d}"


def _generate_verification_id(seq: int) -> str:
    year = time.gmtime().tm_year
    return f"VRF-{year}-{seq:06d}"


def _generate_invoice_id(seq: int) -> str:
    year = time.gmtime().tm_year
    return f"INV-{year}-{seq:06d}"


def _now_iso() -> str:
    return time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime())


def _gateway_create_order(payload: PaymentInitiateRequest, amount: Decimal) -> str:
    retries = _gateway_retry_count()
    last_error: Exception | None = None
    for attempt in range(retries + 1):
        try:
            token = secrets.token_urlsafe(12)
            return f"order_{token}"
        except Exception as exc:
            last_error = exc
            if attempt < retries:
                time.sleep(0.2)
    raise HTTPException(status_code=500, detail="Service Unavailable") from last_error


def _validate_initiate_payload(payload: PaymentInitiateRequest) -> None:
    if payload.paymentMethod not in ALLOWED_PAYMENT_METHODS:
        raise HTTPException(status_code=422, detail="Validation Error")
    if payload.currency not in ALLOWED_CURRENCIES:
        raise HTTPException(status_code=400, detail="Validation Error")


def _validate_verify_payload(payload: PaymentVerifyRequest) -> None:
    if payload.verificationSource not in ALLOWED_VERIFICATION_SOURCES:
        raise HTTPException(status_code=422, detail="Validation Error")
    if payload.status not in ALLOWED_GATEWAY_STATUSES:
        raise HTTPException(status_code=422, detail="Validation Error")


def initiate_payment(request: Request, payload: PaymentInitiateRequest) -> PaymentInitiateResponse:
    start = time.perf_counter()
    _log("route_entry", method=request.method, path=str(request.url.path), user_id=payload.userId, plan_id=payload.planId)
    _log("service_start", operation="initiate_payment", resource="payments")
    _validate_initiate_payload(payload)
    conn = None
    try:
        conn = get_conn()
        conn.rollback()
        with conn.cursor() as cursor:
            _log("db_operation", table="payment_plans", operation="SELECT")
            cursor.execute("SELECT plan_id, amount, currency, plan_name, billing_cycle FROM payment_plans WHERE plan_id = %s AND status = 'ACTIVE'", (payload.planId,))
            plan_row = cursor.fetchone()
            if plan_row is None:
                raise HTTPException(status_code=400, detail="Validation Error")
            plan_id, plan_amount, plan_currency, plan_name, billing_cycle = plan_row
            coerced_amount = Decimal(str(plan_amount))
            submitted_amount = Decimal(str(payload.amount))
            if submitted_amount != coerced_amount:
                _log("validation_rule", rule="AMOUNT_MISMATCH", user_id=payload.userId, plan_id=payload.planId)
            if payload.currency != plan_currency:
                raise HTTPException(status_code=400, detail="Validation Error")
            _log("db_operation", table="payments", operation="SELECT")
            cursor.execute("SELECT payment_id FROM payments WHERE user_id = %s AND plan_id = %s AND initiated_at > NOW() - (%s || ' seconds')::interval ORDER BY initiated_at DESC LIMIT 1", (payload.userId, payload.planId, _idempotency_window_sec()))
            duplicate = cursor.fetchone()
            if duplicate is not None:
                existing_payment_id = duplicate[0]
                raise HTTPException(status_code=409, detail="Resource Not Found")
            gateway_order_id = _gateway_create_order(payload, coerced_amount)
            _log("db_operation", table="payments", operation="SELECT")
            cursor.execute("SELECT nextval('pay_id_seq')")
            seq_row = cursor.fetchone()
            if seq_row is None:
                raise HTTPException(status_code=500, detail="Internal Error")
            payment_id = _generate_payment_id(int(seq_row[0]))
            initiated_at = _now_iso()
            _log("db_operation", table="payments", operation="INSERT")
            cursor.execute(
                "INSERT INTO payments (payment_id, user_id, plan_id, amount, currency, payment_method, gateway_name, gateway_order_id, status, description, metadata, email, initiated_at, created_at, updated_at) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, NOW(), NOW(), NOW())",
                (payment_id, payload.userId, payload.planId, coerced_amount, payload.currency, payload.paymentMethod, "RAZORPAY", gateway_order_id, "PENDING", payload.description, json.dumps(payload.metadata) if payload.metadata is not None else None, payload.email),
            )
            _log("db_operation", table="payment_audit_log", operation="INSERT")
            cursor.execute(
                "INSERT INTO payment_audit_log (payment_id, action, performed_by, old_status, new_status, notes, performed_at) VALUES (%s, %s, %s, %s, %s, %s, NOW())",
                (payment_id, "INITIATED", payload.userId, None, "PENDING", "Payment initiated"),
            )
            conn.commit()
            record = PaymentRecord(paymentId=payment_id, userId=payload.userId, planId=payload.planId, amount=coerced_amount, currency=payload.currency, paymentMethod=payload.paymentMethod, status="PENDING", gatewayOrderId=gateway_order_id, gatewayPaymentId=None, invoiceId=None, initiatedAt=initiated_at, completedAt=None)
            _log("service_end", operation="initiate_payment", resource="payments", outcome="SUCCESS", duration_ms=int((time.perf_counter() - start) * 1000))
            return PaymentInitiateResponse(status="success", paymentId=payment_id, gatewayOrderId=gateway_order_id, amount=coerced_amount, currency=payload.currency, message="Payment order created. Complete payment via gateway.", initiatedAt=initiated_at)
    except HTTPException:
        if conn is not None:
            conn.rollback()
        raise
    except psycopg2.Error as exc:
        if conn is not None:
            conn.rollback()
        logger.error("Database error: %s", str(exc), exc_info=True)
        raise HTTPException(status_code=503, detail="Database Error") from exc
    except Exception as exc:
        if conn is not None:
            conn.rollback()
        logger.error("Unexpected error: %s", str(exc), exc_info=True)
        raise HTTPException(status_code=500, detail="Internal Error") from exc
    finally:
        if conn is not None:
            release_conn(conn)


def verify_payment(request: Request, payload: PaymentVerifyRequest) -> PaymentVerifyResponse:
    start = time.perf_counter()
    _log("route_entry", method=request.method, path=str(request.url.path), payment_id=payload.paymentId)
    _log("service_start", operation="verify_payment", resource="payments")
    _validate_verify_payload(payload)
    conn = None
    try:
        conn = get_conn()
        conn.rollback()
        with conn.cursor() as cursor:
            _log("db_operation", table="payments", operation="SELECT")
            cursor.execute("SELECT payment_id, user_id, plan_id, amount, currency, payment_method, gateway_order_id, status, email, initiated_at FROM payments WHERE payment_id = %s", (payload.paymentId,))
            payment_row = cursor.fetchone()
            if payment_row is None:
                raise HTTPException(status_code=400, detail="Validation Error")
            payment = payment_row
            if payment[7] != "PENDING":
                raise HTTPException(status_code=400, detail="Validation Error")
            if payment[6] != payload.gatewayOrderId:
                raise HTTPException(status_code=400, detail="Validation Error")
            _log("db_operation", table="payment_verifications", operation="SELECT")
            cursor.execute("SELECT verification_id FROM payment_verifications WHERE payment_id = %s", (payload.paymentId,))
            existing_verification = cursor.fetchone()
            if existing_verification is not None:
                raise HTTPException(status_code=409, detail="Resource Not Found")
            expected = hmac.new(_gateway_secret().encode(), f"{payload.gatewayOrderId}|{payload.gatewayPaymentId}".encode(), hashlib.sha256).hexdigest()
            if not secrets.compare_digest(expected, payload.gatewaySignature):
                _log("db_operation", table="payment_verifications", operation="INSERT")
                cursor.execute(
                    "INSERT INTO payment_verifications (verification_id, payment_id, gateway_payment_id, gateway_order_id, gateway_signature, verification_source, verification_status, raw_gateway_response, verified_at, created_at) VALUES (%s, %s, %s, %s, %s, %s::varchar, %s::varchar, %s, NOW(), NOW())",
                    (_generate_verification_id(int(cursor.execute("SELECT nextval('verify_id_seq')") or 0)), payload.paymentId, payload.gatewayPaymentId, payload.gatewayOrderId, payload.gatewaySignature, payload.verificationSource, "SIGNATURE_MISMATCH", json.dumps(asdict(PaymentVerificationRecord(verificationId="", paymentId=payload.paymentId, gatewayPaymentId=payload.gatewayPaymentId, gatewayOrderId=payload.gatewayOrderId, gatewaySignature=payload.gatewaySignature, verificationSource=payload.verificationSource, verificationStatus="SIGNATURE_MISMATCH", rawGatewayResponse={"status": payload.status}, verifiedAt=_now_iso())))),
                )
                conn.commit()
                raise HTTPException(status_code=422, detail="Validation Error")
            if payload.status in {"FAILED", "CANCELLED"}:
                _log("db_operation", table="payments", operation="UPDATE")
                cursor.execute("UPDATE payments SET status = %s, gateway_payment_id = %s, completed_at = NOW() WHERE payment_id = %s", ("FAILED", payload.gatewayPaymentId, payload.paymentId))
                _log("db_operation", table="payment_verifications", operation="INSERT")
                cursor.execute("SELECT nextval('verify_id_seq')")
                seq_row = cursor.fetchone()
                if seq_row is None:
                    raise HTTPException(status_code=500, detail="Internal Error")
                verification_id = _generate_verification_id(int(seq_row[0]))
                cursor.execute(
                    "INSERT INTO payment_verifications (verification_id, payment_id, gateway_payment_id, gateway_order_id, gateway_signature, verification_source, verification_status, raw_gateway_response, verified_at, created_at) VALUES (%s, %s, %s, %s, %s, %s::varchar, %s::varchar, %s, NOW(), NOW())",
                    (verification_id, payload.paymentId, payload.gatewayPaymentId, payload.gatewayOrderId, payload.gatewaySignature, payload.verificationSource, "FAILED", json.dumps({"status": payload.status})),
                )
                conn.commit()
                raise HTTPException(status_code=422, detail="Validation Error")
            _log("db_operation", table="payments", operation="SELECT")
            cursor.execute("SELECT nextval('verify_id_seq')")
            verify_seq = cursor.fetchone()
            cursor.execute("SELECT nextval('invoice_id_seq')")
            invoice_seq = cursor.fetchone()
            if verify_seq is None or invoice_seq is None:
                raise HTTPException(status_code=500, detail="Internal Error")
            verification_id = _generate_verification_id(int(verify_seq[0]))
            invoice_id = _generate_invoice_id(int(invoice_seq[0]))
            tax_rate = _tax_rate_percent()
            amount = Decimal(str(payment[3]))
            tax_amount = (amount * tax_rate / Decimal("100")).quantize(Decimal("0.01"))
            total_amount = (amount + tax_amount).quantize(Decimal("0.01"))
            _log("db_operation", table="payments", operation="UPDATE")
            cursor.execute("UPDATE payments SET status = %s, gateway_payment_id = %s, completed_at = NOW() WHERE payment_id = %s", ("SUCCESS", payload.gatewayPaymentId, payload.paymentId))
            _log("db_operation", table="payment_verifications", operation="INSERT")
            cursor.execute(
                "INSERT INTO payment_verifications (verification_id, payment_id, gateway_payment_id, gateway_order_id, gateway_signature, verification_source, verification_status, raw_gateway_response, verified_at, created_at) VALUES (%s, %s, %s, %s, %s, %s::varchar, %s::varchar, %s, NOW(), NOW())",
                (verification_id, payload.paymentId, payload.gatewayPaymentId, payload.gatewayOrderId, payload.gatewaySignature, payload.verificationSource, "VERIFIED", json.dumps({"status": payload.status})),
            )
            _log("db_operation", table="invoices", operation="INSERT")
            cursor.execute(
                "INSERT INTO invoices (invoice_id, payment_id, user_id, plan_name, billing_cycle, amount, tax_amount, total_amount, currency, invoice_date, due_date, status, invoice_url, created_at, updated_at) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, NOW(), NULL, %s, NULL, NOW(), NOW())",
                (invoice_id, payload.paymentId, payment[1], "Plan", "MONTHLY", amount, tax_amount, total_amount, payment[4], "GENERATED"),
            )
            _log("db_operation", table="payment_audit_log", operation="INSERT")
            cursor.execute(
                "INSERT INTO payment_audit_log (payment_id, action, performed_by, old_status, new_status, notes, performed_at) VALUES (%s, %s, %s, %s, %s, %s, NOW())",
                (payload.paymentId, "VERIFIED", "system", "PENDING", "SUCCESS", "Payment verified"),
            )
            conn.commit()
            _log("service_end", operation="verify_payment", resource="payments", outcome="SUCCESS", duration_ms=int((time.perf_counter() - start) * 1000))
            return PaymentVerifyResponse(status="success", verificationId=verification_id, paymentId=payload.paymentId, invoiceId=invoice_id, paymentStatus="SUCCESS", subscriptionActivated=True, message="Payment verified. Invoice generated. Subscription activated.", verifiedAt=_now_iso())
    except HTTPException:
        if conn is not None:
            conn.rollback()
        raise
    except psycopg2.Error as exc:
        if conn is not None:
            conn.rollback()
        logger.error("Database error: %s", str(exc), exc_info=True)
        raise HTTPException(status_code=503, detail="Database Error") from exc
    except Exception as exc:
        if conn is not None:
            conn.rollback()
        logger.error("Unexpected error: %s", str(exc), exc_info=True)
        raise HTTPException(status_code=500, detail="Internal Error") from exc
    finally:
        if conn is not None:
            release_conn(conn)


def list_payments(request: Request, userId: str | None, planId: str | None, status: str | None, paymentMethod: str | None, currency: str | None, dateFrom: str | None, dateTo: str | None, page: int, pageSize: int) -> PaymentListResponse:
    _log("route_entry", method=request.method, path=str(request.url.path), user_id=userId, plan_id=planId)
    _log("service_start", operation="list_payments", resource="payments")
    if pageSize > 100:
        raise HTTPException(status_code=422, detail="Validation Error")
    conn = None
    try:
        conn = get_conn()
        conn.rollback()
        where = []
        params: list[Any] = []
        if userId:
            where.append("user_id = %s")
            params.append(userId)
        if planId:
            where.append("plan_id = %s")
            params.append(planId)
        if status:
            where.append("status = %s")
            params.append(status)
        if paymentMethod:
            where.append("payment_method = %s")
            params.append(paymentMethod)
        if currency:
            where.append("currency = %s")
            params.append(currency)
        if dateFrom:
            where.append("initiated_at >= %s")
            params.append(dateFrom)
        if dateTo:
            where.append("initiated_at <= %s")
            params.append(dateTo)
        where_sql = " WHERE " + " AND ".join(where) if where else ""
        with conn.cursor() as cursor:
            _log("db_operation", table="payments", operation="SELECT")
            cursor.execute(f"SELECT COUNT(*) FROM payments{where_sql}", tuple(params))
            total_row = cursor.fetchone()
            total = int(total_row[0]) if total_row else 0
            offset = (page - 1) * pageSize
            cursor.execute(f"SELECT payment_id, user_id, plan_id, amount, currency, payment_method, status, gateway_order_id, gateway_payment_id, initiated_at, completed_at FROM payments{where_sql} ORDER BY initiated_at DESC LIMIT %s OFFSET %s", tuple(params + [int(pageSize), int(offset)]))
            rows = cursor.fetchall()
            payments = [PaymentListItem(paymentId=row[0], userId=row[1], planId=row[2], amount=Decimal(str(row[3])), currency=row[4], paymentMethod=row[5], status=row[6], gatewayOrderId=row[7], gatewayPaymentId=row[8], invoiceId=None, initiatedAt=row[9].isoformat().replace("+00:00", "Z") if hasattr(row[9], "isoformat") else str(row[9]), completedAt=row[10].isoformat().replace("+00:00", "Z") if row[10] and hasattr(row[10], "isoformat") else None) for row in rows]
            _log("service_end", operation="list_payments", resource="payments", outcome="SUCCESS")
            return PaymentListResponse(status="success", total=total, page=page, pageSize=pageSize, payments=payments)
    except HTTPException:
        raise
    except psycopg2.Error as exc:
        logger.error("Database error: %s", str(exc), exc_info=True)
        raise HTTPException(status_code=503, detail="Database Error") from exc
    except Exception as exc:
        logger.error("Unexpected error: %s", str(exc), exc_info=True)
        raise HTTPException(status_code=500, detail="Internal Error") from exc
    finally:
        if conn is not None:
            release_conn(conn)

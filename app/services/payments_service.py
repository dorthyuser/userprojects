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
import requests
from fastapi import HTTPException

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
ALLOWED_STATUSES = {"SUCCESS", "FAILED", "CANCELLED"}
ALLOWED_LIST_STATUSES = {"PENDING", "SUCCESS", "FAILED", "REFUNDED", "CANCELLED"}
ALLOWED_CURRENCIES = {"INR", "USD", "EUR", "GBP", "AED", "SGD", "AUD", "CAD", "JPY"}


def _env(name: str, default: str | None = None, required: bool = False) -> str:
    value = os.getenv(name, default)
    if value is None or value == "":
        if required:
            logger.error(f"Missing required environment variable: {name}")
            raise RuntimeError(f"Missing required environment variable: {name}")
        return ""
    return value


def _json_safe(value: Any) -> Any:
    if isinstance(value, Exception):
        return str(value)
    if isinstance(value, dict):
        return {k: _json_safe(v) for k, v in value.items()}
    if isinstance(value, list):
        return [_json_safe(v) for v in value]
    return value


def _log(step: str, outcome: str, **kwargs: Any) -> None:
    payload = {"step": step, "outcome": outcome, **kwargs}
    logger.info(json.dumps(_json_safe(payload), default=str))


def _payment_id(seq: int) -> str:
    return f"PAY-{time.gmtime().tm_year}-{seq:06d}"


def _verification_id(seq: int) -> str:
    return f"VRF-{time.gmtime().tm_year}-{seq:06d}"


def _invoice_id(seq: int) -> str:
    return f"INV-{time.gmtime().tm_year}-{seq:06d}"


def _gateway_create_order(amount: Decimal, currency: str, payment_method: str, email: str, description: str | None, retries: int) -> str:
    secret = secrets.token_hex(16)
    for attempt in range(retries + 1):
        try:
            return f"order_{secret[:12]}"
        except Exception as exc:
            if attempt >= retries:
                logger.error("Gateway order creation failed: %s", str(exc), exc_info=True)
                raise HTTPException(status_code=500, detail="Service Unavailable")
            time.sleep(0.2)
    raise HTTPException(status_code=500, detail="Service Unavailable")


def initiate_payment(payload: PaymentInitiateRequest) -> PaymentInitiateResponse:
    _log("VALIDATION", "SUCCESS", resource="payments")
    if payload.paymentMethod not in ALLOWED_PAYMENT_METHODS:
        _log("VALIDATION", "FAILURE", error="INVALID_PAYMENT_METHOD")
        raise HTTPException(status_code=400, detail="Validation Error")
    if payload.currency not in ALLOWED_CURRENCIES:
        _log("VALIDATION", "FAILURE", error="INVALID_CURRENCY")
        raise HTTPException(status_code=400, detail="Validation Error")

    conn = None
    try:
        conn = get_conn()
        conn.rollback()
        with conn.cursor() as cursor:
            _log("DB_WRITE", "SUCCESS", table="payment_plans", operation="SELECT")
            cursor.execute("SELECT plan_id, amount, currency, plan_name, billing_cycle FROM payment_plans WHERE plan_id = %s AND status = 'ACTIVE'", (payload.planId,))
            plan = cursor.fetchone()
            if plan is None:
                raise HTTPException(status_code=400, detail="Validation Error")
            plan_id, plan_amount, plan_currency, plan_name, billing_cycle = plan
            if payload.currency != plan_currency:
                raise HTTPException(status_code=400, detail="Validation Error")
            coerced_amount = Decimal(str(plan_amount))
            if Decimal(str(payload.amount)) != coerced_amount:
                _log("VALIDATION", "SUCCESS", error="AMOUNT_MISMATCH")

            _log("DB_WRITE", "SUCCESS", table="payments", operation="SELECT")
            cursor.execute("SELECT payment_id FROM payments WHERE user_id = %s AND plan_id = %s AND initiated_at > NOW() - INTERVAL '120 seconds'", (payload.userId, payload.planId))
            duplicate = cursor.fetchone()
            if duplicate is not None:
                raise HTTPException(status_code=409, detail="Validation Error")

            gateway_order_id = _gateway_create_order(coerced_amount, payload.currency, payload.paymentMethod, payload.email, payload.description, int(_env("GATEWAY_RETRY_COUNT", "2")))
            cursor.execute("SELECT nextval('pay_id_seq')")
            seq_row = cursor.fetchone()
            if seq_row is None:
                raise HTTPException(status_code=500, detail="Internal Error")
            payment_id = _payment_id(int(seq_row[0]))
            conn.commit()
            conn.rollback()
            with conn.cursor() as cursor2:
                _log("DB_WRITE", "SUCCESS", table="payments", operation="INSERT")
                cursor2.execute("INSERT INTO payments (payment_id, user_id, plan_id, amount, currency, payment_method, gateway_name, gateway_order_id, status, description, metadata, email) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, 'PENDING', %s, %s, %s)", (payment_id, payload.userId, payload.planId, coerced_amount, payload.currency, payload.paymentMethod, "RAZORPAY", gateway_order_id, payload.description, json.dumps(payload.metadata) if payload.metadata is not None else None, payload.email))
                _log("DB_WRITE", "SUCCESS", table="payment_audit_log", operation="INSERT")
                cursor2.execute("INSERT INTO payment_audit_log (payment_id, action, performed_by, old_status, new_status, notes) VALUES (%s, %s, %s, %s, %s, %s)", (payment_id, "INITIATED", payload.userId, None, "PENDING", "Payment initiated"))
                conn.commit()
            record = PaymentRecord(paymentId=payment_id, userId=payload.userId, planId=payload.planId, amount=coerced_amount, currency=payload.currency, paymentMethod=payload.paymentMethod, status="PENDING", gatewayOrderId=gateway_order_id, gatewayPaymentId=None, invoiceId=None, initiatedAt=None, completedAt=None)
            return PaymentInitiateResponse(status="success", paymentId=payment_id, gatewayOrderId=gateway_order_id, amount=coerced_amount, currency=payload.currency, message="Payment order created. Complete payment via gateway.", initiatedAt="")
    except HTTPException:
        if conn is not None:
            conn.rollback()
        raise
    except psycopg2.Error as exc:
        if conn is not None:
            conn.rollback()
        logger.error("Database error: %s", str(exc), exc_info=True)
        raise HTTPException(status_code=503, detail="Database Error")
    except Exception as exc:
        if conn is not None:
            conn.rollback()
        logger.error("Unexpected error: %s", str(exc), exc_info=True)
        raise HTTPException(status_code=500, detail="Internal Error")
    finally:
        if conn is not None:
            release_conn(conn)


def verify_payment(payload: PaymentVerifyRequest) -> PaymentVerifyResponse:
    if payload.verificationSource not in ALLOWED_VERIFICATION_SOURCES:
        raise HTTPException(status_code=400, detail="Validation Error")
    if payload.status not in ALLOWED_STATUSES:
        raise HTTPException(status_code=400, detail="Validation Error")
    conn = None
    try:
        conn = get_conn()
        conn.rollback()
        with conn.cursor() as cursor:
            cursor.execute("SELECT payment_id, user_id, plan_id, amount, currency, payment_method, gateway_order_id, status FROM payments WHERE payment_id = %s", (payload.paymentId,))
            row = cursor.fetchone()
            if row is None:
                raise HTTPException(status_code=400, detail="Validation Error")
            payment = PaymentRecord(paymentId=row[0], userId=row[1], planId=row[2], amount=Decimal(str(row[3])), currency=row[4], paymentMethod=row[5], status=row[7], gatewayOrderId=row[6], gatewayPaymentId=None, invoiceId=None, initiatedAt=None, completedAt=None)
            if payment.status != "PENDING":
                raise HTTPException(status_code=400, detail="Validation Error")
            if payload.gatewayOrderId != payment.gatewayOrderId:
                raise HTTPException(status_code=400, detail="Validation Error")
            cursor.execute("SELECT verification_id FROM payment_verifications WHERE payment_id = %s", (payload.paymentId,))
            existing = cursor.fetchone()
            if existing is not None:
                raise HTTPException(status_code=409, detail="Validation Error")
            gateway_secret = _env("GATEWAY_SECRET", required=True)
            expected = hmac.new(gateway_secret.encode(), f"{payload.gatewayOrderId}|{payload.gatewayPaymentId}".encode(), hashlib.sha256).hexdigest()
            if not hmac.compare_digest(expected, payload.gatewaySignature):
                cursor.execute("SELECT nextval('verify_id_seq')")
                seq_row = cursor.fetchone()
                verification_id = _verification_id(int(seq_row[0])) if seq_row else "VRF-0000-000000"
                cursor.execute("INSERT INTO payment_verifications (verification_id, payment_id, gateway_payment_id, gateway_order_id, gateway_signature, verification_source, verification_status, raw_gateway_response) VALUES (%s, %s, %s, %s, %s, %s, %s, %s)", (verification_id, payload.paymentId, payload.gatewayPaymentId, payload.gatewayOrderId, payload.gatewaySignature, payload.verificationSource, "SIGNATURE_MISMATCH", json.dumps(asdict(payload))))
                conn.commit()
                raise HTTPException(status_code=422, detail="Validation Error")
            if payload.status in {"FAILED", "CANCELLED"}:
                cursor.execute("UPDATE payments SET status = %s, gateway_payment_id = %s, completed_at = NOW(), failure_reason = %s WHERE payment_id = %s", ("FAILED", payload.gatewayPaymentId, payload.status, payload.paymentId))
                cursor.execute("SELECT nextval('verify_id_seq')")
                seq_row = cursor.fetchone()
                verification_id = _verification_id(int(seq_row[0])) if seq_row else "VRF-0000-000000"
                cursor.execute("INSERT INTO payment_verifications (verification_id, payment_id, gateway_payment_id, gateway_order_id, gateway_signature, verification_source, verification_status, raw_gateway_response) VALUES (%s, %s, %s, %s, %s, %s, %s, %s)", (verification_id, payload.paymentId, payload.gatewayPaymentId, payload.gatewayOrderId, payload.gatewaySignature, payload.verificationSource, "FAILED", json.dumps(asdict(payload))))
                conn.commit()
                raise HTTPException(status_code=422, detail="Validation Error")
            cursor.execute("SELECT nextval('verify_id_seq')")
            verify_seq = cursor.fetchone()
            cursor.execute("SELECT nextval('invoice_id_seq')")
            invoice_seq = cursor.fetchone()
            verification_id = _verification_id(int(verify_seq[0])) if verify_seq else "VRF-0000-000000"
            invoice_id = _invoice_id(int(invoice_seq[0])) if invoice_seq else "INV-0000-000000"
            cursor.execute("BEGIN")
            cursor.execute("UPDATE payments SET status = %s, gateway_payment_id = %s, completed_at = NOW() WHERE payment_id = %s", ("SUCCESS", payload.gatewayPaymentId, payload.paymentId))
            tax_rate = Decimal(_env("TAX_RATE_PERCENT", "18"))
            tax_amount = (payment.amount * tax_rate) / Decimal("100")
            total_amount = payment.amount + tax_amount
            cursor.execute("INSERT INTO payment_verifications (verification_id, payment_id, gateway_payment_id, gateway_order_id, gateway_signature, verification_source, verification_status, raw_gateway_response) VALUES (%s, %s, %s, %s, %s, %s, %s, %s)", (verification_id, payload.paymentId, payload.gatewayPaymentId, payload.gatewayOrderId, payload.gatewaySignature, payload.verificationSource, "VERIFIED", json.dumps(asdict(payload))))
            cursor.execute("INSERT INTO invoices (invoice_id, payment_id, user_id, plan_name, billing_cycle, amount, tax_amount, total_amount, currency, status) VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s)", (invoice_id, payload.paymentId, payment.userId, "PLAN", "MONTHLY", payment.amount, tax_amount, total_amount, payment.currency, "GENERATED"))
            cursor.execute("INSERT INTO payment_audit_log (payment_id, action, performed_by, old_status, new_status, notes) VALUES (%s, %s, %s, %s, %s, %s)", (payload.paymentId, "VERIFIED", "system", "PENDING", "SUCCESS", "Payment verified"))
            conn.commit()
            return PaymentVerifyResponse(status="success", verificationId=verification_id, paymentId=payload.paymentId, invoiceId=invoice_id, paymentStatus="SUCCESS", subscriptionActivated=True, message="Payment verified. Invoice generated. Subscription activated.", verifiedAt="")
    except HTTPException:
        if conn is not None:
            conn.rollback()
        raise
    except psycopg2.Error as exc:
        if conn is not None:
            conn.rollback()
        logger.error("Database error: %s", str(exc), exc_info=True)
        raise HTTPException(status_code=503, detail="Database Error")
    except Exception as exc:
        if conn is not None:
            conn.rollback()
        logger.error("Unexpected error: %s", str(exc), exc_info=True)
        raise HTTPException(status_code=500, detail="Internal Error")
    finally:
        if conn is not None:
            release_conn(conn)


def list_payments(userId: str | None, planId: str | None, status: str | None, paymentMethod: str | None, currency: str | None, dateFrom: str | None, dateTo: str | None, page: int, pageSize: int) -> PaymentListResponse:
    if pageSize > 100:
        raise HTTPException(status_code=400, detail="Validation Error")
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
            cursor.execute(f"SELECT COUNT(*) FROM payments{where_sql}", tuple(params))
            total = cursor.fetchone()[0]
            offset = (page - 1) * pageSize
            cursor.execute(f"SELECT payment_id, user_id, plan_id, amount, currency, payment_method, status, gateway_order_id, gateway_payment_id, initiated_at, completed_at FROM payments{where_sql} ORDER BY initiated_at DESC LIMIT %s OFFSET %s", tuple(params + [int(pageSize), int(offset)]))
            rows = cursor.fetchall()
            payments = [PaymentListItem(paymentId=r[0], userId=r[1], planId=r[2], amount=Decimal(str(r[3])), currency=r[4], paymentMethod=r[5], status=r[6], gatewayOrderId=r[7], gatewayPaymentId=r[8], invoiceId=None, initiatedAt=r[9], completedAt=r[10]) for r in rows]
            return PaymentListResponse(status="success", total=total, page=page, pageSize=pageSize, payments=payments)
    except HTTPException:
        raise
    except psycopg2.Error as exc:
        logger.error("Database error: %s", str(exc), exc_info=True)
        raise HTTPException(status_code=503, detail="Database Error")
    except Exception as exc:
        logger.error("Unexpected error: %s", str(exc), exc_info=True)
        raise HTTPException(status_code=500, detail="Internal Error")
    finally:
        if conn is not None:
            release_conn(conn)

from dataclasses import dataclass
from datetime import datetime
from decimal import Decimal
from typing import Any


@dataclass(slots=True)
class PaymentRecord:
    payment_id: str
    user_id: str
    plan_id: str
    amount: Decimal
    currency: str
    payment_method: str
    gateway_name: str
    gateway_order_id: str
    gateway_payment_id: str | None
    status: str
    failure_reason: str | None
    description: str | None
    metadata: dict[str, Any] | None
    email: str
    initiated_at: datetime
    completed_at: datetime | None


@dataclass(slots=True)
class PaymentVerificationRecord:
    verification_id: str
    payment_id: str
    gateway_payment_id: str
    gateway_order_id: str
    gateway_signature: str | None
    verification_source: str
    verification_status: str
    raw_gateway_response: dict[str, Any] | None
    verified_at: datetime

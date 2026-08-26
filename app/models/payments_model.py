from dataclasses import dataclass
from decimal import Decimal
from typing import Any

from pydantic.types import AwareDatetime


@dataclass
class PaymentRecord:
    paymentId: str
    userId: str
    planId: str
    amount: Decimal
    currency: str
    paymentMethod: str
    status: str
    gatewayOrderId: str
    gatewayPaymentId: str | None
    invoiceId: str | None
    initiatedAt: AwareDatetime | None
    completedAt: AwareDatetime | None


@dataclass
class PaymentVerificationRecord:
    verificationId: str
    paymentId: str
    gatewayPaymentId: str
    gatewayOrderId: str
    gatewaySignature: str
    verificationSource: str
    verificationStatus: str
    rawGatewayResponse: dict[str, Any] | None

from __future__ import annotations

from dataclasses import dataclass
from decimal import Decimal


@dataclass(slots=True)
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
    initiatedAt: str
    completedAt: str | None


@dataclass(slots=True)
class PaymentVerificationRecord:
    verificationId: str
    paymentId: str
    gatewayPaymentId: str
    gatewayOrderId: str
    gatewaySignature: str
    verificationSource: str
    verificationStatus: str
    rawGatewayResponse: dict[str, object] | None
    verifiedAt: str

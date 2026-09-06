from decimal import Decimal
from typing import Any

from pydantic import BaseModel, ConfigDict, EmailStr, Field, field_validator, model_validator
from pydantic.types import AwareDatetime


class PaymentMetadata(BaseModel):
    model_config = ConfigDict(extra="forbid")

    planName: str | None = None
    billingCycle: str | None = None


class PaymentInitiateRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")

    userId: str
    planId: str
    amount: Decimal
    currency: str
    paymentMethod: str
    email: EmailStr
    description: str | None = Field(default=None, max_length=500)
    metadata: dict[str, Any] | None = None

    @field_validator("userId", "planId", "currency", "paymentMethod")
    @classmethod
    def non_empty(cls, value: str) -> str:
        if not value or not value.strip():
            raise ValueError("field must not be empty")
        return value


class PaymentInitiateResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    status: str
    paymentId: str
    gatewayOrderId: str
    amount: Decimal
    currency: str
    message: str
    initiatedAt: AwareDatetime


class PaymentVerifyRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")

    paymentId: str
    gatewayPaymentId: str
    gatewayOrderId: str
    gatewaySignature: str
    verificationSource: str
    status: str

    @field_validator("paymentId", "gatewayPaymentId", "gatewayOrderId", "gatewaySignature", "verificationSource", "status")
    @classmethod
    def non_empty(cls, value: str) -> str:
        if not value or not value.strip():
            raise ValueError("field must not be empty")
        return value


class PaymentVerifyResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    status: str
    verificationId: str
    paymentId: str
    invoiceId: str
    paymentStatus: str
    subscriptionActivated: bool
    message: str
    verifiedAt: AwareDatetime


class PaymentSummary(BaseModel):
    model_config = ConfigDict(extra="forbid")

    paymentId: str
    userId: str
    planId: str
    amount: Decimal
    currency: str
    paymentMethod: str
    status: str
    gatewayOrderId: str
    gatewayPaymentId: str | None = None
    invoiceId: str | None = None
    initiatedAt: AwareDatetime
    completedAt: AwareDatetime | None = None


class PaymentsListResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    status: str
    total: int
    page: int
    pageSize: int
    payments: list[PaymentSummary]

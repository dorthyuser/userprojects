from decimal import Decimal
from typing import Any

from pydantic import BaseModel, ConfigDict, EmailStr, Field, field_validator
from pydantic.types import AwareDatetime


class PaymentInitiateRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")

    userId: str = Field(min_length=1, max_length=50)
    planId: str = Field(min_length=1, max_length=30)
    amount: Decimal
    currency: str = Field(min_length=3, max_length=3)
    paymentMethod: str = Field(min_length=1, max_length=30)
    email: EmailStr
    description: str | None = Field(default=None, max_length=500)
    metadata: dict[str, Any] | None = None


class PaymentInitiateResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    status: str
    paymentId: str
    gatewayOrderId: str
    amount: Decimal
    currency: str
    message: str
    initiatedAt: str


class PaymentVerifyRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")

    paymentId: str = Field(min_length=1, max_length=25)
    gatewayPaymentId: str = Field(min_length=1, max_length=100)
    gatewayOrderId: str = Field(min_length=1, max_length=100)
    gatewaySignature: str = Field(min_length=1, max_length=500)
    verificationSource: str = Field(min_length=1, max_length=20)
    status: str = Field(min_length=1, max_length=20)


class PaymentVerifyResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    status: str
    verificationId: str
    paymentId: str
    invoiceId: str
    paymentStatus: str
    subscriptionActivated: bool
    message: str
    verifiedAt: str


class PaymentListItem(BaseModel):
    model_config = ConfigDict(extra="forbid")

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


class PaymentListResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    status: str
    total: int
    page: int
    pageSize: int
    payments: list[PaymentListItem]

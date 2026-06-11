from __future__ import annotations

from typing import Dict, Optional

from pydantic import BaseModel, Field


class CurrencyTransferRequest(BaseModel):
    fromCurrency: str = Field(..., min_length=3, max_length=3)
    toCurrency: str = Field(..., min_length=3, max_length=3)
    amount: float = Field(..., gt=0)


class CurrencyAmount(BaseModel):
    currency: str
    symbol: str
    amount: Optional[float] = None


class FeeBreakdown(BaseModel):
    transferFee: float
    platformFee: float
    gst: float
    totalFees: float


class Summary(BaseModel):
    senderPays: str
    receiverGets: str


class CurrencyTransferResponse(BaseModel):
    source: Dict[str, object]
    destination: Dict[str, object]
    marketRate: float
    bankRate: float
    grossAmountInINR: float
    fees: Dict[str, float]
    exchangeLoss: float
    netAmountReceived: float
    summary: Dict[str, str]


class ErrorBody(BaseModel):
    code: str
    message: str


class ErrorResponse(BaseModel):
    error: Dict[str, str]
    status_code: Optional[int] = 400

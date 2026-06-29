from __future__ import annotations

from pydantic import BaseModel


class CurrencyTransferRequest(BaseModel):
    fromCurrency: str
    toCurrency: str
    amount: float


class CurrencyTransferSource(BaseModel):
    currency: str
    symbol: str
    amount: float


class CurrencyTransferDestination(BaseModel):
    currency: str
    symbol: str


class CurrencyTransferFees(BaseModel):
    transferFee: float
    platformFee: float
    gst: float
    totalFees: float


class CurrencyTransferSummary(BaseModel):
    senderPays: str
    receiverGets: str


class CurrencyTransferResponse(BaseModel):
    source: CurrencyTransferSource
    destination: CurrencyTransferDestination
    marketRate: float
    bankRate: float
    grossAmountInINR: float
    fees: CurrencyTransferFees
    exchangeLoss: float
    netAmountReceived: float
    summary: CurrencyTransferSummary

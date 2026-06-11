from __future__ import annotations

from decimal import Decimal, ROUND_HALF_UP
from typing import Dict

from models.api_models import (
    CurrencyTransferRequest,
    CurrencyTransferResponse,
    CurrencyTransferFees,
    CurrencyTransferSource,
    CurrencyTransferDestination,
    CurrencyTransferSummary,
)
from models.enums import CURRENCY_SYMBOLS, SUPPORTED_CURRENCIES


_RATE_CACHE: Dict[str, Decimal] = {
    "USD_INR": Decimal("85.00"),
    "USD_EUR": Decimal("0.92"),
    "EUR_INR": Decimal("92.50"),
    "GBP_INR": Decimal("108.25"),
    "AED_INR": Decimal("23.15"),
    "CAD_INR": Decimal("62.10"),
    "AUD_INR": Decimal("56.40"),
    "SGD_INR": Decimal("62.80"),
    "JPY_INR": Decimal("0.57"),
}


def _round_money(value: Decimal) -> Decimal:
    return value.quantize(Decimal("0.01"), rounding=ROUND_HALF_UP)


def _get_market_rate(from_currency: str, to_currency: str) -> Decimal:
    cache_key = f"{from_currency}_{to_currency}"
    if cache_key in _RATE_CACHE:
        return _RATE_CACHE[cache_key]
    fallback = Decimal("1.00")
    _RATE_CACHE[cache_key] = fallback
    return fallback


def calculate_currency_transfer(request: CurrencyTransferRequest) -> CurrencyTransferResponse:
    if request.fromCurrency not in SUPPORTED_CURRENCIES or request.toCurrency not in SUPPORTED_CURRENCIES:
        raise ValueError("Unsupported currency code provided.")

    market_rate = _get_market_rate(request.fromCurrency, request.toCurrency)
    bank_rate = _round_money(market_rate * Decimal("0.9906"))
    gross_amount = _round_money(Decimal(str(request.amount)) * market_rate)
    transfer_fee = Decimal("0.50")
    platform_fee = Decimal("0.20")
    gst = _round_money((transfer_fee + platform_fee) * Decimal("0.19"))
    total_fees = _round_money(transfer_fee + platform_fee + gst)
    exchange_loss = _round_money((market_rate - bank_rate) * Decimal(str(request.amount)))
    net_amount = _round_money(gross_amount - total_fees - exchange_loss)

    source_symbol = CURRENCY_SYMBOLS.get(request.fromCurrency, request.fromCurrency)
    destination_symbol = CURRENCY_SYMBOLS.get(request.toCurrency, request.toCurrency)

    return CurrencyTransferResponse(
        source=CurrencyTransferSource(
            currency=request.fromCurrency,
            symbol=source_symbol,
            amount=_round_money(Decimal(str(request.amount))),
        ),
        destination=CurrencyTransferDestination(
            currency=request.toCurrency,
            symbol=destination_symbol,
        ),
        marketRate=_round_money(market_rate),
        bankRate=bank_rate,
        grossAmountInINR=gross_amount,
        fees=CurrencyTransferFees(
            transferFee=_round_money(transfer_fee),
            platformFee=_round_money(platform_fee),
            gst=gst,
            totalFees=total_fees,
        ),
        exchangeLoss=exchange_loss,
        netAmountReceived=net_amount,
        summary=CurrencyTransferSummary(
            senderPays=f"{source_symbol}{_round_money(Decimal(str(request.amount))):.2f}",
            receiverGets=f"{destination_symbol}{net_amount:.2f}",
        ),
    )

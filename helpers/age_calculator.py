from __future__ import annotations

import os
from decimal import Decimal, ROUND_HALF_UP
from typing import Dict

import httpx
from cachetools import TTLCache

from models.api_models import CurrencyTransferRequest, CurrencyTransferResponse, ErrorResponse
from models.enums import CURRENCY_SYMBOLS

CACHE_TTL_SECONDS = int(os.getenv("CACHE_TTL_SECONDS", "300"))
RATE_CACHE: TTLCache[str, Decimal] = TTLCache(maxsize=256, ttl=CACHE_TTL_SECONDS)


def _round_money(value: Decimal) -> Decimal:
    return value.quantize(Decimal("0.01"), rounding=ROUND_HALF_UP)


async def _fetch_market_rate(from_currency: str, to_currency: str) -> Decimal:
    cache_key = f"{from_currency}:{to_currency}"
    if cache_key in RATE_CACHE:
        return RATE_CACHE[cache_key]

    base_url = os.getenv("EXCHANGE_RATE_API_BASE_URL", "https://api.exchangerate.host")
    url = f"{base_url.rstrip('/')}/convert?from={from_currency}&to={to_currency}"
    async with httpx.AsyncClient(timeout=10.0) as client:
        response = await client.get(url)
        response.raise_for_status()
        data = response.json()

    rate = Decimal(str(data.get("result")))
    RATE_CACHE[cache_key] = rate
    return rate


async def calculate_currency_transfer_quote(request: CurrencyTransferRequest) -> CurrencyTransferResponse:
    amount = Decimal(str(request.amount))
    market_rate = await _fetch_market_rate(request.fromCurrency, request.toCurrency)
    bank_rate = _round_money(market_rate * Decimal("0.992"))

    gross_amount = _round_money(amount * market_rate)
    transfer_fee = Decimal(os.getenv("DEFAULT_TRANSFER_FEE", "0.50"))
    platform_fee = Decimal(os.getenv("DEFAULT_PLATFORM_FEE", "0.20"))
    gst_rate = Decimal(os.getenv("DEFAULT_GST_RATE", "0.18"))
    gst = _round_money((transfer_fee + platform_fee) * gst_rate)
    total_fees = _round_money(transfer_fee + platform_fee + gst)
    exchange_loss = _round_money(amount * (market_rate - bank_rate))
    net_amount = _round_money(gross_amount - total_fees - exchange_loss)

    return CurrencyTransferResponse(
        source={"currency": request.fromCurrency, "symbol": CURRENCY_SYMBOLS[request.fromCurrency], "amount": float(_round_money(amount))},
        destination={"currency": request.toCurrency, "symbol": CURRENCY_SYMBOLS[request.toCurrency]},
        marketRate=float(_round_money(market_rate)),
        bankRate=float(bank_rate),
        grossAmountInINR=float(gross_amount),
        fees={
            "transferFee": float(_round_money(transfer_fee)),
            "platformFee": float(_round_money(platform_fee)),
            "gst": float(gst),
            "totalFees": float(total_fees),
        },
        exchangeLoss=float(exchange_loss),
        netAmountReceived=float(net_amount),
        summary={"senderPays": f"{CURRENCY_SYMBOLS[request.fromCurrency]}{_round_money(amount):.2f}", "receiverGets": f"{CURRENCY_SYMBOLS[request.toCurrency]}{net_amount:.2f}"},
    )

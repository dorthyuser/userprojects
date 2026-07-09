from __future__ import annotations

from typing import Any, Dict, Optional

from models.enums import SUPPORTED_CURRENCIES


def validate_currency_transfer_request(payload: Dict[str, Any]) -> Optional[str]:
    required_fields = ["fromCurrency", "toCurrency", "amount"]
    for field in required_fields:
        if field not in payload:
            return f"{field} is required."

    from_currency = payload.get("fromCurrency")
    to_currency = payload.get("toCurrency")
    amount = payload.get("amount")

    if not isinstance(from_currency, str) or from_currency not in SUPPORTED_CURRENCIES:
        return "fromCurrency must be a supported 3-letter currency code."
    if not isinstance(to_currency, str) or to_currency not in SUPPORTED_CURRENCIES:
        return "toCurrency must be a supported 3-letter currency code."
    if not isinstance(amount, (int, float)) or amount <= 0:
        return "amount must be a positive number."

    return None

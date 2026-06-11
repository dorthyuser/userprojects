from __future__ import annotations

from typing import Any, Optional

from models.api_models import ErrorResponse
from models.enums import SUPPORTED_CURRENCIES


def validate_currency_transfer_request(payload: Any) -> Optional[ErrorResponse]:
    if not isinstance(payload, dict):
        return ErrorResponse(error={"code": "VALIDATION_ERROR", "message": "Request body must be a JSON object."}, status_code=400)

    from_currency = payload.get("fromCurrency")
    to_currency = payload.get("toCurrency")
    amount = payload.get("amount")

    if not isinstance(from_currency, str) or from_currency not in SUPPORTED_CURRENCIES:
        return ErrorResponse(error={"code": "VALIDATION_ERROR", "message": "fromCurrency must be a valid ISO 4217 currency code."}, status_code=400)
    if not isinstance(to_currency, str) or to_currency not in SUPPORTED_CURRENCIES:
        return ErrorResponse(error={"code": "VALIDATION_ERROR", "message": "toCurrency must be a valid ISO 4217 currency code."}, status_code=400)
    if from_currency == to_currency:
        return ErrorResponse(error={"code": "VALIDATION_ERROR", "message": "fromCurrency and toCurrency must be different."}, status_code=400)
    if not isinstance(amount, (int, float)) or amount <= 0:
        return ErrorResponse(error={"code": "VALIDATION_ERROR", "message": "amount must be a positive number."}, status_code=400)
    return None

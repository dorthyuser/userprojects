import asyncio
import json
from unittest.mock import patch

import azure.functions as func

from currency_calculator.function_app import currency_transfer


class DummyRequest:
    def __init__(self, payload):
        self._payload = payload

    def get_json(self):
        if isinstance(self._payload, Exception):
            raise self._payload
        return self._payload


def _response_json(response: func.HttpResponse):
    return json.loads(response.get_body().decode("utf-8"))


def test_currency_transfer_invalid_json_returns_safe_error():
    response = asyncio.run(currency_transfer(DummyRequest(ValueError("bad json"))))
    assert response.status_code == 400
    body = _response_json(response)
    assert body["error"]["code"] == "InvalidJson"
    assert "bad json" not in response.get_body().decode("utf-8")


def test_currency_transfer_validation_error_does_not_leak_sensitive_data():
    with patch("currency_calculator.function_app.validate_currency_transfer_request", return_value="amount must be a positive number."), patch(
        "currency_calculator.function_app.logger"
    ) as mock_logger:
        response = asyncio.run(currency_transfer(DummyRequest({"fromCurrency": "USD", "toCurrency": "INR", "amount": -1})))

    assert response.status_code == 400
    body = _response_json(response)
    assert body["error"]["code"] == "ValidationError"
    assert body["error"]["message"] == "amount must be a positive number."
    mock_logger.error.assert_called()


def test_currency_transfer_success_response_shape_and_no_pii():
    response = asyncio.run(currency_transfer(DummyRequest({"fromCurrency": "USD", "toCurrency": "INR", "amount": 1})))
    assert response.status_code == 200
    body = _response_json(response)
    assert set(body.keys()) == {"source", "destination", "marketRate", "bankRate", "grossAmountInINR", "fees", "exchangeLoss", "netAmountReceived", "summary"}
    assert body["source"]["currency"] == "USD"
    assert body["destination"]["currency"] == "INR"
    assert "account" not in response.get_body().decode("utf-8").lower()
    assert "card" not in response.get_body().decode("utf-8").lower()

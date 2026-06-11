import pytest

from helpers.validation import validate_currency_transfer_request
from models.api_models import ErrorResponse


def test_placeholder_success():
    assert True


def test_placeholder_failure():
    assert 1 != 0


def test_validate_currency_transfer_request_rejects_non_object_payload():
    error = validate_currency_transfer_request([1, 2, 3])
    assert isinstance(error, ErrorResponse)
    assert error.error["code"] == "VALIDATION_ERROR"
    assert error.status_code == 400


def test_validate_currency_transfer_request_rejects_same_currency():
    error = validate_currency_transfer_request({"fromCurrency": "USD", "toCurrency": "USD", "amount": 10})
    assert isinstance(error, ErrorResponse)
    assert error.error["code"] == "VALIDATION_ERROR"
    assert "different" in error.error["message"]


def test_validate_currency_transfer_request_rejects_invalid_amount():
    error = validate_currency_transfer_request({"fromCurrency": "USD", "toCurrency": "INR", "amount": 0})
    assert isinstance(error, ErrorResponse)
    assert error.error["code"] == "VALIDATION_ERROR"
    assert "positive number" in error.error["message"]

from __future__ import annotations

from enum import Enum


class CurrencyCode(str, Enum):
    USD = "USD"
    INR = "INR"
    EUR = "EUR"
    GBP = "GBP"
    AED = "AED"
    CAD = "CAD"
    AUD = "AUD"
    SGD = "SGD"
    JPY = "JPY"


SUPPORTED_CURRENCIES = {item.value for item in CurrencyCode}
CURRENCY_SYMBOLS = {
    "USD": "$",
    "INR": "₹",
    "EUR": "€",
    "GBP": "£",
    "AED": "د.إ",
    "CAD": "$",
    "AUD": "$",
    "SGD": "$",
    "JPY": "¥",
}

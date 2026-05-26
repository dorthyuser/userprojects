from dataclasses import dataclass
from datetime import datetime
from typing import Optional


@dataclass
class CardholderRecord:
    id: int
    travelcard_id: int
    cardholder_title: str
    cardholder_forename: str
    cardholder_surname: str
    cardholder_type: str
    cardholder_photo_name: str | None = None
    cardholder_photo_rrs_key: str | None = None
    cardholder_photo_url: str | None = None
    cardholder_photo_key: str | None = None


@dataclass
class TravelcardRecord:
    id: int
    travelcard_type: str
    travelcard_valid_from: datetime
    travelcard_valid_to: datetime
    travelcard_name: str
    travelcard_number: str
    travelcard_requested_date: datetime
    travelcard_transaction_reference: str
    travelcard_usable_to: datetime | None = None

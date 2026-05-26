from dataclasses import dataclass
from datetime import datetime
from typing import Optional


@dataclass(slots=True)
class CardholderData:
    id: int | None
    travelcard_id: int
    cardholder_title: str
    cardholder_forename: str
    cardholder_surname: str
    cardholder_type: str
    cardholder_photo_name: str
    cardholder_photo_rrs_key: Optional[str]
    cardholder_photo_url: Optional[str]
    cardholder_photo_key: Optional[str]


@dataclass(slots=True)
class TravelcardData:
    id: int | None
    travelcard_type: str
    travelcard_valid_from: datetime
    travelcard_valid_to: datetime
    travelcard_name: Optional[str]
    travelcard_number: Optional[str]
    travelcard_requested_date: datetime
    travelcard_transaction_reference: str
    travelcard_usable_to: Optional[datetime]

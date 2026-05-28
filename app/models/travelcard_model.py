from dataclasses import dataclass
from datetime import datetime


@dataclass(slots=True)
class TravelCardData:
    travelcard_type: str
    travelcard_valid_from: datetime
    travelcard_valid_to: datetime
    travelcard_name: str | None
    travelcard_number: str
    travelcard_requested_date: datetime
    travelcard_transaction_reference: str
    travelcard_usable_to: datetime | None


@dataclass(slots=True)
class CardholderData:
    travelcard_id: int
    cardholder_title: str
    cardholder_forename: str
    cardholder_surname: str
    cardholder_type: str
    cardholder_photo_name: str
    cardholder_photo_rrs_key: str | None
    cardholder_photo_url: str | None
    cardholder_photo_key: str | None
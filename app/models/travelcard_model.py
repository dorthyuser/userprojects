from dataclasses import dataclass
from datetime import datetime


@dataclass(slots=True)
class CardholderData:
    cardholderTitle: str
    cardholderForename: str
    cardholderSurname: str
    cardholderType: str
    cardholderPhotoName: str
    cardholderPhotoRRSKey: str | None
    cardholderPhotoURL: str | None
    cardholderPhotoKey: str | None


@dataclass(slots=True)
class TravelcardData:
    travelcardType: str
    travelcardValidFrom: datetime
    travelcardValidTo: datetime
    travelcardName: str | None
    travelcardNumber: str
    travelcardRequestedDate: datetime
    travelcardTransactionReference: str
    travelcardUsableTo: datetime | None
    cardholders: list[CardholderData]

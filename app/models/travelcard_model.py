from dataclasses import dataclass


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
    travelcardValidFrom: object
    travelcardValidTo: object
    travelcardName: str | None
    travelcardNumber: str
    travelcardRequestedDate: object
    travelcardTransactionReference: str
    travelcardUsableTo: object | None
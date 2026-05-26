from datetime import datetime
from pydantic import BaseModel
from typing import Any, Optional


class CreateCardholderRequest(BaseModel):
    cardholder_title: str
    cardholder_forename: str
    cardholder_surname: str
    cardholder_type: str
    cardholder_photo_name: str | None = None
    cardholder_photo_rrs_key: str | None = None
    cardholder_photo_url: str | None = None
    cardholder_photo_key: str | None = None


class CreateTravelcardRequest(BaseModel):
    travelcard_type: str
    travelcard_valid_from: datetime
    travelcard_valid_to: datetime
    travelcard_name: str
    travelcard_number: str
    travelcard_requested_date: datetime
    travelcard_transaction_reference: str
    travelcard_usable_to: datetime | None = None
    cardholders: list[CreateCardholderRequest] = []


class CardholderResponseItem(BaseModel):
    id: int
    cardholderTitle: str
    cardholderForename: str
    cardholderSurname: str
    cardholderType: str
    cardholderPhotoName: str | None = None
    cardholderPhotoRRSKey: str | None = None
    cardholderPhotoURL: str | None = None
    cardholderPhotoKey: str | None = None


class TravelcardResponseItem(BaseModel):
    id: int
    travelcardId: str | None = None
    travelcardType: str
    travelcardValidFrom: str
    travelcardValidTo: str
    travelcardName: str
    travelcardNumber: str
    travelcardRequestedDate: str
    travelcardTransactionReference: str
    travelcardUsableTo: str | None = None
    cardholders: list[CardholderResponseItem] = []


class TravelcardsResponse(BaseModel):
    travelcards: list[TravelcardResponseItem]

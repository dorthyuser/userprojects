from datetime import datetime
from typing import Annotated, Literal

from pydantic import BaseModel, Field, model_validator, field_validator

TravelcardType = Literal[
    "Young",
    "Barcklays",
    "DevonandCornwall",
    "TwoTogether",
    "Family",
    "Senior",
    "DisabledPersons",
    "Network",
    "TwentySixToThirty",
    "SixteenToSeventeen",
    "Veterans"
]
CardholderType = Literal["Primary", "Secondary"]


class CardholderBase(BaseModel):
    cardholder_title: Annotated[str, Field(min_length=1, max_length=15)]
    cardholder_forename: Annotated[str, Field(min_length=1, max_length=100)]
    cardholder_surname: Annotated[str, Field(min_length=1, max_length=100)]
    cardholder_type: CardholderType
    cardholder_photo_name: Annotated[str, Field(min_length=1, max_length=100)]
    cardholder_photo_rrs_key: Annotated[str | None, Field(default=None, min_length=39, max_length=42)] = None
    cardholder_photo_url: Annotated[str | None, Field(default=None, min_length=20, max_length=2048)] = None
    cardholder_photo_key: Annotated[str | None, Field(default=None, min_length=39, max_length=42)] = None

    @model_validator(mode="after")
    def validate_one_of(self) -> "CardholderBase":
        provided = [self.cardholder_photo_rrs_key, self.cardholder_photo_url, self.cardholder_photo_key]
        if sum(value is not None for value in provided) != 1:
            raise ValueError("Exactly one cardholder photo identifier must be provided")
        return self


class TravelcardCreateRequest(BaseModel):
    travelcard_type: TravelcardType
    travelcard_valid_from: datetime
    travelcard_valid_to: datetime
    travelcard_name: Annotated[str | None, Field(default=None, max_length=255)] = None
    travelcard_number: Annotated[str, Field(min_length=11, max_length=22)]
    travelcard_requested_date: datetime
    travelcard_transaction_reference: Annotated[str, Field(min_length=15, max_length=15)]
    travelcard_usable_to: datetime | None = None
    cardholders: list[CardholderBase]

    @field_validator("travelcard_transaction_reference")
    @classmethod
    def validate_reference(cls, value: str) -> str:
        if len(value) != 15:
            raise ValueError("travelcardTransactionReference must be 15 characters")
        return value

    @model_validator(mode="after")
    def validate_cardholders(self) -> "TravelcardCreateRequest":
        if len(self.cardholders) not in {1, 2}:
            raise ValueError("cardholders must contain one or two items")
        primary_count = sum(1 for item in self.cardholders if item.cardholder_type == "Primary")
        if primary_count != 1:
            raise ValueError("Exactly one Primary cardholder is required")
        return self


class CardholderItem(BaseModel):
    id: int
    cardholderTitle: str
    cardholderForename: str
    cardholderSurname: str
    cardholderType: CardholderType
    cardholderPhotoName: str
    cardholderPhotoRRSKey: str | None = None
    cardholderPhotoURL: str | None = None
    cardholderPhotoKey: str | None = None


class TravelcardItem(BaseModel):
    id: int
    travelcardId: str
    travelcardType: TravelcardType
    travelcardValidFrom: datetime
    travelcardValidTo: datetime
    travelcardName: str | None = None
    travelcardNumber: str | None = None
    travelcardRequestedDate: datetime
    travelcardTransactionReference: str
    travelcardUsableTo: datetime | None = None
    cardholders: list[CardholderItem]


class TravelcardsListResponse(BaseModel):
    travelcards: list[TravelcardItem]


class TravelcardCreateResponse(BaseModel):
    travelcardId: str
    token: str

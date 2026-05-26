from __future__ import annotations

from datetime import datetime, timezone
from typing import Literal

from pydantic import BaseModel, Field, field_validator, model_validator

TravelcardType = Literal["Young", "Barcklays", "DevonandCornwall", "TwoTogether", "Family", "Senior", "DisabledPersons", "Network", "TwentySixToThirty", "SixteenToSeventeen", "Veterans"]
CardholderType = Literal["Primary", "Secondary"]


class CardholderSchema(BaseModel):
    cardholderTitle: str = Field(min_length=1, max_length=15)
    cardholderForename: str = Field(min_length=1, max_length=100)
    cardholderSurname: str = Field(min_length=1, max_length=100)
    cardholderType: CardholderType
    cardholderPhotoName: str = Field(min_length=1, max_length=100)
    cardholderPhotoRRSKey: str | None = Field(default=None, min_length=39, max_length=42)
    cardholderPhotoURL: str | None = Field(default=None, min_length=20, max_length=2048)
    cardholderPhotoKey: str | None = Field(default=None, min_length=39, max_length=42)

    @model_validator(mode="after")
    def validate_one_of_images(self) -> "CardholderSchema":
        provided = [self.cardholderPhotoRRSKey, self.cardholderPhotoURL, self.cardholderPhotoKey]
        if sum(value is not None for value in provided) != 1:
            raise ValueError("Exactly one cardholder photo field must be provided")
        return self


class CreateTravelcardRequest(BaseModel):
    travelcardType: TravelcardType
    travelcardValidFrom: datetime
    travelcardValidTo: datetime
    travelcardName: str | None = Field(default=None, max_length=255)
    travelcardNumber: str = Field(min_length=11, max_length=22)
    travelcardRequestedDate: datetime
    travelcardTransactionReference: str = Field(min_length=15, max_length=15)
    travelcardUsableTo: datetime | None = None
    cardholders: list[CardholderSchema]

    @field_validator("travelcardTransactionReference")
    @classmethod
    def validate_transaction_reference(cls, value: str) -> str:
        if not value.isdigit() or len(value) != 15:
            raise ValueError("Invalid transaction reference")
        return value

    @model_validator(mode="after")
    def validate_business_rules(self) -> "CreateTravelcardRequest":
        now = datetime.now(timezone.utc)
        one_month_from_now = now + timedelta(days=31)
        if self.travelcardRequestedDate >= now:
            raise ValueError("travelcardRequestedDate must be in the past")
        if self.travelcardValidFrom > one_month_from_now:
            raise ValueError("travelcardValidFrom must be within one calendar month from today")
        if self.travelcardValidFrom > self.travelcardValidTo:
            raise ValueError("travelcardValidFrom must be later than travelcardValidTo")
        if self.travelcardValidTo <= now:
            raise ValueError("travelcardValidTo must be in the future")
        if self.travelcardType == "SixteenToSeventeen" and self.travelcardUsableTo is None:
            raise ValueError("travelcardUsableTo is required for SixteenToSeventeen")
        if self.travelcardUsableTo is not None and self.travelcardUsableTo <= now:
            raise ValueError("travelcardUsableTo must be in the future")
        if len(self.cardholders) not in (1, 2):
            raise ValueError("cardholders must contain exactly one or two items")
        primary_count = sum(1 for cardholder in self.cardholders if cardholder.cardholderType == "Primary")
        if primary_count != 1:
            raise ValueError("Exactly one Primary cardholder is required")
        secondary_count = sum(1 for cardholder in self.cardholders if cardholder.cardholderType == "Secondary")
        if secondary_count > 1:
            raise ValueError("Only one Secondary cardholder is allowed")
        if secondary_count == 1 and self.travelcardType in {"SixteenToSeventeen", "Veterans"}:
            raise ValueError("Secondary cardholder is not allowed for this travelcard type")
        return self


class CreateTravelcardResponse(BaseModel):
    travelcardId: str
    token: str
from datetime import datetime
from typing import Any
from pydantic import BaseModel, ConfigDict, Field, HttpUrl, AwareDatetime, field_validator, model_validator


class CardholderSchema(BaseModel):
    model_config = ConfigDict(extra="forbid")

    cardholderTitle: str = Field(min_length=1, max_length=15)
    cardholderForename: str = Field(min_length=1, max_length=100)
    cardholderSurname: str = Field(min_length=1, max_length=100)
    cardholderType: str
    cardholderPhotoName: str = Field(min_length=1, max_length=100)
    cardholderPhotoRRSKey: str | None = Field(default=None)
    cardholderPhotoURL: HttpUrl | None = None
    cardholderPhotoKey: str | None = Field(default=None)

    @field_validator("cardholderPhotoRRSKey", "cardholderPhotoKey")
    @classmethod
    def validate_photo_key_lengths(cls, value: str | None) -> str | None:
        if value is not None and not (39 <= len(value) <= 42):
            raise ValueError("must be between 39 and 42 characters")
        return value

    @field_validator("cardholderPhotoURL")
    @classmethod
    def validate_photo_url_length(cls, value: HttpUrl | None) -> HttpUrl | None:
        if value is not None and not (20 <= len(str(value)) <= 2048):
            raise ValueError("must be between 20 and 2048 characters")
        return value


class TravelCardCreateRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")

    travelcardType: str
    travelcardValidFrom: AwareDatetime
    travelcardValidTo: AwareDatetime
    travelcardName: str | None = Field(default=None, max_length=255)
    travelcardNumber: str = Field(min_length=11, max_length=22)
    travelcardRequestedDate: AwareDatetime
    travelcardTransactionReference: str = Field(min_length=15, max_length=15)
    travelcardUsableTo: AwareDatetime | None = None
    cardholders: list[CardholderSchema]

    @field_validator("travelcardType")
    @classmethod
    def validate_travelcard_type(cls, value: str) -> str:
        allowed = {
            "Young",
            "TwoTogether",
            "Family",
            "Senior",
            "Network",
            "TwentySixToThirty",
            "SixteenToSeventeen",
            "Veterans"
        }
        if value not in allowed:
            raise ValueError("invalid travelcardType")
        return value

    @field_validator("travelcardName")
    @classmethod
    def validate_name(cls, value: str | None) -> str | None:
        if value is not None and not __import__("re").fullmatch(r"^[A-Za-z0-9 ]*$", value):
            raise ValueError("invalid travelcardName")
        return value

    @field_validator("travelcardNumber")
    @classmethod
    def validate_number(cls, value: str) -> str:
        if not __import__("re").fullmatch(r"^[A-Za-z0-9]+$", value):
            raise ValueError("invalid travelcardNumber")
        return value

    @field_validator("travelcardTransactionReference")
    @classmethod
    def validate_reference(cls, value: str) -> str:
        if not __import__("re").fullmatch(r"^[0-9]{15}$", value):
            raise ValueError("invalid travelcardTransactionReference")
        return value

    @model_validator(mode="after")
    def validate_cardholders(self) -> "TravelCardCreateRequest":
        if len(self.cardholders) not in {1, 2}:
            raise ValueError("cardholders must contain exactly one or two items")
        primary_count = sum(1 for c in self.cardholders if c.cardholderType == "Primary")
        secondary_count = sum(1 for c in self.cardholders if c.cardholderType == "Secondary")
        if primary_count != 1:
            raise ValueError("Exactly one Primary cardholder is required")
        if secondary_count > 1:
            raise ValueError("Only one Secondary cardholder is allowed")
        return self


class TravelCardCreateResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    travelcardId: str
    token: str
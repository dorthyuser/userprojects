from datetime import datetime
from typing import Annotated, Literal

from pydantic import AwareDatetime, BaseModel, ConfigDict, Field, HttpUrl, field_validator, model_validator

TravelcardType = Literal["Young", "TwoTogether", "Family", "Senior", "Network", "TwentySixToThirty", "SixteenToSeventeen", "Veterans"]
CardholderType = Literal["Primary", "Secondary"]


class CardholderSchema(BaseModel):
    model_config = ConfigDict(extra="forbid")

    cardholderTitle: Annotated[str, Field(min_length=1, max_length=15)]
    cardholderForename: Annotated[str, Field(min_length=1, max_length=100)]
    cardholderSurname: Annotated[str, Field(min_length=1, max_length=100)]
    cardholderType: CardholderType
    cardholderPhotoName: Annotated[str, Field(min_length=1, max_length=100)]
    cardholderPhotoRRSKey: Annotated[str | None, Field(default=None, min_length=39, max_length=42)] = None
    cardholderPhotoURL: HttpUrl | None = None
    cardholderPhotoKey: Annotated[str | None, Field(default=None, min_length=39, max_length=42)] = None

    @field_validator('cardholderPhotoURL')
    @classmethod
    def validate_photo_url(cls, v: HttpUrl | None) -> HttpUrl | None:
        if v is not None:
            url_str = str(v)
            if len(url_str) < 20 or len(url_str) > 2048:
                raise ValueError('cardholderPhotoURL must be between 20 and 2048 characters')
        return v

    @model_validator(mode="after")
    def validate_one_of(self) -> "CardholderSchema":
        provided = [self.cardholderPhotoRRSKey is not None, self.cardholderPhotoURL is not None, self.cardholderPhotoKey is not None]
        if sum(provided) != 1:
            raise ValueError("Each cardholder must provide exactly one of cardholderPhotoRRSKey, cardholderPhotoURL, or cardholderPhotoKey")
        return self


class TravelcardCreateRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")

    travelcardType: TravelcardType
    travelcardValidFrom: AwareDatetime
    travelcardValidTo: AwareDatetime
    travelcardName: Annotated[str | None, Field(default=None, max_length=255)] = None
    travelcardNumber: Annotated[str, Field(min_length=11, max_length=22, pattern=r"^[A-Za-z0-9]+$")]
    travelcardRequestedDate: AwareDatetime
    travelcardTransactionReference: Annotated[str, Field(min_length=15, max_length=15)]
    travelcardUsableTo: AwareDatetime | None = None
    cardholders: list[CardholderSchema]

    @field_validator("travelcardName")
    @classmethod
    def validate_travelcard_name(cls, value: str | None) -> str | None:
        if value is None:
            return value
        if not all(ch.isalnum() or ch == " " for ch in value):
            raise ValueError("travelcardName must match the required pattern")
        return value

    @field_validator("travelcardTransactionReference")
    @classmethod
    def validate_travelcard_transaction_reference(cls, value: str) -> str:
        if len(value) != 15 or not value.isalnum():
            raise ValueError("travelcardTransactionReference must be exactly 15 alphanumeric characters")
        return value

    @model_validator(mode="after")
    def validate_usable_to(self) -> "TravelcardCreateRequest":
        if self.travelcardType == "SixteenToSeventeen" and self.travelcardUsableTo is None:
            raise ValueError("travelcardUsableTo is required for SixteenToSeventeen")
        if self.travelcardType != "SixteenToSeventeen" and self.travelcardUsableTo is not None:
            raise ValueError("travelcardUsableTo is only allowed for SixteenToSeventeen")
        return self


class TravelcardCreateResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    travelcardId: str
    token: str

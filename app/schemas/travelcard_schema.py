from datetime import datetime
from typing import Annotated, Literal

from pydantic import AwareDatetime, BaseModel, ConfigDict, Field, HttpUrl, field_validator, model_validator


class CardholderCreate(BaseModel):
    model_config = ConfigDict(extra="forbid")

    cardholderTitle: Annotated[str, Field(min_length=1, max_length=15)]
    cardholderForename: Annotated[str, Field(min_length=1, max_length=100)]
    cardholderSurname: Annotated[str, Field(min_length=1, max_length=100)]
    cardholderType: Literal["Primary", "Secondary"]
    cardholderPhotoName: Annotated[str, Field(min_length=1, max_length=100)]
    cardholderPhotoRRSKey: str | None = None
    cardholderPhotoURL: HttpUrl | None = None
    cardholderPhotoKey: str | None = None

    @field_validator("cardholderPhotoRRSKey", "cardholderPhotoKey")
    @classmethod
    def validate_photo_key(cls, value: str | None) -> str | None:
        if value is None:
            return value
        if not (39 <= len(value) <= 42):
            raise ValueError("invalid length")
        return value

    @field_validator("cardholderPhotoURL")
    @classmethod
    def validate_photo_url(cls, value: HttpUrl | None) -> HttpUrl | None:
        if value is None:
            return value
        if not (20 <= len(str(value)) <= 2048):
            raise ValueError("invalid length")
        return value


class TravelcardCreateRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")

    travelcardType: Literal[
        "Young",
        "TwoTogether",
        "Family",
        "Senior",
        "Network",
        "TwentySixToThirty",
        "SixteenToSeventeen",
        "Veterans",
    ]
    travelcardValidFrom: AwareDatetime
    travelcardValidTo: AwareDatetime
    travelcardName: Annotated[str | None, Field(default=None, max_length=255)] = None
    travelcardNumber: Annotated[str, Field(min_length=11, max_length=22, pattern=r"^[A-Za-z0-9]+$")]
    travelcardRequestedDate: AwareDatetime
    travelcardTransactionReference: Annotated[str, Field(min_length=15, max_length=15)]
    travelcardUsableTo: AwareDatetime | None = None
    cardholders: list[CardholderCreate]

    @field_validator("travelcardName")
    @classmethod
    def validate_travelcard_name(cls, value: str | None) -> str | None:
        if value is None:
            return value
        if not __import__("re").fullmatch(r"^[A-Za-z0-9 ]*$", value):
            raise ValueError("invalid pattern")
        return value

    @field_validator("travelcardTransactionReference")
    @classmethod
    def validate_transaction_reference(cls, value: str) -> str:
        if not __import__("re").fullmatch(r"^[0-9]{2}[A-Z0-9]{4}[0-9]{4}[0-9]{5}$", value):
            raise ValueError("invalid pattern")
        return value

    @model_validator(mode="after")
    def validate_conditional_fields(self) -> "TravelcardCreateRequest":
        if self.travelcardType == "SixteenToSeventeen" and self.travelcardUsableTo is None:
            raise ValueError("travelcardUsableTo required")
        if self.travelcardType != "SixteenToSeventeen" and self.travelcardUsableTo is not None:
            raise ValueError("travelcardUsableTo not allowed")
        return self


class TravelcardCreateResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    travelcardId: str
    token: str

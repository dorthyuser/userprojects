from datetime import datetime
from typing import Annotated, Literal
from pydantic import AwareDatetime, BaseModel, ConfigDict, Field, HttpUrl, field_validator, model_validator

TravelcardType = Literal["Young", "TwoTogether", "Family", "Senior", "Network", "TwentySixToThirty", "SixteenToSeventeen", "Veterans"]
CardholderType = Literal["Primary", "Secondary"]


class CardholderSchema(BaseModel):
    model_config = ConfigDict(extra="forbid")

    cardholderTitle: Annotated[str, Field(min_length=1, max_length=15, pattern=r"^(?!.*[×÷ˇ˘μ])[A-Za-zÀ-žºª .'’\-]+$")]
    cardholderForename: Annotated[str, Field(min_length=1, max_length=100, pattern=r"^(?!.*[×÷ˇ˘μ])[A-Za-zÀ-ž .'’\-]+$")]
    cardholderSurname: Annotated[str, Field(min_length=1, max_length=100, pattern=r"^(?!.*[×÷ˇ˘μ])[A-Za-zÀ-ž .'’\-]+$")]
    cardholderType: CardholderType
    cardholderPhotoName: Annotated[str, Field(min_length=1, max_length=100, pattern=r"^(?!.*[×÷ˇ˘μ])[A-Za-z0-9À-ž _.\-()\[\]',&+#]+$")]
    cardholderPhotoRRSKey: str | None = Field(default=None)
    cardholderPhotoURL: HttpUrl | None = Field(default=None)
    cardholderPhotoKey: str | None = Field(default=None)

    @field_validator("cardholderPhotoRRSKey", "cardholderPhotoKey")
    @classmethod
    def validate_keys(cls, value: str | None) -> str | None:
        if value is None:
            return None
        if not (39 <= len(value) <= 42):
            raise ValueError("must be between 39 and 42 characters")
        if not __import__("re").match(r"^[A-Za-z0-9-]{36}\.[A-Za-z0-9]{2,5}$", value):
            raise ValueError("invalid format")
        return value

    @field_validator("cardholderPhotoURL")
    @classmethod
    def validate_url_length(cls, value: HttpUrl | None) -> HttpUrl | None:
        if value is None:
            return None
        if not (20 <= len(str(value)) <= 2048):
            raise ValueError("invalid length")
        return value

    @model_validator(mode="after")
    def validate_one_of(self) -> "CardholderSchema":
        one_of_count = sum(1 for item in (self.cardholderPhotoRRSKey, self.cardholderPhotoURL, self.cardholderPhotoKey) if item is not None)
        if one_of_count != 1:
            raise ValueError("exactly one of cardholderPhotoRRSKey, cardholderPhotoURL, cardholderPhotoKey must be provided")
        return self


class TravelcardCreateRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")

    travelcardType: TravelcardType
    travelcardValidFrom: AwareDatetime
    travelcardValidTo: AwareDatetime
    travelcardName: Annotated[str | None, Field(default=None, max_length=255, pattern=r"^[A-Za-z0-9 ]*$")]
    travelcardNumber: Annotated[str, Field(min_length=11, max_length=22, pattern=r"^[A-Za-z0-9]+$")]
    travelcardRequestedDate: AwareDatetime
    travelcardTransactionReference: Annotated[str, Field(min_length=15, max_length=15, pattern=r"^[0-9]{2}[A-Z0-9]{4}[0-9]{4}[0-9]{5}$")]
    travelcardUsableTo: AwareDatetime | None = None
    cardholders: list[CardholderSchema]

    @model_validator(mode="after")
    def validate_cardholders(self) -> "TravelcardCreateRequest":
        if len(self.cardholders) not in (1, 2):
            raise ValueError("cardholders must contain one or two items")
        return self


class TravelcardCreateResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    travelcardId: str
    token: str

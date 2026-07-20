from datetime import datetime
from typing import Any

from pydantic import AwareDatetime, BaseModel, ConfigDict, EmailStr, Field, field_validator, model_validator


class UserCreateItem(BaseModel):
    model_config = ConfigDict(extra="forbid")

    first_name: str | None = Field(default=None, max_length=100)
    last_name: str = Field(max_length=100)
    email: EmailStr
    phone: str | None = Field(default=None, max_length=30)
    mobile: str | None = Field(default=None, max_length=30)
    role: str = Field(pattern=r"^[0-9]+$")
    profile: str = Field(pattern=r"^[0-9]+$")
    country_locale: str | None = Field(default=None, max_length=10)
    time_zone: str | None = Field(default=None, max_length=60)

    @field_validator("first_name", "last_name")
    @classmethod
    def validate_name(cls, value: str | None) -> str | None:
        if value is None:
            return value
        if len(value) > 100:
            raise ValueError("invalid length")
        return value


class CreateUserRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")

    users: list[UserCreateItem]

    @model_validator(mode="after")
    def validate_users(self) -> "CreateUserRequest":
        if len(self.users) != 1:
            raise ValueError("only one user is allowed")
        return self


class CreateUserResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    status: str
    zoho_id: str
    email: EmailStr
    created_at: str


class ZohoUserResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    status: str
    user: dict[str, Any]


class ZohoUserListResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    status: str
    info: dict[str, Any]
    users: list[dict[str, Any]]


class DeltaSyncRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")

    full_sync: bool | None = False
    type: str | None = Field(default="AllUsers")
    per_page: int | None = Field(default=200, ge=1, le=200)


class DeltaSyncResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    status: str
    watermark_used: str
    new_watermark: str
    pages_fetched: int | None = None
    zoho_records_read: int | None = None
    upserted: int | None = None
    unchanged: int | None = None
    errors: int | None = None
    sync_duration_ms: int | None = None
    error_detail: list[dict[str, Any]] | None = None


class LocalUserResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    status: str
    user: dict[str, Any]


class LocalUserListResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    status: str
    page: int
    page_size: int
    total_count: int
    users: list[dict[str, Any]]

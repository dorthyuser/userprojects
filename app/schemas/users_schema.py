from datetime import datetime
from typing import Any

from pydantic import AwareDatetime, BaseModel, ConfigDict, EmailStr, Field, field_validator, model_validator


class UserInputSchema(BaseModel):
    model_config = ConfigDict(extra="forbid")

    first_name: str | None = Field(default=None, max_length=100)
    last_name: str
    email: EmailStr
    phone: str | None = Field(default=None, max_length=30)
    mobile: str | None = Field(default=None, max_length=30)
    role: str
    profile: str
    country_locale: str | None = Field(default=None, max_length=10)
    time_zone: str | None = Field(default=None, max_length=60)

    @field_validator("first_name", "last_name")
    @classmethod
    def validate_name(cls, value: str | None) -> str | None:
        if value is None:
            return value
        if len(value) > 100:
            raise ValueError("Validation Error")
        return value

    @field_validator("role", "profile")
    @classmethod
    def validate_numeric_string(cls, value: str) -> str:
        if not value.isdigit():
            raise ValueError("Validation Error")
        return value


class CreateUserRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")

    users: list[UserInputSchema]


class SyncUsersRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")

    full_sync: bool | None = False
    type: str | None = "AllUsers"
    per_page: int | None = 200


class ZohoUserResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    status: str
    zoho_id: str
    email: str
    created_at: str


class ZohoUserListResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    status: str
    info: dict[str, Any] | None = None
    users: list[dict[str, Any]] | None = None


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


class SyncUsersResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    status: str
    watermark_used: str | None = None
    new_watermark: str | None = None
    pages_fetched: int | None = None
    zoho_records_read: int | None = None
    upserted: int | None = None
    unchanged: int | None = None
    errors: int | None = None
    sync_duration_ms: int | None = None
    error_detail: list[dict[str, str]] | None = None

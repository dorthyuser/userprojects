from datetime import datetime
from typing import Any

from pydantic import BaseModel, ConfigDict, EmailStr, Field, field_validator, model_validator


class ZohoUserCreateItem(BaseModel):
    model_config = ConfigDict(extra="forbid")

    first_name: str | None = Field(default=None, max_length=100)
    last_name: str = Field(min_length=1, max_length=100)
    email: EmailStr
    phone: str | None = Field(default=None, max_length=30)
    mobile: str | None = Field(default=None, max_length=30)
    role: str = Field(pattern=r"^[0-9]+$")
    profile: str = Field(pattern=r"^[0-9]+$")
    country_locale: str | None = Field(default=None, max_length=10)
    time_zone: str | None = Field(default=None, max_length=60)

    @field_validator("first_name", "last_name", "phone", "mobile", "country_locale", "time_zone")
    @classmethod
    def validate_strings(cls, value: str | None) -> str | None:
        return value


class CreateUserRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")

    users: list[ZohoUserCreateItem]

    @model_validator(mode="after")
    def validate_single_user(self) -> "CreateUserRequest":
        if len(self.users) != 1:
            raise ValueError("users must contain exactly one item")
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


class SyncUsersRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")

    full_sync: bool = False
    type: str = Field(default="AllUsers")
    per_page: int = Field(default=200, ge=1, le=200)

    @field_validator("type")
    @classmethod
    def validate_type(cls, value: str) -> str:
        allowed = {"AllUsers", "ActiveUsers", "DeactiveUsers"}
        if value not in allowed:
            raise ValueError("invalid type")
        return value


class SyncUsersResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    status: str
    watermark_used: str
    new_watermark: str
    pages_fetched: int | None = None
    zoho_records_read: int | None = None
    upserted: int
    unchanged: int | None = None
    errors: int | None = None
    sync_duration_ms: int | None = None
    error_detail: list[dict[str, str]] | None = None


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

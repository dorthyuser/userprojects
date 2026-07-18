from typing import Any

from pydantic import BaseModel, ConfigDict, EmailStr, Field, field_validator


class UserInputSchema(BaseModel):
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


class CreateUserRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")

    users: list[UserInputSchema]

    @field_validator("users")
    @classmethod
    def validate_users(cls, value: list[UserInputSchema]) -> list[UserInputSchema]:
        if len(value) != 1:
            raise ValueError("Only one user is allowed per request")
        return value


class CreateUserResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    status: str
    zoho_id: str
    email: str
    created_at: str


class GetZohoUserResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    status: str
    user: dict[str, Any]


class GetZohoUsersListResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    status: str
    info: dict[str, Any]
    users: list[dict[str, Any]]


class SyncUsersRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")

    full_sync: bool | None = None
    type: str | None = None
    per_page: int | None = None


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


class GetLocalUserResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    status: str
    user: dict[str, Any]


class GetLocalUsersListResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    status: str
    page: int
    page_size: int
    total_count: int
    users: list[dict[str, Any]]

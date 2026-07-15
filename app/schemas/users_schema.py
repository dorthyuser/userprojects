from datetime import datetime
from typing import Any

from pydantic import AwareDatetime, BaseModel, ConfigDict, EmailStr, Field, field_validator


class UserInputSchema(BaseModel):
    model_config = ConfigDict(extra="forbid")

    first_name: str | None = Field(default=None)
    last_name: str = Field(min_length=1, max_length=100)
    email: EmailStr = Field()
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
    users: list[UserInputSchema]


class CreateUserResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")
    status: str
    zoho_id: str
    email: EmailStr
    created_at: AwareDatetime


class DeltaSyncRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")
    full_sync: bool = False
    type: str = "AllUsers"
    per_page: int = 200


class DeltaSyncResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")
    status: str
    watermark_used: AwareDatetime
    new_watermark: AwareDatetime
    pages_fetched: int | None = None
    zoho_records_read: int | None = None
    upserted: int | None = None
    unchanged: int | None = None
    errors: int | None = None
    sync_duration_ms: int | None = None


class UserResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")
    status: str
    user: dict[str, Any]


class UserListResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")
    status: str
    info: dict[str, Any]
    users: list[dict[str, Any]]


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

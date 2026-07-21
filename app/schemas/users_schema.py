from datetime import datetime
from typing import Any

from pydantic import AwareDatetime, BaseModel, ConfigDict, EmailStr, Field, field_validator


class CreateUserItem(BaseModel):
    model_config = ConfigDict(extra="forbid")

    first_name: str | None = Field(default=None, max_length=100)
    last_name: str = Field(max_length=100)
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
            raise ValueError("invalid length")
        return value


class CreateUserRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")

    users: list[CreateUserItem]


class CreateUserResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    status: str
    zoho_id: str
    email: EmailStr
    created_at: AwareDatetime


class ErrorResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    status: str
    code: str
    message: str
    field: str | None = None


class ZohoUserResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    status: str
    user: dict[str, Any]


class ZohoUserItem(BaseModel):
    model_config = ConfigDict(extra="forbid")

    id: str | None = None
    first_name: str | None = None
    last_name: str | None = None
    email: str | None = None
    status: str | None = None
    role: dict[str, Any] | None = None
    profile: dict[str, Any] | None = None
    modified_time: str | None = None


class GetUserListResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    status: str
    info: dict[str, Any]
    users: list[dict[str, Any]]


class GetUserResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    status: str
    user: dict[str, Any]


class SyncUsersRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")

    full_sync: bool | None = False
    type: str | None = "AllUsers"
    per_page: int | None = 200


class SyncUsersResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    status: str
    watermark_used: datetime | None = None
    new_watermark: datetime | None = None
    pages_fetched: int | None = None
    zoho_records_read: int | None = None
    upserted: int | None = None
    unchanged: int | None = None
    errors: int | None = None
    sync_duration_ms: int | None = None


class LocalUserResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    user_pk: int
    zoho_uid: str
    given_name: str | None = None
    family_name: str | None = None
    display_name: str | None = None
    email_address: str | None = None
    phone_number: str | None = None
    mobile_number: str | None = None
    account_status: str | None = None
    is_confirmed: bool | None = None
    user_type: str | None = None
    zoho_role_id: str | None = None
    zoho_role_name: str | None = None
    zoho_profile_id: str | None = None
    zoho_profile_name: str | None = None
    reports_to_uid: str | None = None
    country_code: str | None = None
    locale_code: str | None = None
    iana_timezone: str | None = None
    zoho_created_at: AwareDatetime | None = None
    zoho_modified_at: AwareDatetime | None = None
    local_synced_at: AwareDatetime | None = None


class LocalUserListItem(BaseModel):
    model_config = ConfigDict(extra="forbid")

    user_pk: int
    zoho_uid: str
    given_name: str | None = None
    family_name: str | None = None
    email_address: str | None = None
    account_status: str | None = None
    zoho_role_name: str | None = None
    local_synced_at: AwareDatetime | None = None


class GetLocalUserResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    status: str
    user: dict[str, Any]


class GetLocalUserListResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    status: str
    page: int
    page_size: int
    total_count: int
    users: list[dict[str, Any]]

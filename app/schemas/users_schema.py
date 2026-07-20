from datetime import datetime
from typing import Any

from pydantic import BaseModel, ConfigDict, EmailStr, Field, field_validator


class UserInput(BaseModel):
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
            raise ValueError("Validation Error")
        return value


class CreateUserRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")

    users: list[UserInput]


class CreateUserResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    status: str
    zoho_id: str
    email: EmailStr
    created_at: datetime


class ZohoUserResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    status: str
    user: dict[str, Any]


class ZohoUserListResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    status: str
    info: dict[str, Any]
    users: list[dict[str, Any]]


class SyncErrorDetail(BaseModel):
    model_config = ConfigDict(extra="forbid")

    zoho_id: str
    reason: str


class DeltaSyncRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")

    full_sync: bool | None = None
    type: str | None = None
    per_page: int | None = None


class DeltaSyncResponse(BaseModel):
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
    error_detail: list[SyncErrorDetail] | None = None


class LocalUser(BaseModel):
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
    zoho_created_at: datetime | None = None
    zoho_modified_at: datetime | None = None
    local_synced_at: datetime | None = None


class LocalUserResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    status: str
    user: LocalUser


class LocalUserSummary(BaseModel):
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
    zoho_created_at: datetime | None = None
    zoho_modified_at: datetime | None = None
    local_synced_at: datetime | None = None


class LocalUserListResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    status: str
    page: int
    page_size: int
    total_count: int
    users: list[LocalUserSummary]

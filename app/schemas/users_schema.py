from datetime import datetime
from typing import Any

from pydantic import AwareDatetime, BaseModel, ConfigDict, EmailStr, Field, field_validator, model_validator


class UserInputModel(BaseModel):
    model_config = ConfigDict(extra="forbid")

    first_name: str | None = Field(default=None)
    last_name: str
    email: EmailStr
    phone: str | None = None
    mobile: str | None = None
    role: str
    profile: str
    country_locale: str | None = None
    time_zone: str | None = None

    @field_validator("first_name", "last_name")
    @classmethod
    def validate_name(cls, value: str | None) -> str | None:
        if value is None:
            return value
        if len(value) > 100:
            raise ValueError("name too long")
        return value

    @field_validator("phone", "mobile")
    @classmethod
    def validate_phone(cls, value: str | None) -> str | None:
        if value is None:
            return value
        if len(value) > 30:
            raise ValueError("phone too long")
        return value

    @field_validator("role", "profile")
    @classmethod
    def validate_numeric_id(cls, value: str) -> str:
        if not value.isdigit():
            raise ValueError("invalid numeric id")
        return value


class CreateUserRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")

    users: list[UserInputModel]

    @model_validator(mode="after")
    def validate_single_user(self) -> "CreateUserRequest":
        if len(self.users) != 1:
            raise ValueError("only one user allowed")
        return self


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


class ZohoRoleProfileModel(BaseModel):
    model_config = ConfigDict(extra="forbid")

    id: str | None = None
    name: str | None = None


class ZohoUserResponseModel(BaseModel):
    model_config = ConfigDict(extra="forbid")

    id: str
    first_name: str | None = None
    last_name: str | None = None
    full_name: str | None = None
    email: EmailStr | None = None
    phone: str | None = None
    mobile: str | None = None
    status: str | None = None
    confirm: bool | None = None
    type__s: str | None = None
    role: ZohoRoleProfileModel | None = None
    profile: ZohoRoleProfileModel | None = None
    reporting_to: ZohoRoleProfileModel | None = None
    country: str | None = None
    country_locale: str | None = None
    time_zone: str | None = None
    language: str | None = None
    created_time: str | None = None
    modified_time: str | None = None
    created_by: ZohoRoleProfileModel | None = None


class ZohoUserResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    status: str
    user: ZohoUserResponseModel


class ZohoUserListInfo(BaseModel):
    model_config = ConfigDict(extra="forbid")

    page: int
    per_page: int
    count: int
    more_records: bool


class ZohoUserListResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    status: str
    info: ZohoUserListInfo | None = None
    users: list[dict[str, Any]] | None = None


class DeltaSyncRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")

    full_sync: bool | None = False
    type: str | None = "AllUsers"
    per_page: int | None = 200


class DeltaSyncResponse(BaseModel):
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

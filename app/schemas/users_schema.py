from datetime import datetime

from pydantic import BaseModel, ConfigDict, EmailStr, Field


class ZohoUserCreateItem(BaseModel):
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


class ZohoUserCreateRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")

    users: list[ZohoUserCreateItem]


class ZohoUserCreateResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    status: str
    zoho_id: str
    email: EmailStr
    created_at: datetime


class ZohoUserResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    status: str
    user: dict


class ZohoUserListResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    status: str
    info: dict
    users: list[dict]


class ZohoUserSyncRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")

    full_sync: bool | None = False
    type: str | None = Field(default="AllUsers")
    per_page: int | None = Field(default=200, ge=1, le=200)


class ZohoUserSyncResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    status: str
    watermark_used: datetime
    new_watermark: datetime
    pages_fetched: int
    zoho_records_read: int
    upserted: int
    unchanged: int
    errors: int
    sync_duration_ms: int


class LocalUserResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    status: str
    user: dict


class LocalUserListResponse(BaseModel):
    model_config = ConfigDict(extra="forbid")

    status: str
    page: int
    page_size: int
    total_count: int
    users: list[dict]

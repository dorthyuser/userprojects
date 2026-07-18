from dataclasses import dataclass
from datetime import datetime, timezone
from typing import Any


@dataclass(slots=True)
class ZohoUserRecord:
    zoho_uid: str
    given_name: str | None
    family_name: str | None
    display_name: str | None
    email_address: str
    phone_number: str | None
    mobile_number: str | None
    account_status: str
    is_confirmed: bool | None
    user_type: str | None
    zoho_role_id: str | None
    zoho_role_name: str | None
    zoho_profile_id: str | None
    zoho_profile_name: str | None
    reports_to_uid: str | None
    country_code: str | None
    locale_code: str | None
    iana_timezone: str | None
    zoho_created_at: datetime | None
    zoho_modified_at: datetime

    @classmethod
    def from_zoho(cls, record: dict[str, Any]) -> "ZohoUserRecord":
        role = record.get("role") or {}
        profile = record.get("profile") or {}
        reporting_to = record.get("reporting_to") or {}
        created_time = record.get("Created_Time") or record.get("created_time")
        modified_time = record.get("Modified_Time") or record.get("modified_time")
        return cls(
            zoho_uid=str(record.get("id", "")),
            given_name=record.get("first_name"),
            family_name=record.get("last_name"),
            display_name=record.get("full_name"),
            email_address=str(record.get("email", "")),
            phone_number=record.get("phone"),
            mobile_number=record.get("mobile"),
            account_status=record.get("status", "active"),
            is_confirmed=record.get("confirm"),
            user_type=record.get("type__s"),
            zoho_role_id=role.get("id"),
            zoho_role_name=role.get("name"),
            zoho_profile_id=profile.get("id"),
            zoho_profile_name=profile.get("name"),
            reports_to_uid=reporting_to.get("id"),
            country_code=record.get("country"),
            locale_code=record.get("country_locale"),
            iana_timezone=record.get("time_zone"),
            zoho_created_at=datetime.fromisoformat(created_time) if created_time else None,
            zoho_modified_at=datetime.fromisoformat(modified_time) if modified_time else datetime.now(timezone.utc),
        )


@dataclass(slots=True)
class SyncSummary:
    status: str
    watermark_used: str | None
    new_watermark: str | None
    pages_fetched: int | None
    zoho_records_read: int | None
    upserted: int | None
    unchanged: int | None
    errors: int | None
    sync_duration_ms: int | None
    error_detail: list[dict[str, str]] | None = None

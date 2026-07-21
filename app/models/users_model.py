from dataclasses import dataclass
from datetime import datetime, timezone
from typing import Any


@dataclass
class ZohoUserRecord:
    zoho_uid: str
    given_name: str | None
    family_name: str | None
    display_name: str | None
    email_address: str | None
    phone_number: str | None
    mobile_number: str | None
    account_status: str | None
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
    def from_zoho(cls, item: dict[str, Any]) -> "ZohoUserRecord":
        role = item.get("role") or {}
        profile = item.get("profile") or {}
        reporting_to = item.get("reporting_to") or {}
        created_time = item.get("Created_Time") or item.get("created_time")
        modified_time = item.get("Modified_Time") or item.get("modified_time")
        return cls(
            zoho_uid=str(item.get("id") or ""),
            given_name=item.get("first_name"),
            family_name=item.get("last_name"),
            display_name=item.get("full_name"),
            email_address=item.get("email"),
            phone_number=item.get("phone"),
            mobile_number=item.get("mobile"),
            account_status=item.get("status"),
            is_confirmed=item.get("confirm"),
            user_type=item.get("type__s"),
            zoho_role_id=role.get("id"),
            zoho_role_name=role.get("name"),
            zoho_profile_id=profile.get("id"),
            zoho_profile_name=profile.get("name"),
            reports_to_uid=reporting_to.get("id"),
            country_code=item.get("country"),
            locale_code=item.get("country_locale"),
            iana_timezone=item.get("time_zone"),
            zoho_created_at=datetime.fromisoformat(created_time.replace("Z", "+00:00")) if created_time else None,
            zoho_modified_at=datetime.fromisoformat(modified_time.replace("Z", "+00:00")) if modified_time else datetime.now(timezone.utc),
        )


@dataclass
class LocalUserRecord:
    user_pk: int
    zoho_uid: str
    given_name: str | None
    family_name: str | None
    display_name: str | None
    email_address: str | None
    phone_number: str | None
    mobile_number: str | None
    account_status: str | None
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
    zoho_modified_at: datetime | None
    local_synced_at: datetime | None

from dataclasses import dataclass
from datetime import datetime
from typing import Any


@dataclass(slots=True)
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
            email_address=record.get("email"),
            phone_number=record.get("phone"),
            mobile_number=record.get("mobile"),
            account_status=record.get("status"),
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
            zoho_created_at=datetime.fromisoformat(created_time.replace("Z", "+00:00")) if created_time else None,
            zoho_modified_at=datetime.fromisoformat(modified_time.replace("Z", "+00:00")) if modified_time else datetime.now().astimezone(),
        )


@dataclass(slots=True)
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

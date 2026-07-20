from dataclasses import dataclass
from datetime import datetime


@dataclass(slots=True)
class ZohoUser:
    id: str
    first_name: str | None = None
    last_name: str | None = None
    full_name: str | None = None
    email: str | None = None
    phone: str | None = None
    mobile: str | None = None
    status: str | None = None
    confirm: bool | None = None
    type_s: str | None = None
    role: dict | None = None
    profile: dict | None = None
    reporting_to: dict | None = None
    country: str | None = None
    country_locale: str | None = None
    time_zone: str | None = None
    created_time: datetime | None = None
    modified_time: datetime | None = None


@dataclass(slots=True)
class LocalUser:
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

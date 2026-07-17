from dataclasses import dataclass
from datetime import datetime


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

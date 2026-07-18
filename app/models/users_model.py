from dataclasses import dataclass


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
    zoho_created_at: str | None
    zoho_modified_at: str


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
    zoho_created_at: str | None
    zoho_modified_at: str | None
    local_synced_at: str | None

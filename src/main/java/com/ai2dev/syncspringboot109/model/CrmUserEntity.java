package com.ai2dev.syncspringboot109.model;

import java.time.Instant;
import org.springframework.data.annotation.Id;
import org.springframework.data.relational.core.mapping.Column;
import org.springframework.data.relational.core.mapping.Table;

@Table("crm_users")
public record CrmUserEntity(@Id @Column("user_pk") Long userPk, @Column("zoho_uid") String zohoUid, @Column("given_name") String givenName, @Column("family_name") String familyName, @Column("display_name") String displayName, @Column("email_address") String emailAddress, @Column("phone_number") String phoneNumber, @Column("mobile_number") String mobileNumber, @Column("account_status") String accountStatus, @Column("is_confirmed") Boolean isConfirmed, @Column("user_type") String userType, @Column("zoho_role_id") String zohoRoleId, @Column("zoho_role_name") String zohoRoleName, @Column("zoho_profile_id") String zohoProfileId, @Column("zoho_profile_name") String zohoProfileName, @Column("reports_to_uid") String reportsToUid, @Column("country_code") String countryCode, @Column("locale_code") String localeCode, @Column("iana_timezone") String ianaTimezone, @Column("zoho_created_at") Instant zohoCreatedAt, @Column("zoho_modified_at") Instant zohoModifiedAt, @Column("local_synced_at") Instant localSyncedAt, @Column("local_created_at") Instant localCreatedAt)
{
}

package com.ai2dev.bustravelaccountssa.util;

import java.util.Map;

import com.ai2dev.bustravelaccountssa.model.response.AccountResponse;

public final class AccountMapper {
    private AccountMapper() {
    }

    public static AccountResponse fromRow(Map<String, Object> row) {
        return new AccountResponse(
                string(row.get("id")),
                string(row.get("salutation")),
                string(row.get("first_name")),
                string(row.get("last_name")),
                row.get("person_birthdate") == null ? null : ((java.sql.Date) row.get("person_birthdate")).toLocalDate(),
                string(row.get("phone")),
                string(row.get("mobile_phone")),
                string(row.get("person_email")),
                string(row.get("mailing_street")),
                string(row.get("mailing_postal_code")),
                string(row.get("mailing_city")),
                string(row.get("mailing_country")),
                string(row.get("account_status")),
                string(row.get("account_source")),
                row.get("hotlisted") == null ? null : (Boolean) row.get("hotlisted")
        );
    }

    private static String string(Object value) {
        return value == null ? null : value.toString();
    }
}

package com.ai2dev.bustravelaccounts.dto.response;

import com.fasterxml.jackson.annotation.JsonInclude;

@JsonInclude(JsonInclude.Include.NON_NULL)
public record AccountResponse(
        String id,
        String salutation,
        String firstName,
        String lastName,
        String personBirthdate,
        String phone,
        String mobilePhone,
        String personEmail,
        String mailingStreet,
        String mailingPostalCode,
        String mailingCity,
        String mailingCountry,
        String accountStatus,
        String accountSource,
        Boolean hotlisted) {
}

package com.ai2dev.bustravelaccounts.dto.request;

import com.fasterxml.jackson.annotation.JsonInclude;

@JsonInclude(JsonInclude.Include.NON_NULL)
public record AccountRequest(
        String salutation,
        String firstName,
        String lastName,
        String personEmail,
        String personBirthdate,
        String phone,
        String mobilePhone,
        String mailingStreet,
        String mailingPostalCode,
        String mailingCity,
        String mailingCountry,
        String accountStatus,
        String accountSource) {
}

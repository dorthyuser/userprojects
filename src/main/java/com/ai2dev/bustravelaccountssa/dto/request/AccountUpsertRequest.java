package com.ai2dev.bustravelaccountssa.dto.request;

public record AccountUpsertRequest(
    String salutation,
    String firstName,
    String lastName,
    String phone,
    String mailingStreet,
    String mailingCity,
    String mailingPostalCode,
    String mailingCountry,
    String mobilePhone,
    String personEmail,
    String personBirthdate,
    String accountStatus,
    String accountSource
) {
}

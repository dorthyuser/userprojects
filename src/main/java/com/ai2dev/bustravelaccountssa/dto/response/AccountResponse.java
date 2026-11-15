package com.ai2dev.bustravelaccountssa.dto.response;

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
    Boolean hotlisted
) {
}

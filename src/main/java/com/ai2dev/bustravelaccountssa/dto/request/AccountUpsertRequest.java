package com.ai2dev.bustravelaccountssa.dto.request;

import java.time.LocalDate;

public record AccountUpsertRequest(
    String lastName,
    String firstName,
    String salutation,
    String phone,
    String mailingStreet,
    String mailingCity,
    String mailingPostalCode,
    String mailingCountry,
    String mobilePhone,
    String personEmail,
    LocalDate personBirthdate,
    String accountStatus,
    String accountSource
) {
}

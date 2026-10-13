package com.ai2dev.bustravelaccountssa.model.request;

public record AccountUpdateRequest(
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
        String accountSource,
        Boolean hotlisted) {
}

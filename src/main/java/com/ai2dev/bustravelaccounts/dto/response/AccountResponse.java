package com.ai2dev.bustravelaccounts.dto.response;

import com.fasterxml.jackson.annotation.JsonInclude;
import java.time.LocalDate;
import java.util.UUID;

@JsonInclude(JsonInclude.Include.NON_NULL)
public record AccountResponse(
        UUID id,
        String salutation,
        String firstName,
        String lastName,
        String personEmail,
        LocalDate personBirthdate,
        String phone,
        String mobilePhone,
        String mailingStreet,
        String mailingPostalCode,
        String mailingCity,
        String mailingCountry,
        String accountStatus,
        String accountSource,
        Boolean hotlisted
) {}

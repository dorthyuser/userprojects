package com.ai2dev.bustravelaccounts.model.request;

import com.fasterxml.jackson.annotation.JsonInclude;
import java.time.LocalDate;

public record AccountUpsertRequest(
    String salutation,
    String firstName,
    String lastName,
    @JsonInclude(JsonInclude.Include.NON_NULL) LocalDate personBirthdate,
    @JsonInclude(JsonInclude.Include.NON_NULL) String phone,
    @JsonInclude(JsonInclude.Include.NON_NULL) String mobilePhone,
    String personEmail,
    String mailingStreet,
    String mailingCity,
    String mailingPostalCode,
    String mailingCountry,
    String accountStatus,
    String accountSource
) {}

package com.ai2dev.bustravelaccounts.model.response;

import com.fasterxml.jackson.annotation.JsonInclude;
import java.time.LocalDate;

public record AccountResponse(
    String id,
    String salutation,
    String firstName,
    String lastName,
    @JsonInclude(JsonInclude.Include.NON_NULL) LocalDate personBirthdate,
    @JsonInclude(JsonInclude.Include.NON_NULL) String phone,
    @JsonInclude(JsonInclude.Include.NON_NULL) String mobilePhone,
    String personEmail,
    String mailingStreet,
    String mailingPostalCode,
    String mailingCity,
    String mailingCountry,
    String accountStatus,
    String accountSource,
    Boolean hotlisted
) {}

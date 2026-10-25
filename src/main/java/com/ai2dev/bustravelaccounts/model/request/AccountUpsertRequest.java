package com.ai2dev.bustravelaccounts.model.request;

import com.fasterxml.jackson.annotation.JsonProperty;
import java.time.LocalDate;

public record AccountUpsertRequest(
    @JsonProperty("salutation") String salutation,
    @JsonProperty("firstName") String firstName,
    @JsonProperty("lastName") String lastName,
    @JsonProperty("personBirthdate") LocalDate personBirthdate,
    @JsonProperty("phone") String phone,
    @JsonProperty("mobilePhone") String mobilePhone,
    @JsonProperty("personEmail") String personEmail,
    @JsonProperty("mailingStreet") String mailingStreet,
    @JsonProperty("mailingCity") String mailingCity,
    @JsonProperty("mailingPostalCode") String mailingPostalCode,
    @JsonProperty("mailingCountry") String mailingCountry,
    @JsonProperty("accountStatus") String accountStatus,
    @JsonProperty("accountSource") String accountSource
) {}

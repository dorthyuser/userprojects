package com.ai2dev.bustravelaccounts.model.request;

import com.fasterxml.jackson.annotation.JsonInclude;
import com.fasterxml.jackson.annotation.JsonProperty;
import java.time.LocalDate;

@JsonInclude(JsonInclude.Include.NON_NULL)
public record AccountRequest(
        @JsonProperty("salutation") String salutation,
        @JsonProperty("firstName") String firstName,
        @JsonProperty("lastName") String lastName,
        @JsonProperty("personEmail") String personEmail,
        @JsonProperty("personBirthdate") LocalDate personBirthdate,
        @JsonProperty("phone") String phone,
        @JsonProperty("mobilePhone") String mobilePhone,
        @JsonProperty("mailingStreet") String mailingStreet,
        @JsonProperty("mailingPostalCode") String mailingPostalCode,
        @JsonProperty("mailingCity") String mailingCity,
        @JsonProperty("mailingCountry") String mailingCountry,
        @JsonProperty("accountStatus") String accountStatus,
        @JsonProperty("accountSource") String accountSource
) {}

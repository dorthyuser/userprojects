package com.ai2dev.bustravelaccounts.model.response;

import com.fasterxml.jackson.annotation.JsonInclude;
import com.fasterxml.jackson.annotation.JsonProperty;
import java.time.LocalDate;

@JsonInclude(JsonInclude.Include.NON_NULL)
public record AccountResponse(
        String id,
        String salutation,
        @JsonProperty("firstName") String firstName,
        @JsonProperty("lastName") String lastName,
        @JsonProperty("personBirthdate") LocalDate personBirthdate,
        String phone,
        @JsonProperty("mobilePhone") String mobilePhone,
        @JsonProperty("personEmail") String personEmail,
        @JsonProperty("mailingStreet") String mailingStreet,
        @JsonProperty("mailingPostalCode") String mailingPostalCode,
        @JsonProperty("mailingCity") String mailingCity,
        @JsonProperty("mailingCountry") String mailingCountry,
        @JsonProperty("accountStatus") String accountStatus,
        @JsonProperty("accountSource") String accountSource,
        Boolean hotlisted) {
}

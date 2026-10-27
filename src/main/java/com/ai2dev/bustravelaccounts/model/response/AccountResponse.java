package com.ai2dev.bustravelaccounts.model.response;

import com.fasterxml.jackson.annotation.JsonInclude;
import com.fasterxml.jackson.annotation.JsonProperty;

@JsonInclude(JsonInclude.Include.NON_NULL)
public record AccountResponse(
        @JsonProperty("id") String id,
        @JsonProperty("salutation") String salutation,
        @JsonProperty("firstName") String firstName,
        @JsonProperty("lastName") String lastName,
        @JsonProperty("personBirthdate") String personBirthdate,
        @JsonProperty("phone") String phone,
        @JsonProperty("mobilePhone") String mobilePhone,
        @JsonProperty("personEmail") String personEmail,
        @JsonProperty("mailingStreet") String mailingStreet,
        @JsonProperty("mailingPostalCode") String mailingPostalCode,
        @JsonProperty("mailingCity") String mailingCity,
        @JsonProperty("mailingCountry") String mailingCountry,
        @JsonProperty("accountStatus") String accountStatus,
        @JsonProperty("accountSource") String accountSource,
        @JsonProperty("hotlisted") Boolean hotlisted) {
}

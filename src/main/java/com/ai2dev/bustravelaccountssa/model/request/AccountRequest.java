package com.ai2dev.bustravelaccountssa.model.request;

import com.fasterxml.jackson.annotation.JsonProperty;

public record AccountRequest(
        String salutation,
        @JsonProperty("firstName") String firstName,
        @JsonProperty("lastName") String lastName,
        @JsonProperty("personEmail") String personEmail,
        @JsonProperty("personBirthdate") String personBirthdate,
        String phone,
        @JsonProperty("mobilePhone") String mobilePhone,
        @JsonProperty("mailingStreet") String mailingStreet,
        @JsonProperty("mailingPostalCode") String mailingPostalCode,
        @JsonProperty("mailingCity") String mailingCity,
        @JsonProperty("mailingCountry") String mailingCountry,
        @JsonProperty("accountStatus") String accountStatus,
        @JsonProperty("accountSource") String accountSource,
        @JsonProperty("hotlisted") Boolean hotlisted) {}

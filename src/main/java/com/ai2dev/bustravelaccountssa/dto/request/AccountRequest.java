package com.ai2dev.bustravelaccountssa.dto.request;

import com.fasterxml.jackson.annotation.JsonInclude;

@JsonInclude(JsonInclude.Include.NON_NULL)
public record AccountRequest(String salutation, String firstName, String lastName, String phone, String mobilePhone, String personEmail, String personBirthdate, String mailingStreet, String mailingPostalCode, String mailingCity, String mailingCountry, String accountStatus, String accountSource) {}

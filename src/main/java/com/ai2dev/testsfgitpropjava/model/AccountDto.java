package com.ai2dev.testsfgitpropjava.model;

import jakarta.validation.constraints.NotBlank;

public record AccountDto(
    String id,
    @NotBlank String name,
    String type,
    String industry,
    String phone,
    String website,
    String email,
    String billingStreet,
    String billingCity,
    String billingState,
    String billingPostalCode,
    String billingCountry
)
{
}

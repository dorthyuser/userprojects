package com.ai2dev.testsfgitpropjava.model;

import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.Size;

public record AccountDto(
    String id,
    @NotBlank @Size(max = 255) String name,
    @Size(max = 320) String email,
    @Size(max = 50) String phone,
    @Size(max = 40) String industry,
    @Size(max = 1000) String description
)
{
}

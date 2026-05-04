package com.ai2dev.testsfgitpropjava.model;

import jakarta.validation.constraints.NotBlank;

public record AccountModel(
        String id,
        @NotBlank(message = "UserId is mandatory") String userId,
        @NotBlank String name,
        String phone,
        String website,
        String industry,
        String description
) {
}

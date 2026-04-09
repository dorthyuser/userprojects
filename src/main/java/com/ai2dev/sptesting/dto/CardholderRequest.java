package com.ai2dev.sptesting.dto;

import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;
import jakarta.validation.constraints.Size;

public record CardholderRequest(
    @NotBlank
    @Size(min = 1, max = 15)
    String cardholderTitle,

    @NotBlank
    @Size(min = 1, max = 100)
    String cardholderForename,

    @NotBlank
    @Size(min = 1, max = 100)
    String cardholderSurname,

    @NotNull
    String cardholderType,

    @NotBlank
    @Size(min = 1, max = 100)
    String cardholderPhotoName,

    String cardholderPhotoRRSKey,

    String cardholderPhotoURL,

    String cardholderPhotoKey
) {}

package com.ai2dev.sb_lambda_testing.dto;

import com.ai2dev.sb_lambda_testing.model.CardholderType;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;
import jakarta.validation.constraints.Pattern;
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
    CardholderType cardholderType,

    @NotBlank
    @Size(min = 1, max = 100)
    String cardholderPhotoName,

    @Size(min = 39, max = 42)
    String cardholderPhotoRRSKey,

    @Size(min = 20, max = 2048)
    @Pattern(regexp = "^(https?://).*")
    String cardholderPhotoURL,

    @Size(min = 39, max = 42)
    String cardholderPhotoKey
) {}

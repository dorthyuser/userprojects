package com.ai2dev.test_sb_codex.dto;

import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;
import jakarta.validation.constraints.Pattern;
import jakarta.validation.constraints.Size;

public record CardholderDto(
    @NotBlank
    @Size(min = 1, max = 15)
    String cardholderTitle,

    @NotBlank
    @Size(min = 1, max = 100)
    String cardholderForename,

    @NotBlank
    @Size(min = 1, max = 100)
    String cardholderSurname,

    @NotBlank
    @Pattern(regexp = "^(Primary|Secondary)$")
    String cardholderType,

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

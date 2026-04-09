package com.ai2dev.sb_lambda_testng_2.dto;

import com.ai2dev.sb_lambda_testng_2.model.CardholderType;
import jakarta.validation.constraints.NotNull;
import jakarta.validation.constraints.Pattern;
import jakarta.validation.constraints.Size;

public record CardholderRequest(
        @NotNull
        @Size(min = 1, max = 15)
        String cardholderTitle,

        @NotNull
        @Size(min = 1, max = 100)
        String cardholderForename,

        @NotNull
        @Size(min = 1, max = 100)
        String cardholderSurname,

        @NotNull
        CardholderType cardholderType,

        @NotNull
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

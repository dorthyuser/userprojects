package com.ai2dev.testing330.dto;

import com.ai2dev.testing330.model.CardholderTypeEnum;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;
import jakarta.validation.constraints.Pattern;
import jakarta.validation.constraints.Size;

public record CreateCardholderDto(
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
        CardholderTypeEnum cardholderType,
        @NotBlank
        @Size(min = 1, max = 100)
        String cardholderPhotoName,
        @Pattern(regexp = "^[A-Za-z0-9-]{36}\\.[A-Za-z0-9]{2,5}$")
        @Size(min = 39, max = 42)
        String cardholderPhotoRRSKey,
        @Pattern(regexp = "^(https?://)[A-Za-z0-9._~:/?#@!$&'()*+,;=%-]+$")
        @Size(min = 20, max = 2048)
        String cardholderPhotoURL,
        @Pattern(regexp = "^[A-Za-z0-9-]{36}\\.[A-Za-z0-9]{2,5}$")
        @Size(min = 39, max = 42)
        String cardholderPhotoKey)
{
}
package com.ai2dev.springboottravelcardapi.model.dto;

import com.ai2dev.springboottravelcardapi.model.CardholderType;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;
import jakarta.validation.constraints.Pattern;
import jakarta.validation.constraints.Size;

public record CardholderRequestDto(
        @NotBlank @Size(max = 15) String cardholderTitle,
        @NotBlank @Size(max = 100) String cardholderForename,
        @NotBlank @Size(max = 100) String cardholderSurname,
        @NotNull CardholderType cardholderType,
        @NotBlank @Size(max = 100) String cardholderPhotoName,
        @Size(min = 39, max = 42) @Pattern(regexp = "^[A-Za-z0-9-]{36}\\.[A-Za-z0-9]{2,5}$") String cardholderPhotoRRSKey,
        @Size(min = 20, max = 2048) @Pattern(regexp = "^(https?://)[A-Za-z0-9._~:/?#@!$&'()*+,;=%-]+$") String cardholderPhotoURL,
        @Size(min = 39, max = 42) @Pattern(regexp = "^[A-Za-z0-9-]{36}\\.[A-Za-z0-9]{2,5}$") String cardholderPhotoKey)
{
}

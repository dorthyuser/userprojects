package com.ai2dev.travelcardspringboot929.dto;

import com.ai2dev.travelcardspringboot929.model.CardholderType;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;
import jakarta.validation.constraints.Pattern;
import jakarta.validation.constraints.Size;

public record CardholderRequestDto(
        @NotBlank @Size(min = 1, max = 15) String cardholderTitle,
        @NotBlank @Size(min = 1, max = 100) String cardholderForename,
        @NotBlank @Size(min = 1, max = 100) String cardholderSurname,
        @NotNull CardholderType cardholderType,
        @NotBlank @Size(min = 1, max = 100) String cardholderPhotoName,
        @Size(min = 39, max = 42) @Pattern(regexp = "^[A-Za-z0-9-]{36}\\.[A-Za-z0-9]{2,5}$") String cardholderPhotoRRSKey,
        @Size(min = 20, max = 2048) @Pattern(regexp = "^(https?://)[A-Za-z0-9._~:/?#@!$&'()*+,;=%-]+$") String cardholderPhotoURL,
        @Size(min = 39, max = 42) @Pattern(regexp = "^[A-Za-z0-9-]{36}\\.[A-Za-z0-9]{2,5}$") String cardholderPhotoKey) {
}

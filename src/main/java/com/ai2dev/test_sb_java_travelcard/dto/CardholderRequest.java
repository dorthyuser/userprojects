package com.ai2dev.test_sb_java_travelcard.dto;

import com.ai2dev.test_sb_java_travelcard.model.enums.CardholderType;
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
    @Pattern(regexp = "^(https?://)[A-Za-z0-9._~:/?#@!$&'()*+,;=%-]+$")
    String cardholderPhotoURL,

    @Size(min = 39, max = 42)
    String cardholderPhotoKey
) {
}

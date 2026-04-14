package com.ai2dev.demo_travelcards_spring.dto;

import com.ai2dev.demo_travelcards_spring.model.CardholderType;
import jakarta.validation.constraints.NotNull;
import jakarta.validation.constraints.Size;

public record CardholderRequest(
        @NotNull @Size(min = 1, max = 15) String cardholderTitle,
        @NotNull @Size(min = 1, max = 100) String cardholderForename,
        @NotNull @Size(min = 1, max = 100) String cardholderSurname,
        @NotNull CardholderType cardholderType,
        @NotNull @Size(min = 1, max = 100) String cardholderPhotoName,
        String cardholderPhotoURL,
        String cardholderPhotoKey
) {
}

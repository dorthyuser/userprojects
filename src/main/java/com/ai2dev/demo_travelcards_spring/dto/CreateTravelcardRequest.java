package com.ai2dev.demo_travelcards_spring.dto;

import com.ai2dev.demo_travelcards_spring.model.CardholderType;
import com.ai2dev.demo_travelcards_spring.model.TravelcardType;
import java.time.OffsetDateTime;
import java.util.List;
import jakarta.validation.Valid;
import jakarta.validation.constraints.NotNull;
import jakarta.validation.constraints.Pattern;
import jakarta.validation.constraints.Size;

public record CreateTravelcardRequest(
    @NotNull
    TravelcardType travelcardType,

    @NotNull
    OffsetDateTime travelcardValidFrom,

    @NotNull
    OffsetDateTime travelcardValidTo,

    @Pattern(regexp = "^[A-Za-z0-9 ]*$")
    @Size(max = 255)
    String travelcardName,

    @NotNull
    @Size(min = 11, max = 22)
    @Pattern(regexp = "^[A-Za-z0-9]+$")
    String travelcardNumber,

    @NotNull
    OffsetDateTime travelcardRequestedDate,

    @NotNull
    @Pattern(regexp = "^[0-9]{2}[A-Z0-9]{4}[0-9]{4}[0-9]{5}$")
    String travelcardTransactionReference,

    OffsetDateTime travelcardUsableTo,

    @NotNull
    @Size(min = 1, max = 2)
    @Valid
    List<CardholderRequest> cardholders
) {
    public static record CardholderRequest(
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

        @Size(min = 20, max = 2048)
        String cardholderPhotoURL,

        @Size(min = 39, max = 42)
        String cardholderPhotoKey
    ) {}
}

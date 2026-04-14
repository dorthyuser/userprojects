package com.ai2dev.demo_travelcards_spring.dto;

import com.ai2dev.demo_travelcards_spring.model.CardholderType;
import com.ai2dev.demo_travelcards_spring.model.TravelcardType;
import java.time.OffsetDateTime;
import jakarta.validation.constraints.NotNull;
import jakarta.validation.constraints.Size;

public record TravelcardRequest(
        @NotNull TravelcardType travelcardType,
        @NotNull OffsetDateTime travelcardValidFrom,
        @NotNull OffsetDateTime travelcardValidTo,
        @Size(max = 255) String travelcardName,
        @NotNull @Size(min = 11, max = 22) String travelcardNumber,
        @NotNull OffsetDateTime travelcardRequestedDate,
        @NotNull @Size(min = 15, max = 15) String travelcardTransactionReference,
        OffsetDateTime travelcardUsableTo,
        @NotNull CardholderRequest[] cardholders
) {
}

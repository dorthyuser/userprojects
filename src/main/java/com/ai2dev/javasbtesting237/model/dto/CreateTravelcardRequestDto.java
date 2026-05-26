package com.ai2dev.javasbtesting237.model.dto;

import jakarta.validation.Valid;
import jakarta.validation.constraints.AssertTrue;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotEmpty;
import jakarta.validation.constraints.NotNull;
import jakarta.validation.constraints.Pattern;
import jakarta.validation.constraints.Size;
import java.time.OffsetDateTime;
import java.util.List;

public record CreateTravelcardRequestDto(
        @NotNull TravelcardType travelcardType,
        @NotNull OffsetDateTime travelcardValidFrom,
        @NotNull OffsetDateTime travelcardValidTo,
        @Size(max = 255)
        @Pattern(regexp = "^[A-Za-z0-9 ]*$", message = "invalid name format")
        String travelcardName,
        @NotBlank
        @Size(min = 11, max = 22)
        @Pattern(regexp = "^[A-Za-z0-9]+$", message = "invalid number format")
        String travelcardNumber,
        @NotNull OffsetDateTime travelcardRequestedDate,
        @NotBlank
        @Size(min = 15, max = 15)
        @Pattern(regexp = "^[0-9]{2}[A-Z0-9]{4}[0-9]{4}[0-9]{5}$", message = "invalid transaction reference format")
        String travelcardTransactionReference,
        OffsetDateTime travelcardUsableTo,
        @NotEmpty List<@Valid CardholderRequestDto> cardholders) {

    @AssertTrue(message = "travelcardUsableTo is required for SixteenToSeventeen and forbidden otherwise")
    public boolean isUsableToValidForType() {
        return travelcardType == TravelcardType.SixteenToSeventeen
                ? travelcardUsableTo != null
                : travelcardUsableTo == null;
    }
}

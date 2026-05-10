package com.ai2dev.travelcardsb949.dto;

import com.ai2dev.travelcardsb949.model.TravelcardType;
import jakarta.validation.Valid;
import jakarta.validation.constraints.AssertTrue;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;
import jakarta.validation.constraints.Pattern;
import jakarta.validation.constraints.Size;
import java.time.LocalDateTime;
import java.util.List;

public record CreateTravelcardRequestDto(@NotNull TravelcardType travelcardType, @NotNull LocalDateTime travelcardValidFrom, @NotNull LocalDateTime travelcardValidTo, @Size(max = 255) String travelcardName, @NotBlank @Size(min = 11, max = 22) String travelcardNumber, @NotNull LocalDateTime travelcardRequestedDate, @NotBlank @Pattern(regexp = "^[A-Za-z0-9]{15}$") String travelcardTransactionReference, LocalDateTime travelcardUsableTo, @NotNull @Valid List<CardholderRequestDto> cardholders)
{
    @AssertTrue(message = "cardholders must contain exactly one or two items")
    public boolean isCardholdersSizeValid()
    {
        return cardholders != null && cardholders.size() >= 1 && cardholders.size() <= 2;
    }
}
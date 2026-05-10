package com.ai2dev.travelcardspringboot1248.dto;

import com.ai2dev.travelcardspringboot1248.model.TravelcardType;
import jakarta.validation.Valid;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotEmpty;
import jakarta.validation.constraints.NotNull;
import jakarta.validation.constraints.Size;
import java.time.Instant;
import java.util.List;

public record TravelcardCreateRequestDto(
        @NotNull TravelcardType travelcardType,
        @NotNull Instant travelcardValidFrom,
        @NotNull Instant travelcardValidTo,
        @Size(max = 255) String travelcardName,
        @NotBlank @Size(min = 11, max = 22) String travelcardNumber,
        @NotNull Instant travelcardRequestedDate,
        @NotBlank @Size(min = 15, max = 15) String travelcardTransactionReference,
        Instant travelcardUsableTo,
        @NotEmpty @Size(min = 1, max = 2) @Valid List<CardholderRequestDto> cardholders)
{
}
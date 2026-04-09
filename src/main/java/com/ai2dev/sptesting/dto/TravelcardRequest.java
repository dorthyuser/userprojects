package com.ai2dev.sptesting.dto;

import jakarta.validation.Valid;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;
import jakarta.validation.constraints.Size;
import java.time.Instant;
import java.util.List;

public record TravelcardRequest(
    @NotBlank
    @Size(min = 1, max = 255)
    String travelcardType,

    @NotNull
    Instant travelcardValidFrom,

    @NotNull
    Instant travelcardValidTo,

    @Size(max = 255)
    String travelcardName,

    @NotBlank
    @Size(min = 11, max = 22)
    String travelcardNumber,

    @NotNull
    Instant travelcardRequestedDate,

    @NotBlank
    @Size(min = 15, max = 15)
    String travelcardTransactionReference,

    Instant travelcardUsableTo,

    @NotNull
    @Size(min = 1, max = 2)
    List<@Valid CardholderRequest> cardholders
) {}

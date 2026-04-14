package com.ai2dev.demo_travelcards_spring.dto;

import com.ai2dev.demo_travelcards_spring.model.TravelcardType;
import jakarta.validation.Valid;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;
import jakarta.validation.constraints.Pattern;
import jakarta.validation.constraints.Size;
import java.time.OffsetDateTime;
import java.util.List;

public record TravelcardRequest(
    @NotNull TravelcardType travelcardType,
    @NotNull OffsetDateTime travelcardValidFrom,
    @NotNull OffsetDateTime travelcardValidTo,
    @Size(max = 255) @Pattern(regexp = "^[A-Za-z0-9 ]*$") String travelcardName,
    @NotBlank @Size(min = 11, max = 22) @Pattern(regexp = "^[A-Za-z0-9]+$") String travelcardNumber,
    @NotNull OffsetDateTime travelcardRequestedDate,
    @NotBlank @Size(min = 15, max = 15) @Pattern(regexp = "^[0-9]{2}[A-Z0-9]{4}[0-9]{4}[0-9]{5}$") String travelcardTransactionReference,
    OffsetDateTime travelcardUsableTo,
    @NotNull @Size(min = 1, max = 2) @Valid List<CardholderRequest> cardholders
) {}

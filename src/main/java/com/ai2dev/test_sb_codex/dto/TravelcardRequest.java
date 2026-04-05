package com.ai2dev.test_sb_codex.dto;

import jakarta.validation.Valid;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotEmpty;
import jakarta.validation.constraints.NotNull;
import jakarta.validation.constraints.Pattern;
import jakarta.validation.constraints.Size;
import java.time.Instant;
import java.util.List;

public record TravelcardRequest(
    @NotBlank
    @Pattern(regexp = "^(Young|Barcklays|DevonandCornwall|TwoTogether|Family|Senior|DisabledPersons|Network|TwentySixToThirty|SixteenToSeventeen|Veterans)$")
    String travelcardType,

    @NotNull
    Instant travelcardValidFrom,

    @NotNull
    Instant travelcardValidTo,

    @Size(max = 255)
    @Pattern(regexp = "^[A-Za-z0-9 ]*$")
    String travelcardName,

    @NotBlank
    @Size(min = 11, max = 22)
    @Pattern(regexp = "^[A-Za-z0-9]+$")
    String travelcardNumber,

    @NotNull
    Instant travelcardRequestedDate,

    @NotBlank
    @Pattern(regexp = "^[0-9]{2}[A-Z0-9]{4}[0-9]{4}[0-9]{5}$")
    String travelcardTransactionReference,

    Instant travelcardUsableTo,

    @NotEmpty
    @Valid
    List<CardholderDto> cardholders
) {}

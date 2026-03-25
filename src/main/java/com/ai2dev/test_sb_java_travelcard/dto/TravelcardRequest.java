package com.ai2dev.test_sb_java_travelcard.dto;

import com.ai2dev.test_sb_java_travelcard.model.enums.TravelcardType;
import jakarta.validation.Valid;
import jakarta.validation.constraints.NotNull;
import jakarta.validation.constraints.Pattern;
import jakarta.validation.constraints.Size;
import java.time.OffsetDateTime;
import java.util.List;

public record TravelcardRequest(
    @NotNull
    TravelcardType travelcardType,

    @NotNull
    OffsetDateTime travelcardValidFrom,

    @NotNull
    OffsetDateTime travelcardValidTo,

    @Size(max = 255)
    @Pattern(regexp = "^[A-Za-z0-9 ]*$")
    String travelcardName,

    @NotNull
    @Size(min = 11, max = 22)
    @Pattern(regexp = "^[A-Za-z0-9]+$")
    String travelcardNumber,

    @NotNull
    OffsetDateTime travelcardRequestedDate,

    @NotNull
    @Size(min = 15, max = 15)
    String travelcardTransactionReference,

    OffsetDateTime travelcardUsableTo,

    @NotNull
    @Valid
    List<CardholderRequest> cardholders
) {
}

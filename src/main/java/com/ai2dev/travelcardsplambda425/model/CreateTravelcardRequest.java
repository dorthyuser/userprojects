package com.ai2dev.travelcardsplambda425.model;

import com.fasterxml.jackson.annotation.JsonFormat;
import jakarta.validation.Valid;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;
import jakarta.validation.constraints.Pattern;
import jakarta.validation.constraints.Size;
import java.time.OffsetDateTime;
import java.util.List;

public record CreateTravelcardRequest(
        @NotBlank
        @Size(min = 1, max = 128)
        String client_id,
        @NotNull
        TravelcardType travelcardType,
        @NotNull
        @JsonFormat(pattern = "yyyy-MM-dd'T'HH:mm:ssXXX")
        OffsetDateTime travelcardValidFrom,
        @NotNull
        @JsonFormat(pattern = "yyyy-MM-dd'T'HH:mm:ssXXX")
        OffsetDateTime travelcardValidTo,
        @Size(max = 255)
        String travelcardName,
        @NotBlank
        @Size(min = 11, max = 22)
        @Pattern(regexp = "^[A-Za-z0-9]+$")
        String travelcardNumber,
        @NotNull
        @JsonFormat(pattern = "yyyy-MM-dd'T'HH:mm:ssXXX")
        OffsetDateTime travelcardRequestedDate,
        @NotBlank
        @Size(min = 15, max = 15)
        String travelcardTransactionReference,
        @JsonFormat(pattern = "yyyy-MM-dd'T'HH:mm:ssXXX")
        OffsetDateTime travelcardUsableTo,
        @NotNull
        @Valid
        List<CardholderRequest> cardholders
) {
}
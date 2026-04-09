package com.ai2dev.sb_lambda_testng_2.dto;

import com.ai2dev.sb_lambda_testng_2.model.TravelcardType;
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
        String travelcardName,

        @NotNull
        @Size(min = 11, max = 22)
        String travelcardNumber,

        @NotNull
        OffsetDateTime travelcardRequestedDate,

        @NotNull
        @Pattern(regexp = "^.{15}$")
        String travelcardTransactionReference,

        OffsetDateTime travelcardUsableTo,

        @NotNull
        @Valid
        List<CardholderRequest> cardholders
) {}

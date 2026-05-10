package com.ai2dev.travelcardsb1005.dto;

import com.ai2dev.travelcardsb1005.model.TravelcardTypeEnum;
import jakarta.validation.Valid;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotEmpty;
import jakarta.validation.constraints.NotNull;
import jakarta.validation.constraints.Pattern;
import jakarta.validation.constraints.Size;
import java.time.OffsetDateTime;
import java.util.List;

public record TravelcardRequestDto(
    @NotNull
    TravelcardTypeEnum travelcardType,
    @NotNull
    OffsetDateTime travelcardValidFrom,
    @NotNull
    OffsetDateTime travelcardValidTo,
    @Size(max = 255)
    @Pattern(regexp = "^[A-Za-z0-9 ]*$")
    String travelcardName,
    @NotBlank
    @Size(min = 11, max = 22)
    @Pattern(regexp = "^[A-Za-z0-9]+$")
    String travelcardNumber,
    @NotNull
    OffsetDateTime travelcardRequestedDate,
    @NotBlank
    @Size(min = 15, max = 15)
    String travelcardTransactionReference,
    OffsetDateTime travelcardUsableTo,
    @NotEmpty
    @Valid
    List<CardholderRequestDto> cardholders)
{
}

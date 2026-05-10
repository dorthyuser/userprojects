package com.ai2dev.travelcardspringboot501.dto;

import com.ai2dev.travelcardspringboot501.model.CardholderTypeEnum;
import com.ai2dev.travelcardspringboot501.model.TravelcardTypeEnum;
import jakarta.validation.Valid;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotEmpty;
import jakarta.validation.constraints.NotNull;
import jakarta.validation.constraints.Size;
import java.time.OffsetDateTime;
import java.util.List;

public record TravelcardCreateRequestDto(
        @NotNull TravelcardTypeEnum travelcardType,
        @NotNull OffsetDateTime travelcardValidFrom,
        @NotNull OffsetDateTime travelcardValidTo,
        @Size(max = 255) String travelcardName,
        @NotBlank @Size(min = 11, max = 22) String travelcardNumber,
        @NotNull OffsetDateTime travelcardRequestedDate,
        @NotBlank @Size(min = 15, max = 15) String travelcardTransactionReference,
        OffsetDateTime travelcardUsableTo,
        @NotEmpty @Size(min = 1, max = 2) List<@Valid CardholderRequestDto> cardholders)
{
}

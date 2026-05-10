package com.ai2dev.travelcardspringboot929.dto;

import com.ai2dev.travelcardspringboot929.model.TravelcardType;
import jakarta.validation.Valid;
import jakarta.validation.constraints.AssertTrue;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;
import jakarta.validation.constraints.Size;
import java.time.ZonedDateTime;
import java.util.List;

public record TravelcardRequestDto(
        @NotNull TravelcardType travelcardType,
        @NotNull ZonedDateTime travelcardValidFrom,
        @NotNull ZonedDateTime travelcardValidTo,
        @Size(max = 255) String travelcardName,
        @NotBlank @Size(min = 11, max = 22) String travelcardNumber,
        @NotNull ZonedDateTime travelcardRequestedDate,
        @NotBlank @Size(min = 15, max = 15) String travelcardTransactionReference,
        ZonedDateTime travelcardUsableTo,
        @NotNull @Size(min = 1, max = 2) List<@Valid CardholderRequestDto> cardholders) {

    @AssertTrue(message = "travelcardRequestedDate must be in the past")
    public boolean isTravelcardRequestedDateValid() {
        return travelcardRequestedDate != null && travelcardRequestedDate.isBefore(ZonedDateTime.now(travelcardRequestedDate.getZone()));
    }

    @AssertTrue(message = "travelcardValidFrom must not be later than travelcardValidTo")
    public boolean isValidFromBeforeValidTo() {
        return travelcardValidFrom != null && travelcardValidTo != null && !travelcardValidFrom.isAfter(travelcardValidTo);
    }

    @AssertTrue(message = "travelcardValidTo must be in the future")
    public boolean isValidToInFuture() {
        return travelcardValidTo != null && travelcardValidTo.isAfter(ZonedDateTime.now(travelcardValidTo.getZone()));
    }

    @AssertTrue(message = "travelcardValidFrom must not be later than one calendar month from today")
    public boolean isValidFromWithinOneMonth() {
        if (travelcardValidFrom == null) return true;
        ZonedDateTime now = ZonedDateTime.now(travelcardValidFrom.getZone());
        return !travelcardValidFrom.isAfter(now.plusMonths(1));
    }

    @AssertTrue(message = "travelcardUsableTo must be in the future")
    public boolean isUsableToInFuture() {
        if (travelcardUsableTo == null) return true;
        return travelcardUsableTo.isAfter(ZonedDateTime.now(travelcardUsableTo.getZone()));
    }

    @AssertTrue(message = "travelcardUsableTo is required for SixteenToSeventeen")
    public boolean isUsableToRequiredForSixteenToSeventeen() {
        return travelcardType != TravelcardType.SixteenToSeventeen || travelcardUsableTo != null;
    }

    @AssertTrue(message = "secondary cardholder is not allowed for this travelcard type")
    public boolean isSecondaryAllowed() {
        if (cardholders == null) return true;
        if (travelcardType != TravelcardType.SixteenToSeventeen && travelcardType != TravelcardType.Veterans) return true;
        return cardholders.stream().noneMatch(cardholder -> cardholder.cardholderType() == com.ai2dev.travelcardspringboot929.model.CardholderType.Secondary);
    }

    @AssertTrue(message = "exactly one primary cardholder is required")
    public boolean isExactlyOnePrimary() {
        if (cardholders == null) return true;
        return cardholders.stream().filter(cardholder -> cardholder.cardholderType() == com.ai2dev.travelcardspringboot929.model.CardholderType.Primary).count() == 1;
    }
}

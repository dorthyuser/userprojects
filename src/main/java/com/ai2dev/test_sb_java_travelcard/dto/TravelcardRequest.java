package com.ai2dev.test_sb_java_travelcard.dto;

import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;
import jakarta.validation.constraints.Pattern;
import jakarta.validation.constraints.Size;
import java.time.OffsetDateTime;
import java.util.List;

public class TravelcardRequest {

    @NotBlank
    private String travelcardType;

    @NotNull
    private OffsetDateTime travelcardValidFrom;

    @NotNull
    private OffsetDateTime travelcardValidTo;

    @Size(max = 255)
    @Pattern(regexp = "^[A-Za-z0-9 ]*$")
    private String travelcardName;

    @NotBlank
    @Size(min = 11, max = 22)
    @Pattern(regexp = "^[A-Za-z0-9]+$")
    private String travelcardNumber;

    @NotNull
    private OffsetDateTime travelcardRequestedDate;

    @NotBlank
    @Size(min = 15, max = 15)
    private String travelcardTransactionReference;

    private OffsetDateTime travelcardUsableTo;

    @NotNull
    private List<CardholderDto> cardholders;

    public TravelcardRequest() {
    }

    public String travelcardType() {
        return travelcardType;
    }

    public void setTravelcardType(String travelcardType) {
        this.travelcardType = travelcardType;
    }

    public OffsetDateTime travelcardValidFrom() {
        return travelcardValidFrom;
    }

    public void setTravelcardValidFrom(OffsetDateTime travelcardValidFrom) {
        this.travelcardValidFrom = travelcardValidFrom;
    }

    public OffsetDateTime travelcardValidTo() {
        return travelcardValidTo;
    }

    public void setTravelcardValidTo(OffsetDateTime travelcardValidTo) {
        this.travelcardValidTo = travelcardValidTo;
    }

    public String travelcardName() {
        return travelcardName;
    }

    public void setTravelcardName(String travelcardName) {
        this.travelcardName = travelcardName;
    }

    public String travelcardNumber() {
        return travelcardNumber;
    }

    public void setTravelcardNumber(String travelcardNumber) {
        this.travelcardNumber = travelcardNumber;
    }

    public OffsetDateTime travelcardRequestedDate() {
        return travelcardRequestedDate;
    }

    public void setTravelcardRequestedDate(OffsetDateTime travelcardRequestedDate) {
        this.travelcardRequestedDate = travelcardRequestedDate;
    }

    public String travelcardTransactionReference() {
        return travelcardTransactionReference;
    }

    public void setTravelcardTransactionReference(String travelcardTransactionReference) {
        this.travelcardTransactionReference = travelcardTransactionReference;
    }

    public OffsetDateTime travelcardUsableTo() {
        return travelcardUsableTo;
    }

    public void setTravelcardUsableTo(OffsetDateTime travelcardUsableTo) {
        this.travelcardUsableTo = travelcardUsableTo;
    }

    public List<CardholderDto> cardholders() {
        return cardholders;
    }

    public void setCardholders(List<CardholderDto> cardholders) {
        this.cardholders = cardholders;
    }

    @Override
    public String toString() {
        return "TravelcardRequest{" +
            "travelcardType='" + travelcardType + '\'' +
            ", travelcardNumber='" + travelcardNumber + '\'' +
            '}';
    }
}

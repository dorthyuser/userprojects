package com.ai2dev.test_tc_sb_api.dto;

import com.ai2dev.test_tc_sb_api.model.TravelcardType;

import jakarta.validation.Valid;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;
import jakarta.validation.constraints.Pattern;
import jakarta.validation.constraints.Size;
import java.time.OffsetDateTime;
import java.util.List;

public class TravelcardRequest {

    @NotNull
    private TravelcardType travelcardType;

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
    @Pattern(regexp = "^[0-9]{2}[A-Z0-9]{4}[0-9]{4}[0-9]{5}$")
    private String travelcardTransactionReference;

    private OffsetDateTime travelcardUsableTo;

    @NotNull
    @Size(min = 1, max = 2)
    @Valid
    private List<CardholderRequest> cardholders;

    public TravelcardRequest() {
    }

    public TravelcardType getTravelcardType() {
        return travelcardType;
    }

    public void setTravelcardType(TravelcardType travelcardType) {
        this.travelcardType = travelcardType;
    }

    public OffsetDateTime getTravelcardValidFrom() {
        return travelcardValidFrom;
    }

    public void setTravelcardValidFrom(OffsetDateTime travelcardValidFrom) {
        this.travelcardValidFrom = travelcardValidFrom;
    }

    public OffsetDateTime getTravelcardValidTo() {
        return travelcardValidTo;
    }

    public void setTravelcardValidTo(OffsetDateTime travelcardValidTo) {
        this.travelcardValidTo = travelcardValidTo;
    }

    public String getTravelcardName() {
        return travelcardName;
    }

    public void setTravelcardName(String travelcardName) {
        this.travelcardName = travelcardName;
    }

    public String getTravelcardNumber() {
        return travelcardNumber;
    }

    public void setTravelcardNumber(String travelcardNumber) {
        this.travelcardNumber = travelcardNumber;
    }

    public OffsetDateTime getTravelcardRequestedDate() {
        return travelcardRequestedDate;
    }

    public void setTravelcardRequestedDate(OffsetDateTime travelcardRequestedDate) {
        this.travelcardRequestedDate = travelcardRequestedDate;
    }

    public String getTravelcardTransactionReference() {
        return travelcardTransactionReference;
    }

    public void setTravelcardTransactionReference(String travelcardTransactionReference) {
        this.travelcardTransactionReference = travelcardTransactionReference;
    }

    public OffsetDateTime getTravelcardUsableTo() {
        return travelcardUsableTo;
    }

    public void setTravelcardUsableTo(OffsetDateTime travelcardUsableTo) {
        this.travelcardUsableTo = travelcardUsableTo;
    }

    public List<CardholderRequest> getCardholders() {
        return cardholders;
    }

    public void setCardholders(List<CardholderRequest> cardholders) {
        this.cardholders = cardholders;
    }
}

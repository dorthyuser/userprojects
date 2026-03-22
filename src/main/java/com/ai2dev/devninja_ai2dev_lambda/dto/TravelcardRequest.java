package com.ai2dev.devninja_ai2dev_lambda.dto;

import io.micronaut.core.annotation.Introspected;
import java.time.OffsetDateTime;
import java.util.List;

@Introspected
public class TravelcardRequest {
    private static final org.slf4j.Logger LOG = org.slf4j.LoggerFactory.getLogger(TravelcardRequest.class);

    private String travelcardType;

    private OffsetDateTime travelcardValidFrom;

    private OffsetDateTime travelcardValidTo;

    private String travelcardName;

    private String travelcardNumber;

    private OffsetDateTime travelcardRequestedDate;

    private String travelcardTransactionReference;

    private OffsetDateTime travelcardUsableTo;

    private List<CardholderRequest> cardholders;

    public String getTravelcardType() {
        return travelcardType;
    }

    public void setTravelcardType(String travelcardType) {
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

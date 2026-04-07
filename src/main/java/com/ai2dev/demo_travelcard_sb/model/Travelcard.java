package com.ai2dev.demo_travelcard_sb.model;

import org.springframework.data.annotation.Id;
import org.springframework.data.relational.core.mapping.MappedCollection;
import org.springframework.data.relational.core.mapping.Table;
import java.time.OffsetDateTime;
import java.util.Set;

@Table("travelcards")
public class Travelcard {
    @Id
    private Long id;

    private TravelcardType travelcardType;

    private OffsetDateTime travelcardValidFrom;

    private OffsetDateTime travelcardValidTo;

    private String travelcardName;

    private String travelcardNumber;

    private OffsetDateTime travelcardRequestedDate;

    private String travelcardTransactionReference;

    private OffsetDateTime travelcardUsableTo;

    @MappedCollection(idColumn = "travelcard_id")
    private Set<Cardholder> cardholders;

    public Long getId() {
        return id;
    }

    public void setId(Long id) {
        this.id = id;
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

    public Set<Cardholder> getCardholders() {
        return cardholders;
    }

    public void setCardholders(Set<Cardholder> cardholders) {
        this.cardholders = cardholders;
    }
}

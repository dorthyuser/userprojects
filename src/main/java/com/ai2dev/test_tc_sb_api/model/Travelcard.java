package com.ai2dev.test_tc_sb_api.model;

import org.springframework.data.annotation.Id;
import org.springframework.data.relational.core.mapping.Column;
import org.springframework.data.relational.core.mapping.Table;

import java.time.OffsetDateTime;

@Table("travelcards")
public class Travelcard {
    @Id
    private Integer id;

    @Column("travelcard_type")
    private TravelcardType travelcardType;

    @Column("travelcard_valid_from")
    private OffsetDateTime travelcardValidFrom;

    @Column("travelcard_valid_to")
    private OffsetDateTime travelcardValidTo;

    @Column("travelcard_name")
    private String travelcardName;

    @Column("travelcard_number")
    private String travelcardNumber;

    @Column("travelcard_requested_date")
    private OffsetDateTime travelcardRequestedDate;

    @Column("travelcard_transaction_reference")
    private String travelcardTransactionReference;

    @Column("travelcard_usable_to")
    private OffsetDateTime travelcardUsableTo;

    public Travelcard() {
    }

    public Travelcard(Integer id, TravelcardType travelcardType, OffsetDateTime travelcardValidFrom, OffsetDateTime travelcardValidTo, String travelcardName, String travelcardNumber, OffsetDateTime travelcardRequestedDate, String travelcardTransactionReference, OffsetDateTime travelcardUsableTo) {
        this.id = id;
        this.travelcardType = travelcardType;
        this.travelcardValidFrom = travelcardValidFrom;
        this.travelcardValidTo = travelcardValidTo;
        this.travelcardName = travelcardName;
        this.travelcardNumber = travelcardNumber;
        this.travelcardRequestedDate = travelcardRequestedDate;
        this.travelcardTransactionReference = travelcardTransactionReference;
        this.travelcardUsableTo = travelcardUsableTo;
    }

    public Integer getId() {
        return id;
    }

    public void setId(Integer id) {
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
}

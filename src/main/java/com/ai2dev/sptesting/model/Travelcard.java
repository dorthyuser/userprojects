package com.ai2dev.sptesting.model;

import java.time.Instant;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.data.annotation.Id;
import org.springframework.data.relational.core.mapping.Column;
import org.springframework.data.relational.core.mapping.Table;

@Table("travelcards")
public class Travelcard {
    private static final Logger logger = LoggerFactory.getLogger(Travelcard.class);

    @Id
    private Long id;

    @Column("travelcard_type")
    private TravelcardType travelcardType;

    @Column("travelcard_valid_from")
    private Instant travelcardValidFrom;

    @Column("travelcard_valid_to")
    private Instant travelcardValidTo;

    @Column("travelcard_name")
    private String travelcardName;

    @Column("travelcard_number")
    private String travelcardNumber;

    @Column("travelcard_requested_date")
    private Instant travelcardRequestedDate;

    @Column("travelcard_transaction_reference")
    private String travelcardTransactionReference;

    @Column("travelcard_usable_to")
    private Instant travelcardUsableTo;

    public Travelcard() {
        logger.debug("Travelcard.<init>");
    }

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

    public Instant getTravelcardValidFrom() {
        return travelcardValidFrom;
    }

    public void setTravelcardValidFrom(Instant travelcardValidFrom) {
        this.travelcardValidFrom = travelcardValidFrom;
    }

    public Instant getTravelcardValidTo() {
        return travelcardValidTo;
    }

    public void setTravelcardValidTo(Instant travelcardValidTo) {
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

    public Instant getTravelcardRequestedDate() {
        return travelcardRequestedDate;
    }

    public void setTravelcardRequestedDate(Instant travelcardRequestedDate) {
        this.travelcardRequestedDate = travelcardRequestedDate;
    }

    public String getTravelcardTransactionReference() {
        return travelcardTransactionReference;
    }

    public void setTravelcardTransactionReference(String travelcardTransactionReference) {
        this.travelcardTransactionReference = travelcardTransactionReference;
    }

    public Instant getTravelcardUsableTo() {
        return travelcardUsableTo;
    }

    public void setTravelcardUsableTo(Instant travelcardUsableTo) {
        this.travelcardUsableTo = travelcardUsableTo;
    }
}

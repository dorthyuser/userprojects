package com.ai2dev.travelcardsplambda425.model;

import java.time.OffsetDateTime;
import org.springframework.data.annotation.Id;
import org.springframework.data.relational.core.mapping.Column;
import org.springframework.data.relational.core.mapping.Table;

@Table("travelcards")
public class Travelcard {

    @Id
    private Long id;

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

    public Travelcard(Long id, TravelcardType travelcardType, OffsetDateTime travelcardValidFrom, OffsetDateTime travelcardValidTo, String travelcardName, String travelcardNumber, OffsetDateTime travelcardRequestedDate, String travelcardTransactionReference, OffsetDateTime travelcardUsableTo) {
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

    public Long getId() {
        return id;
    }

    public TravelcardType getTravelcardType() {
        return travelcardType;
    }

    public OffsetDateTime getTravelcardValidFrom() {
        return travelcardValidFrom;
    }

    public OffsetDateTime getTravelcardValidTo() {
        return travelcardValidTo;
    }

    public String getTravelcardName() {
        return travelcardName;
    }

    public String getTravelcardNumber() {
        return travelcardNumber;
    }

    public OffsetDateTime getTravelcardRequestedDate() {
        return travelcardRequestedDate;
    }

    public String getTravelcardTransactionReference() {
        return travelcardTransactionReference;
    }

    public OffsetDateTime getTravelcardUsableTo() {
        return travelcardUsableTo;
    }
}
package com.ai2dev.devninja_ai2dev_lambda.model;

import io.micronaut.core.annotation.Introspected;
import io.micronaut.data.annotation.GeneratedValue;
import io.micronaut.data.annotation.Id;
import io.micronaut.data.annotation.MappedEntity;
import java.time.OffsetDateTime;

@MappedEntity("travelcards")
@Introspected
public class Travelcard {
    private static final org.slf4j.Logger LOG = org.slf4j.LoggerFactory.getLogger(Travelcard.class);

    @Id
    @GeneratedValue
    private Integer id;

    private String travelcardType;

    private OffsetDateTime travelcardValidFrom;

    private OffsetDateTime travelcardValidTo;

    private String travelcardName;

    private String travelcardNumber;

    private OffsetDateTime travelcardRequestedDate;

    private String travelcardTransactionReference;

    private OffsetDateTime travelcardUsableTo;

    public Integer getId() {
        LOG.info("{\"event\":\"entry\"}");
        LOG.info("{\"event\":\"exit\",\"id\":\"{}\"}", id);
        return id;
    }

    public void setId(Integer id) {
        LOG.info("{\"event\":\"entry\"}");
        this.id = id;
        LOG.info("{\"event\":\"exit\",\"id\":\"{}\"}", id);
    }

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
}

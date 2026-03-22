package com.ai2dev.devninja_ai2dev_lambda.model;

import io.micronaut.core.annotation.Introspected;
import io.micronaut.data.annotation.GeneratedValue;
import io.micronaut.data.annotation.Id;
import io.micronaut.data.annotation.MappedEntity;

@MappedEntity("cardholders")
@Introspected
public class Cardholder {
    private static final org.slf4j.Logger LOG = org.slf4j.LoggerFactory.getLogger(Cardholder.class);

    @Id
    @GeneratedValue
    private Integer id;

    private Integer travelcardId;

    private String cardholderTitle;

    private String cardholderForename;

    private String cardholderSurname;

    private String cardholderType;

    private String cardholderPhotoName;

    private String cardholderPhotoRRSKey;

    private String cardholderPhotoURL;

    private String cardholderPhotoKey;

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

    public Integer getTravelcardId() {
        return travelcardId;
    }

    public void setTravelcardId(Integer travelcardId) {
        this.travelcardId = travelcardId;
    }

    public String getCardholderTitle() {
        return cardholderTitle;
    }

    public void setCardholderTitle(String cardholderTitle) {
        this.cardholderTitle = cardholderTitle;
    }

    public String getCardholderForename() {
        return cardholderForename;
    }

    public void setCardholderForename(String cardholderForename) {
        this.cardholderForename = cardholderForename;
    }

    public String getCardholderSurname() {
        return cardholderSurname;
    }

    public void setCardholderSurname(String cardholderSurname) {
        this.cardholderSurname = cardholderSurname;
    }

    public String getCardholderType() {
        return cardholderType;
    }

    public void setCardholderType(String cardholderType) {
        this.cardholderType = cardholderType;
    }

    public String getCardholderPhotoName() {
        return cardholderPhotoName;
    }

    public void setCardholderPhotoName(String cardholderPhotoName) {
        this.cardholderPhotoName = cardholderPhotoName;
    }

    public String getCardholderPhotoRRSKey() {
        return cardholderPhotoRRSKey;
    }

    public void setCardholderPhotoRRSKey(String cardholderPhotoRRSKey) {
        this.cardholderPhotoRRSKey = cardholderPhotoRRSKey;
    }

    public String getCardholderPhotoURL() {
        return cardholderPhotoURL;
    }

    public void setCardholderPhotoURL(String cardholderPhotoURL) {
        this.cardholderPhotoURL = cardholderPhotoURL;
    }

    public String getCardholderPhotoKey() {
        return cardholderPhotoKey;
    }

    public void setCardholderPhotoKey(String cardholderPhotoKey) {
        this.cardholderPhotoKey = cardholderPhotoKey;
    }
}

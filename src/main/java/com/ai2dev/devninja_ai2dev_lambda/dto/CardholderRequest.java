package com.ai2dev.devninja_ai2dev_lambda.dto;

import io.micronaut.core.annotation.Introspected;

@Introspected
public class CardholderRequest {
    private static final org.slf4j.Logger LOG = org.slf4j.LoggerFactory.getLogger(CardholderRequest.class);

    private String cardholderTitle;

    private String cardholderForename;

    private String cardholderSurname;

    private String cardholderType;

    private String cardholderPhotoName;

    private String cardholderPhotoRRSKey;

    private String cardholderPhotoURL;

    private String cardholderPhotoKey;

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

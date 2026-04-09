package com.ai2dev.sb_lambda_testing.model;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.data.annotation.Id;
import org.springframework.data.relational.core.mapping.Table;

@Table("cardholders")
public class Cardholder {
    private static final Logger logger = LoggerFactory.getLogger(Cardholder.class);

    @Id
    private Long id;

    private Long travelcardId;

    private String cardholderTitle;

    private String cardholderForename;

    private String cardholderSurname;

    private CardholderType cardholderType;

    private String cardholderPhotoName;

    private String cardholderPhotoRrsKey;

    private String cardholderPhotoUrl;

    private String cardholderPhotoKey;

    public Long getId() {
        return id;
    }

    public void setId(Long id) {
        this.id = id;
    }

    public Long getTravelcardId() {
        return travelcardId;
    }

    public void setTravelcardId(Long travelcardId) {
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

    public CardholderType getCardholderType() {
        return cardholderType;
    }

    public void setCardholderType(CardholderType cardholderType) {
        this.cardholderType = cardholderType;
    }

    public String getCardholderPhotoName() {
        return cardholderPhotoName;
    }

    public void setCardholderPhotoName(String cardholderPhotoName) {
        this.cardholderPhotoName = cardholderPhotoName;
    }

    public String getCardholderPhotoRrsKey() {
        return cardholderPhotoRrsKey;
    }

    public void setCardholderPhotoRrsKey(String cardholderPhotoRrsKey) {
        this.cardholderPhotoRrsKey = cardholderPhotoRrsKey;
    }

    public String getCardholderPhotoUrl() {
        return cardholderPhotoUrl;
    }

    public void setCardholderPhotoUrl(String cardholderPhotoUrl) {
        this.cardholderPhotoUrl = cardholderPhotoUrl;
    }

    public String getCardholderPhotoKey() {
        return cardholderPhotoKey;
    }

    public void setCardholderPhotoKey(String cardholderPhotoKey) {
        this.cardholderPhotoKey = cardholderPhotoKey;
    }
}

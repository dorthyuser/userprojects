package com.ai2dev.sptesting.model;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.data.annotation.Id;
import org.springframework.data.relational.core.mapping.Column;
import org.springframework.data.relational.core.mapping.Table;

@Table("cardholders")
public class Cardholder {
    private static final Logger logger = LoggerFactory.getLogger(Cardholder.class);

    @Id
    private Long id;

    @Column("travelcard_id")
    private Long travelcardId;

    @Column("cardholder_title")
    private String cardholderTitle;

    @Column("cardholder_forename")
    private String cardholderForename;

    @Column("cardholder_surname")
    private String cardholderSurname;

    @Column("cardholder_type")
    private CardholderType cardholderType;

    @Column("cardholder_photo_name")
    private String cardholderPhotoName;

    @Column("cardholder_photo_rrs_key")
    private String cardholderPhotoRrsKey;

    @Column("cardholder_photo_url")
    private String cardholderPhotoUrl;

    @Column("cardholder_photo_key")
    private String cardholderPhotoKey;

    public Cardholder() {
        logger.debug("Cardholder.<init>");
    }

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

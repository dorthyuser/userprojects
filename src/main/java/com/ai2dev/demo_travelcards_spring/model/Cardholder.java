package com.ai2dev.demo_travelcards_spring.model;

import org.springframework.data.annotation.Id;
import org.springframework.data.relational.core.mapping.Column;
import org.springframework.data.relational.core.mapping.Table;

@Table("cardholders")
public class Cardholder {
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
    private String cardholderPhotoURL;

    @Column("cardholder_photo_key")
    private String cardholderPhotoKey;

    public Cardholder() {
    }

    public Cardholder(Long id, Long travelcardId, String cardholderTitle, String cardholderForename, String cardholderSurname, CardholderType cardholderType, String cardholderPhotoName, String cardholderPhotoRrsKey, String cardholderPhotoURL, String cardholderPhotoKey) {
        this.id = id;
        this.travelcardId = travelcardId;
        this.cardholderTitle = cardholderTitle;
        this.cardholderForename = cardholderForename;
        this.cardholderSurname = cardholderSurname;
        this.cardholderType = cardholderType;
        this.cardholderPhotoName = cardholderPhotoName;
        this.cardholderPhotoRrsKey = cardholderPhotoRrsKey;
        this.cardholderPhotoURL = cardholderPhotoURL;
        this.cardholderPhotoKey = cardholderPhotoKey;
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

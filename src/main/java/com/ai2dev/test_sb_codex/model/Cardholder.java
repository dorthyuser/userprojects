package com.ai2dev.test_sb_codex.model;

import org.springframework.data.annotation.Id;
import org.springframework.data.relational.core.mapping.Column;

public class Cardholder {
    @Id
    private Long id;

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
    private String cardholderPhotoRRSKey;

    @Column("cardholder_photo_url")
    private String cardholderPhotoURL;

    @Column("cardholder_photo_key")
    private String cardholderPhotoKey;

    public Cardholder() {
    }

    public Long getId() {
        return id;
    }

    public void setId(Long id) {
        this.id = id;
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

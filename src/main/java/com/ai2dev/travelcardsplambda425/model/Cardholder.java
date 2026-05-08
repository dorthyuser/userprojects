package com.ai2dev.travelcardsplambda425.model;

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
    private String cardholderPhotoRRSKey;

    @Column("cardholder_photo_url")
    private String cardholderPhotoURL;

    @Column("cardholder_photo_key")
    private String cardholderPhotoKey;

    public Cardholder(Long id, Long travelcardId, String cardholderTitle, String cardholderForename, String cardholderSurname, CardholderType cardholderType, String cardholderPhotoName, String cardholderPhotoRRSKey, String cardholderPhotoURL, String cardholderPhotoKey) {
        this.id = id;
        this.travelcardId = travelcardId;
        this.cardholderTitle = cardholderTitle;
        this.cardholderForename = cardholderForename;
        this.cardholderSurname = cardholderSurname;
        this.cardholderType = cardholderType;
        this.cardholderPhotoName = cardholderPhotoName;
        this.cardholderPhotoRRSKey = cardholderPhotoRRSKey;
        this.cardholderPhotoURL = cardholderPhotoURL;
        this.cardholderPhotoKey = cardholderPhotoKey;
    }

    public Long getId() {
        return id;
    }

    public Long getTravelcardId() {
        return travelcardId;
    }

    public String getCardholderTitle() {
        return cardholderTitle;
    }

    public String getCardholderForename() {
        return cardholderForename;
    }

    public String getCardholderSurname() {
        return cardholderSurname;
    }

    public CardholderType getCardholderType() {
        return cardholderType;
    }

    public String getCardholderPhotoName() {
        return cardholderPhotoName;
    }

    public String getCardholderPhotoRRSKey() {
        return cardholderPhotoRRSKey;
    }

    public String getCardholderPhotoURL() {
        return cardholderPhotoURL;
    }

    public String getCardholderPhotoKey() {
        return cardholderPhotoKey;
    }
}
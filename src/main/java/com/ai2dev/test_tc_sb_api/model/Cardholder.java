package com.ai2dev.test_tc_sb_api.model;

import org.springframework.data.annotation.Id;
import org.springframework.data.relational.core.mapping.Column;
import org.springframework.data.relational.core.mapping.Table;

@Table("cardholders")
public class Cardholder {
    @Id
    private Integer id;

    @Column("travelcard_id")
    private Integer travelcardId;

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
    }

    public Cardholder(Integer id, Integer travelcardId, String cardholderTitle, String cardholderForename, String cardholderSurname, CardholderType cardholderType, String cardholderPhotoName, String cardholderPhotoRrsKey, String cardholderPhotoUrl, String cardholderPhotoKey) {
        this.id = id;
        this.travelcardId = travelcardId;
        this.cardholderTitle = cardholderTitle;
        this.cardholderForename = cardholderForename;
        this.cardholderSurname = cardholderSurname;
        this.cardholderType = cardholderType;
        this.cardholderPhotoName = cardholderPhotoName;
        this.cardholderPhotoRrsKey = cardholderPhotoRrsKey;
        this.cardholderPhotoUrl = cardholderPhotoUrl;
        this.cardholderPhotoKey = cardholderPhotoKey;
    }

    public Integer getId() {
        return id;
    }

    public void setId(Integer id) {
        this.id = id;
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

package com.ai2dev.test_sb_java_travelcard.dto;

import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;
import jakarta.validation.constraints.Size;

public class CardholderDto {

    @NotBlank
    @Size(min = 1, max = 15)
    private String cardholderTitle;

    @NotBlank
    @Size(min = 1, max = 100)
    private String cardholderForename;

    @NotBlank
    @Size(min = 1, max = 100)
    private String cardholderSurname;

    @NotBlank
    private String cardholderType;

    @NotBlank
    @Size(min = 1, max = 100)
    private String cardholderPhotoName;

    private String cardholderPhotoRrsKey;
    private String cardholderPhotoUrl;
    private String cardholderPhotoKey;

    public CardholderDto() {
    }

    public String cardholderTitle() {
        return cardholderTitle;
    }

    public void setCardholderTitle(String cardholderTitle) {
        this.cardholderTitle = cardholderTitle;
    }

    public String cardholderForename() {
        return cardholderForename;
    }

    public void setCardholderForename(String cardholderForename) {
        this.cardholderForename = cardholderForename;
    }

    public String cardholderSurname() {
        return cardholderSurname;
    }

    public void setCardholderSurname(String cardholderSurname) {
        this.cardholderSurname = cardholderSurname;
    }

    public String cardholderType() {
        return cardholderType;
    }

    public void setCardholderType(String cardholderType) {
        this.cardholderType = cardholderType;
    }

    public String cardholderPhotoName() {
        return cardholderPhotoName;
    }

    public void setCardholderPhotoName(String cardholderPhotoName) {
        this.cardholderPhotoName = cardholderPhotoName;
    }

    public String cardholderPhotoRrsKey() {
        return cardholderPhotoRrsKey;
    }

    public void setCardholderPhotoRrsKey(String cardholderPhotoRrsKey) {
        this.cardholderPhotoRrsKey = cardholderPhotoRrsKey;
    }

    public String cardholderPhotoUrl() {
        return cardholderPhotoUrl;
    }

    public void setCardholderPhotoUrl(String cardholderPhotoUrl) {
        this.cardholderPhotoUrl = cardholderPhotoUrl;
    }

    public String cardholderPhotoKey() {
        return cardholderPhotoKey;
    }

    public void setCardholderPhotoKey(String cardholderPhotoKey) {
        this.cardholderPhotoKey = cardholderPhotoKey;
    }
}

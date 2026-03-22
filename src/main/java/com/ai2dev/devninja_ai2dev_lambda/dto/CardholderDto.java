package com.ai2dev.devninja_ai2dev_lambda.dto;

import com.ai2dev.devninja_ai2dev_lambda.model.CardholderType;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;
import jakarta.validation.constraints.Size;

public class CardholderDto
{
    @NotBlank
    @Size(min = 1, max = 15)
    private String cardholderTitle;

    @NotBlank
    @Size(min = 1, max = 100)
    private String cardholderForename;

    @NotBlank
    @Size(min = 1, max = 100)
    private String cardholderSurname;

    @NotNull
    private CardholderType cardholderType;

    @NotBlank
    @Size(min = 1, max = 100)
    private String cardholderPhotoName;

    @Size(min = 39, max = 42)
    private String cardholderPhotoRRSKey;

    @Size(min = 20, max = 2048)
    private String cardholderPhotoURL;

    @Size(min = 39, max = 42)
    private String cardholderPhotoKey;

    public String getCardholderTitle()
    {
        return cardholderTitle;
    }

    public void setCardholderTitle(String cardholderTitle)
    {
        this.cardholderTitle = cardholderTitle;
    }

    public String getCardholderForename()
    {
        return cardholderForename;
    }

    public void setCardholderForename(String cardholderForename)
    {
        this.cardholderForename = cardholderForename;
    }

    public String getCardholderSurname()
    {
        return cardholderSurname;
    }

    public void setCardholderSurname(String cardholderSurname)
    {
        this.cardholderSurname = cardholderSurname;
    }

    public CardholderType getCardholderType()
    {
        return cardholderType;
    }

    public void setCardholderType(CardholderType cardholderType)
    {
        this.cardholderType = cardholderType;
    }

    public String getCardholderPhotoName()
    {
        return cardholderPhotoName;
    }

    public void setCardholderPhotoName(String cardholderPhotoName)
    {
        this.cardholderPhotoName = cardholderPhotoName;
    }

    public String getCardholderPhotoRRSKey()
    {
        return cardholderPhotoRRSKey;
    }

    public void setCardholderPhotoRRSKey(String cardholderPhotoRRSKey)
    {
        this.cardholderPhotoRRSKey = cardholderPhotoRRSKey;
    }

    public String getCardholderPhotoURL()
    {
        return cardholderPhotoURL;
    }

    public void setCardholderPhotoURL(String cardholderPhotoURL)
    {
        this.cardholderPhotoURL = cardholderPhotoURL;
    }

    public String getCardholderPhotoKey()
    {
        return cardholderPhotoKey;
    }

    public void setCardholderPhotoKey(String cardholderPhotoKey)
    {
        this.cardholderPhotoKey = cardholderPhotoKey;
    }
}

package com.ai2dev.devninja_ai2dev_lambda.dto;

public class CreateTravelcardResponse
{
    private String travelcardId;

    private String token;

    public CreateTravelcardResponse()
    {
    }

    public CreateTravelcardResponse(String travelcardId, String token)
    {
        this.travelcardId = travelcardId;
        this.token = token;
    }

    public String getTravelcardId()
    {
        return travelcardId;
    }

    public void setTravelcardId(String travelcardId)
    {
        this.travelcardId = travelcardId;
    }

    public String getToken()
    {
        return token;
    }

    public void setToken(String token)
    {
        this.token = token;
    }
}

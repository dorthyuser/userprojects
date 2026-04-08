package com.ai2dev.test_tc_sb_api.dto;

public class TravelcardResponse {
    private String travelcardId;
    private String token;

    public TravelcardResponse() {
    }

    public TravelcardResponse(String travelcardId, String token) {
        this.travelcardId = travelcardId;
        this.token = token;
    }

    public String getTravelcardId() {
        return travelcardId;
    }

    public void setTravelcardId(String travelcardId) {
        this.travelcardId = travelcardId;
    }

    public String getToken() {
        return token;
    }

    public void setToken(String token) {
        this.token = token;
    }
}

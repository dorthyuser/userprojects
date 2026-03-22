package com.ai2dev.devninja_ai2dev_lambda.dto;

import io.micronaut.core.annotation.Introspected;

@Introspected
public class TravelcardResponse {
    private static final org.slf4j.Logger LOG = org.slf4j.LoggerFactory.getLogger(TravelcardResponse.class);

    private String travelcardId;

    private String token;

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

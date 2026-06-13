package com.ai2dev.syncspringboot109.model.dto;

import com.fasterxml.jackson.annotation.JsonProperty;

public record ZohoTokenResponseDto(@JsonProperty("access_token") String access_token, @JsonProperty("expires_in") Integer expires_in, @JsonProperty("api_domain") String api_domain)
{
}

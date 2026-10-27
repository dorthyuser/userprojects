package com.ai2dev.bustravelaccounts.model.error;

import com.fasterxml.jackson.annotation.JsonInclude;
import com.fasterxml.jackson.annotation.JsonProperty;

public record ErrorResponse(@JsonProperty("error") ErrorBody error) {
    @JsonInclude(JsonInclude.Include.NON_NULL)
    public record ErrorBody(
            @JsonProperty("errorCode") int errorCode,
            @JsonProperty("errorDateTime") String errorDateTime,
            @JsonProperty("errorMessage") String errorMessage,
            @JsonProperty("errorDescription") String errorDescription) {
    }
}

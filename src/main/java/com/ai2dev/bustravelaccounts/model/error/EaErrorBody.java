package com.ai2dev.bustravelaccounts.model.error;

import com.fasterxml.jackson.annotation.JsonInclude;

@JsonInclude(JsonInclude.Include.NON_NULL)
public record EaErrorBody(EaError error) {
    @JsonInclude(JsonInclude.Include.NON_NULL)
    public record EaError(int errorCode, String errorDateTime, String errorMessage, String errorDescription) {
    }
}

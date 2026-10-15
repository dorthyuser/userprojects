package com.ai2dev.bustravelaccounts.model.error;

import com.fasterxml.jackson.annotation.JsonInclude;

@JsonInclude(JsonInclude.Include.NON_NULL)
public record SaErrorBody(SaError error) {
    @JsonInclude(JsonInclude.Include.NON_NULL)
    public record SaError(int errorCode, String errorDateTime, String errorMessage, String errorDescription) {
    }
}

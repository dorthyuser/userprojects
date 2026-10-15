package com.ai2dev.bustravelaccounts.model.error;

import com.fasterxml.jackson.annotation.JsonInclude;

@JsonInclude(JsonInclude.Include.NON_NULL)
public record PaErrorBody(PaError error) {
    @JsonInclude(JsonInclude.Include.NON_NULL)
    public record PaError(int errorCode, String errorDateTime, String errorMessage, String errorDescription) {
    }
}

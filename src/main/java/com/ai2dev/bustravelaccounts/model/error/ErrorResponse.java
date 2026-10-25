package com.ai2dev.bustravelaccounts.model.error;

import com.fasterxml.jackson.annotation.JsonInclude;

@JsonInclude(JsonInclude.Include.NON_NULL)
public record ErrorResponse(Error error) {
    @JsonInclude(JsonInclude.Include.NON_NULL)
    public record Error(Integer errorCode, String errorDateTime, String errorMessage, String errorDescription) {}
}

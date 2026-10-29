package com.ai2dev.bustravelaccounts.dto.error;

import com.fasterxml.jackson.annotation.JsonInclude;

@JsonInclude(JsonInclude.Include.NON_NULL)
public record ErrorResponse(ErrorBody error) {
    @JsonInclude(JsonInclude.Include.NON_NULL)
    public record ErrorBody(int errorCode, String errorDateTime, String errorMessage, String errorDescription) {}
}

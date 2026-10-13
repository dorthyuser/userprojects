package com.ai2dev.bustravelaccountssa.model.error;

import java.time.OffsetDateTime;

import com.fasterxml.jackson.annotation.JsonInclude;

@JsonInclude(JsonInclude.Include.NON_NULL)
public record ErrorResponse(ErrorBody error) {
    public record ErrorBody(int errorCode, OffsetDateTime errorDateTime, String errorMessage, String errorDescription) {}
}

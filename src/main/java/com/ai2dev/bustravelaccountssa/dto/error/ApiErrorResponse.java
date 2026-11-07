package com.ai2dev.bustravelaccountssa.dto.error;

import com.fasterxml.jackson.annotation.JsonInclude;

@JsonInclude(JsonInclude.Include.NON_NULL)
public record ApiErrorResponse(ErrorBody error) {
    @JsonInclude(JsonInclude.Include.NON_NULL)
    public record ErrorBody(Integer errorCode, String errorDateTime, String errorMessage, String errorDescription) {}
}

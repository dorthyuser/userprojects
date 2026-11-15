package com.ai2dev.bustravelaccountssa.exception;

import com.ai2dev.bustravelaccountssa.dto.error.ApiErrorResponse;

public class SalesforceOperationException extends RuntimeException {
    private final ApiErrorResponse errorResponse;

    public SalesforceOperationException(String message, ApiErrorResponse errorResponse) {
        super(message);
        this.errorResponse = errorResponse;
    }

    public ApiErrorResponse errorResponse() {
        return errorResponse;
    }
}

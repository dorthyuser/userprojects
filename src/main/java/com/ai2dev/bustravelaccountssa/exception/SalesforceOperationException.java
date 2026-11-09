package com.ai2dev.bustravelaccountssa.exception;

import com.ai2dev.bustravelaccountssa.dto.error.ApiErrorResponse;
import org.springframework.http.HttpStatus;

public class SalesforceOperationException extends RuntimeException {
    private final ApiErrorResponse errorResponse;
    private final HttpStatus status;

    public SalesforceOperationException(String message, ApiErrorResponse errorResponse, HttpStatus status) {
        super(message);
        this.errorResponse = errorResponse;
        this.status = status;
    }

    public ApiErrorResponse errorResponse() {
        return errorResponse;
    }

    public HttpStatus status() {
        return status;
    }
}

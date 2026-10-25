package com.ai2dev.bustravelaccounts.exception;

public class DatabaseOperationException extends RuntimeException {
    private final String errorMessage;
    private final String errorDescription;

    public DatabaseOperationException(String errorMessage, String errorDescription, Throwable cause) {
        super(errorDescription, cause);
        this.errorMessage = errorMessage;
        this.errorDescription = errorDescription;
    }

    public String errorMessage() {
        return errorMessage;
    }

    public String errorDescription() {
        return errorDescription;
    }
}

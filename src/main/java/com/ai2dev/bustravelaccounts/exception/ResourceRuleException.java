package com.ai2dev.bustravelaccounts.exception;

public abstract class ResourceRuleException extends RuntimeException {
    private final int status;
    private final String errorMessage;

    protected ResourceRuleException(int status, String errorMessage, String message) {
        super(message);
        this.status = status;
        this.errorMessage = errorMessage;
    }

    public int status() {
        return status;
    }

    public String errorMessage() {
        return errorMessage;
    }
}

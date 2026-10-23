package com.ai2dev.bustravelaccounts.exception;

public class BusinessRuleException extends RuntimeException {
    private final int status;

    public BusinessRuleException(int status, String message) {
        super(message);
        this.status = status;
    }

    public int status() {
        return status;
    }
}

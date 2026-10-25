package com.ai2dev.bustravelaccounts.exception;

public class ConflictException extends ResourceRuleException {
    public ConflictException(String message) {
        super(409, "CONFLICT", message);
    }
}

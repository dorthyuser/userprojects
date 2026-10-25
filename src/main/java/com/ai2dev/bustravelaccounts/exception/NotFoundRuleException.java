package com.ai2dev.bustravelaccounts.exception;

public class NotFoundRuleException extends ResourceRuleException {
    public NotFoundRuleException(String message) {
        super(404, "RESOURCE NOT FOUND", message);
    }
}

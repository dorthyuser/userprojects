package com.ai2dev.bustravelaccounts.exception;

public class HotlistedAccountException extends ResourceRuleException {
    public HotlistedAccountException(String message) {
        super(422, "UNPROCESSABLE ENTITY", message);
    }
}

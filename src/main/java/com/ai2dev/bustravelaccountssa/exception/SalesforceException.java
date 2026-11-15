package com.ai2dev.bustravelaccountssa.exception;

public class SalesforceException extends RuntimeException {
    private final String namespace;
    private final String identifier;

    public SalesforceException(String namespace, String identifier, String description) {
        super(description);
        this.namespace = namespace;
        this.identifier = identifier;
    }

    public String namespace() {
        return namespace;
    }

    public String identifier() {
        return identifier;
    }
}

package com.ai2dev.bustravelaccountssa.exception;

public class SalesforceException extends RuntimeException {
    private final String errorType;
    private final String errorCode;

    public SalesforceException(String errorType, String errorCode, String message) {
        super(message);
        this.errorType = errorType;
        this.errorCode = errorCode;
    }

    public String errorType() { return errorType; }
    public String errorCode() { return errorCode; }

    public static SalesforceException of(org.springframework.web.client.RestClientResponseException e) {
        return new SalesforceException("SALESFORCE:CONNECTIVITY", "UNREACHABLE", "Salesforce could not be reached");
    }
}

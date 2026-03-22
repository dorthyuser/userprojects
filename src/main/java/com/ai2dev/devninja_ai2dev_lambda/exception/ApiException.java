package com.ai2dev.devninja_ai2dev_lambda.exception;

public class ApiException extends RuntimeException {
    private static final org.slf4j.Logger LOG = org.slf4j.LoggerFactory.getLogger(ApiException.class);

    private final int statusCode;

    public ApiException(int statusCode, String message) {
        super(message);
        LOG.info("{\"event\":\"entry\"}");
        this.statusCode = statusCode;
        LOG.info("{\"event\":\"exit\",\"id\":\"{}\"}", statusCode);
    }

    public int getStatusCode() {
        return statusCode;
    }
}

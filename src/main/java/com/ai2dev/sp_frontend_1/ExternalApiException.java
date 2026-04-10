package com.ai2dev.sp_frontend_1;

import org.springframework.http.HttpStatus;

/**
 * Exception representing an error response from the external API.
 */
public class ExternalApiException extends RuntimeException {
    private final HttpStatus status;
    private final String body;

    public ExternalApiException(HttpStatus status, String body) {
        super("External API error: " + status);
        this.status = status;
        this.body = body;
    }

    public ExternalApiException(HttpStatus status, String body, Throwable cause) {
        super("External API error: " + status, cause);
        this.status = status;
        this.body = body;
    }

    public HttpStatus getStatus() {
        return status;
    }

    public String getBody() {
        return body;
    }
}

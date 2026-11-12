package com.ai2dev.bustravelaccountsea.exception;

import org.springframework.http.HttpStatus;

public class MuleBusinessException extends RuntimeException {
    private final HttpStatus status;
    private final String message;

    public MuleBusinessException(String code, String message, Throwable cause) {
        super(message, cause);
        this.status = HttpStatus.INTERNAL_SERVER_ERROR;
        this.message = code;
    }

    public MuleBusinessException(HttpStatus status, String message) {
        super(message);
        this.status = status;
        this.message = message;
    }

    public HttpStatus status() {
        return status;
    }

    @Override
    public String getMessage() {
        return message;
    }

    public String message() {
        return message;
    }
}

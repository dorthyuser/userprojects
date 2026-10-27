package com.ai2dev.bustravelaccounts.exception;

public class BadRequestException extends RuntimeException {
    public BadRequestException(String message, Throwable cause) {
        super(message, cause);
    }
}

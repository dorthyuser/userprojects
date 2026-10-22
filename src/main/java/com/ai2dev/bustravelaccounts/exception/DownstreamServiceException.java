package com.ai2dev.bustravelaccounts.exception;

public class DownstreamServiceException extends RuntimeException {
    private final int status;

    public DownstreamServiceException(int status, String message) {
        super(message);
        this.status = status;
    }

    public int status() { return status; }
}

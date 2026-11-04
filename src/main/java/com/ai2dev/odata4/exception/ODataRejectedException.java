package com.ai2dev.odata4.exception;

public class ODataRejectedException extends RuntimeException {
    private final int status;

    public ODataRejectedException(int status, String message) {
        super(message);
        this.status = status;
    }

    public int status() {
        return status;
    }
}

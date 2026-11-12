package com.ai2dev.bustravelaccountsea.dto.error;

public record ErrorResponse(Error error) {
    public record Error(int errorCode, String errorMessage) {}
}

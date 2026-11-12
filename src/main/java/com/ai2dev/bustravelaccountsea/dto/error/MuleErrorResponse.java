package com.ai2dev.bustravelaccountsea.dto.error;

public record MuleErrorResponse(Error error) {
    public record Error(int errorCode, String errorMessage, String errorDescription) {}
}

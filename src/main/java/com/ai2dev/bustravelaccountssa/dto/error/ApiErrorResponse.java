package com.ai2dev.bustravelaccountssa.dto.error;

import java.time.OffsetDateTime;

public record ApiErrorResponse(ApiError error) {
    public static ApiErrorResponse of(int errorCode, String errorMessage, String errorDescription) {
        return new ApiErrorResponse(new ApiError(errorCode, OffsetDateTime.now(), errorMessage, errorDescription));
    }
}

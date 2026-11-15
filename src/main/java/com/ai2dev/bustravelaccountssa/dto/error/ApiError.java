package com.ai2dev.bustravelaccountssa.dto.error;

import java.time.OffsetDateTime;

public record ApiError(
    int errorCode,
    OffsetDateTime errorDateTime,
    String errorMessage,
    String errorDescription
) {
}

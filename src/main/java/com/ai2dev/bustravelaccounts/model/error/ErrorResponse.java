package com.ai2dev.bustravelaccounts.model.error;

import java.time.OffsetDateTime;

public record ErrorResponse(
    int errorCode,
    OffsetDateTime errorDateTime,
    String errorMessage,
    String errorDescription
) {}

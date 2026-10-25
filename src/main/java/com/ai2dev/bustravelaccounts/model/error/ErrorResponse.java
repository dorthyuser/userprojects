package com.ai2dev.bustravelaccounts.model.error;

import com.fasterxml.jackson.annotation.JsonProperty;
import java.time.OffsetDateTime;

public record ErrorResponse(
    @JsonProperty("error") ErrorDetails error
) {
    public ErrorResponse(int errorCode, OffsetDateTime errorDateTime, String errorMessage, String errorDescription) {
        this(new ErrorDetails(errorCode, errorDateTime, errorMessage, errorDescription));
    }

    public record ErrorDetails(
        @JsonProperty("errorCode") int errorCode,
        @JsonProperty("errorDateTime") OffsetDateTime errorDateTime,
        @JsonProperty("errorMessage") String errorMessage,
        @JsonProperty("errorDescription") String errorDescription
    ) {}
}

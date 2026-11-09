package com.ai2dev.bustravelaccountssa.dto.error;

import com.fasterxml.jackson.annotation.JsonProperty;
import java.time.OffsetDateTime;

public record ApiErrorDetail(
    @JsonProperty("errorCode") int errorCode,
    @JsonProperty("errorDateTime") OffsetDateTime errorDateTime,
    @JsonProperty("errorMessage") String errorMessage,
    @JsonProperty("errorDescription") String errorDescription
) {
}

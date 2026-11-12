package com.ai2dev.bustravelaccountsea.dto.error;

import com.fasterxml.jackson.annotation.JsonProperty;
import java.time.OffsetDateTime;

public record ErrorResponse(ErrorBody error) {
    public record ErrorBody(@JsonProperty("errorCode") int errorCode,
                            @JsonProperty("errorDateTime") OffsetDateTime errorDateTime,
                            @JsonProperty("errorMessage") String errorMessage,
                            @JsonProperty("errorDescription") String errorDescription) {
    }
}

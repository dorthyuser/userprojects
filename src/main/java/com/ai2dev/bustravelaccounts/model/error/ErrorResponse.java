package com.ai2dev.bustravelaccounts.model.error;

import com.fasterxml.jackson.annotation.JsonInclude;
import com.fasterxml.jackson.annotation.JsonProperty;
import java.time.OffsetDateTime;

@JsonInclude(JsonInclude.Include.NON_NULL)
public record ErrorResponse(@JsonProperty("errorCode") int errorCode,
                            @JsonProperty("errorDateTime") OffsetDateTime errorDateTime,
                            @JsonProperty("errorMessage") String errorMessage,
                            @JsonProperty("errorDescription") String errorDescription) {
}

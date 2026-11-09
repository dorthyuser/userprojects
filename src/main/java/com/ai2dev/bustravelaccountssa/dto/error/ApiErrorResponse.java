package com.ai2dev.bustravelaccountssa.dto.error;

import com.fasterxml.jackson.annotation.JsonProperty;
import java.time.OffsetDateTime;

public record ApiErrorResponse(@JsonProperty("error") ApiErrorDetail error) {
    public static ApiErrorResponse of(int status, String message, String description) {
        return new ApiErrorResponse(new ApiErrorDetail(status, OffsetDateTime.now(), message, description));
    }
}

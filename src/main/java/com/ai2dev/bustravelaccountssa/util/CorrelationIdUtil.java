package com.ai2dev.bustravelaccountssa.util;

import java.util.UUID;
import org.springframework.http.HttpHeaders;

public final class CorrelationIdUtil {
    private CorrelationIdUtil() {
    }

    public static String resolve(HttpHeaders headers) {
        String correlationId = headers.getFirst("X-Correlation-ID");
        if (correlationId == null || correlationId.isBlank()) {
            correlationId = headers.getFirst("X_CORRELATION_ID");
        }
        if (correlationId == null || correlationId.isBlank()) {
            return UUID.randomUUID().toString();
        }
        return correlationId.trim();
    }
}

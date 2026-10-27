package com.ai2dev.bustravelaccounts.util;

import java.util.UUID;
import org.springframework.http.HttpHeaders;

public final class HeaderUtils {
    private HeaderUtils() {
    }

    public static String correlationId(HttpHeaders headers) {
        String value = headers.getFirst("X-Correlation-ID");
        if (value == null || value.isBlank()) {
            value = headers.getFirst("X_CORRELATION_ID");
        }
        return value == null || value.isBlank() ? UUID.randomUUID().toString() : value.trim();
    }

    public static String customerCorrelationId(HttpHeaders headers) {
        String value = headers.getFirst("CUSTOMER_CORRELATION_ID");
        return value == null ? "CUSTOMER_CORRELATION_ID_NOT_FOUND" : value.trim();
    }
}

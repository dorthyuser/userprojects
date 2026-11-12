package com.ai2dev.bustravelaccountsea.util;

import java.util.UUID;
import org.springframework.http.HttpHeaders;

public final class CorrelationIdUtil {
    private CorrelationIdUtil() {
    }

    public static String correlationId(HttpHeaders headers) {
        String value = headers.getFirst("X-Correlation-ID");
        if (value == null || value.isBlank()) {
            return UUID.randomUUID().toString();
        }
        return value.trim();
    }
}

package com.ai2dev.bustravelaccounts.util;

import java.util.UUID;
import org.springframework.http.HttpHeaders;

public final class CorrelationIdUtil {
    private CorrelationIdUtil() {}

    public static String correlationId(HttpHeaders headers) {
        String value = headers.getFirst("X-Correlation-ID");
        return value == null || value.isBlank() ? UUID.randomUUID().toString() : value.trim();
    }

    public static String header(HttpHeaders headers, String name) {
        String value = headers.getFirst(name);
        return value == null ? null : value.trim();
    }
}

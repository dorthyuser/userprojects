package com.ai2dev.bustravelaccounts.util;

import java.util.Map;
import java.util.UUID;
import org.springframework.http.HttpHeaders;

public final class CorrelationUtil {
    private CorrelationUtil() {
    }

    public static String header(Map<String, String> headers, String name) {
        for (Map.Entry<String, String> entry : headers.entrySet()) {
            if (entry.getKey() != null && entry.getKey().equalsIgnoreCase(name)) {
                return entry.getValue();
            }
        }
        return null;
    }

    public static String header(HttpHeaders headers, String name) {
        return headers.getFirst(name);
    }

    public static String correlationId(HttpHeaders headers) {
        String value = headers.getFirst("X-Correlation-ID");
        return value == null || value.isBlank() ? UUID.randomUUID().toString() : value.trim();
    }
}

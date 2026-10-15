package com.ai2dev.bustravelaccounts.util;

import org.springframework.http.HttpHeaders;

import java.util.Map;
import java.util.UUID;

public final class HttpUtil {
    private HttpUtil() {
    }

    public static String headerIgnoreCase(HttpHeaders headers, String name) {
        if (headers == null) {
            return null;
        }
        return headers.getFirst(name);
    }

    public static String headerIgnoreCase(Map<String, String> headers, String name) {
        if (headers == null) {
            return null;
        }
        for (Map.Entry<String, String> entry : headers.entrySet()) {
            if (entry.getKey() != null && entry.getKey().equalsIgnoreCase(name)) {
                return entry.getValue();
            }
        }
        return null;
    }

    public static String correlationId(HttpHeaders headers) {
        String value = headerIgnoreCase(headers, "X-Correlation-ID");
        return value == null || value.isBlank() ? UUID.randomUUID().toString() : value.trim();
    }

    public static String correlationId(Map<String, String> headers) {
        String value = headerIgnoreCase(headers, "X-Correlation-ID");
        return value == null || value.isBlank() ? UUID.randomUUID().toString() : value.trim();
    }
}

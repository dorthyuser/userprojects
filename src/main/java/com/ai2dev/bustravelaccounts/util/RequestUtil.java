package com.ai2dev.bustravelaccounts.util;

import java.util.UUID;
import org.springframework.http.HttpHeaders;

public final class RequestUtil {
    private RequestUtil() {}

    public static String header(HttpHeaders headers, String name) {
        return headers.getFirst(name);
    }

    public static String correlationId(HttpHeaders headers) {
        String value = headers.getFirst("X-Correlation-ID");
        return value == null || value.isBlank() ? UUID.randomUUID().toString() : value.trim();
    }

    public static String customerCorrelationId(HttpHeaders headers) {
        String value = headers.getFirst("CUSTOMER_CORRELATION_ID");
        return value == null || value.isBlank() ? "CUSTOMER_CORRELATION_ID_NOT_FOUND" : value.trim();
    }
}

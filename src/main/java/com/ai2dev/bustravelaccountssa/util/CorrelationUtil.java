package com.ai2dev.bustravelaccountssa.util;

import java.util.UUID;

import org.springframework.http.HttpHeaders;

public final class CorrelationUtil {
    private CorrelationUtil() {}

    public static String correlationId(HttpHeaders headers) {
        String value = headers.getFirst("X-Correlation-ID");
        return value == null || value.isBlank() ? UUID.randomUUID().toString() : value;
    }
}

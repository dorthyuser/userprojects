package com.ai2dev.bustravelaccountssa.util;

import java.util.UUID;
import org.springframework.http.HttpHeaders;

public final class CorrelationIdUtil {
    private CorrelationIdUtil() {
    }

    public static String correlationId(HttpHeaders headers) {
        String incoming = headers.getFirst("X-Correlation-ID");
        if (incoming == null || incoming.trim().isEmpty()) {
            incoming = headers.getFirst("X_CORRELATION_ID");
        }
        if (incoming == null || incoming.trim().isEmpty()) {
            return UUID.randomUUID().toString();
        }
        return incoming.trim();
    }
}

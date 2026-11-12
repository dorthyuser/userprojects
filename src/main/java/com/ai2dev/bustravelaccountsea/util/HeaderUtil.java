package com.ai2dev.bustravelaccountsea.util;

import org.springframework.http.HttpHeaders;

public final class HeaderUtil {
    private HeaderUtil() {
    }

    public static String header(HttpHeaders headers, String name) {
        String value = headers.getFirst(name);
        return value == null ? null : value.trim();
    }
}

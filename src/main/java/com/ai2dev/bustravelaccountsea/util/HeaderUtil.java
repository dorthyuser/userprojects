package com.ai2dev.bustravelaccountsea.util;

public final class HeaderUtil {
    private HeaderUtil() {}

    public static String trimOrDefault(String value, String defaultValue) {
        if (value == null || value.isBlank()) {
            return defaultValue;
        }
        return value.trim();
    }

    public static String trimOrNull(String value) {
        if (value == null || value.isBlank()) {
            return null;
        }
        return value.trim();
    }

    public static String extractQueryParam(String queryString, String name) {
        if (queryString == null || queryString.isBlank()) {
            return null;
        }
        String prefix = name + "=";
        for (String part : queryString.split("&")) {
            if (part.startsWith(prefix)) {
                return part.substring(prefix.length()).trim();
            }
        }
        return null;
    }
}

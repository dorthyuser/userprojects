package com.ai2dev.bustravelaccounts.util;

import java.util.Map;

public final class DatabaseSettingsUtil {
    private DatabaseSettingsUtil() {
    }

    public static String value(Map<String, String> values, String key, String fallback) {
        String secret = values.get(key);
        if (secret != null && !secret.isBlank()) {
            return secret;
        }
        return fallback;
    }
}

package com.ai2dev.bustravelaccounts.util;

import java.util.Map;

public final class DbSettingsUtil {
    private DbSettingsUtil() {}

    public static String value(Map<String, String> secret, String key, String fallback) {
        if (secret != null && secret.containsKey(key) && secret.get(key) != null && !secret.get(key).isBlank()) {
            return secret.get(key);
        }
        return fallback;
    }
}

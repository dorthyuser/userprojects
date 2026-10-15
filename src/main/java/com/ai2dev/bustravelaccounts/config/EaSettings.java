package com.ai2dev.bustravelaccounts.config;

import org.springframework.core.env.Environment;
import org.springframework.stereotype.Component;

@Component
public class EaSettings {
    private final String basePath;

    public EaSettings(Environment environment) {
        String value = environment.getProperty("HTTPS_LISTENER_PATH");
        if (value == null || value.isBlank()) {
            value = "api/v1/*";
        }
        value = value.trim();
        if (value.endsWith("/*")) {
            value = value.substring(0, value.length() - 2);
        }
        if (value.endsWith("/")) {
            value = value.substring(0, value.length() - 1);
        }
        this.basePath = value;
    }

    public String basePath() {
        return basePath;
    }
}

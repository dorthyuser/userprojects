package com.ai2dev.odata4.config;

import org.springframework.core.env.Environment;
import org.springframework.stereotype.Component;

@Component
public class Settings {
    private final Environment environment;

    public Settings(Environment environment) {
        this.environment = environment;
    }

    public String basePath() {
        String value = environment.getProperty("API_BASE_PATH", "/api");
        if (value == null || value.isBlank()) {
            return "/api";
        }
        String normalized = value.trim();
        if (normalized.endsWith("/*")) {
            normalized = normalized.substring(0, normalized.length() - 2);
        }
        if (normalized.endsWith("/")) {
            normalized = normalized.substring(0, normalized.length() - 1);
        }
        return normalized;
    }

    public String dbHost() {
        return environment.getProperty("DS_DB_HOST");
    }

    public String dbPort() {
        return environment.getProperty("DS_DB_PORT");
    }

    public String dbUsername() {
        return environment.getProperty("DS_DB_USER");
    }

    public String dbPassword() {
        return environment.getProperty("DS_DB_PASSWORD");
    }

    public String dbName() {
        return environment.getProperty("DS_DB_DATABASE");
    }

    public String ordersPageSize() {
        return environment.getProperty("SERVICE_ORDERS_PAGE_SIZE");
    }
}

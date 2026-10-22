package com.ai2dev.bustravelaccounts.config;

import org.springframework.core.env.Environment;
import org.springframework.stereotype.Component;

@Component
public class Settings {
    private final Environment environment;

    public Settings(Environment environment) {
        this.environment = environment;
    }

    public String basePath() {
        String path = environment.getProperty("HTTPS_LISTENER_PATH", "api/v1/*");
        if (path == null) {
            return "";
        }
        String normalized = path.trim();
        if (normalized.endsWith("/*")) {
            normalized = normalized.substring(0, normalized.length() - 2);
        }
        if (normalized.endsWith("/")) {
            normalized = normalized.substring(0, normalized.length() - 1);
        }
        return normalized;
    }

    public String dbUsername() { return environment.getProperty("DB_USERNAME"); }
    public String dbPassword() { return environment.getProperty("DB_PASSWORD"); }
    public String dbHost() { return environment.getProperty("DB_HOST"); }
    public String dbName() { return environment.getProperty("DB_NAME"); }
    public String dbPort() { return environment.getProperty("DB_PORT"); }
}

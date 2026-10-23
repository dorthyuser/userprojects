package com.ai2dev.bustravelaccounts.config;

import org.springframework.core.env.Environment;
import org.springframework.stereotype.Component;

@Component
public class Settings {
    private final Environment environment;
    private final SecretsLoader secretsLoader;

    public Settings(Environment environment, SecretsLoader secretsLoader) {
        this.environment = environment;
        this.secretsLoader = secretsLoader;
    }

    public String basePath() {
        String value = environment.getProperty("HTTPS_LISTENER_PATH");
        if (value == null || value.isBlank()) {
            return "api/v1";
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

    public String dbUsername() { return value("username", "DB_USERNAME"); }
    public String dbPassword() { return value("password", "DB_PASSWORD"); }
    public String dbHost() { return value("host", "DB_HOST"); }
    public String dbName() { return value("dbname", "DB_NAME"); }
    public String dbPort() { return value("port", "DB_PORT"); }

    private String value(String secretKey, String envVar) {
        String secret = secretsLoader.values().get(secretKey);
        if (secret != null && !secret.isBlank()) {
            return secret;
        }
        return environment.getProperty(envVar);
    }
}

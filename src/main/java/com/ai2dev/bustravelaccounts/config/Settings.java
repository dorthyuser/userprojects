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
        String configured = environment.getProperty("HTTPS_LISTENER_PATH");
        String resolved = configured == null || configured.isBlank() ? "api/v1/*" : configured;
        if (resolved.endsWith("/*")) {
            resolved = resolved.substring(0, resolved.length() - 2);
        }
        if (resolved.startsWith("/")) {
            resolved = resolved.substring(1);
        }
        if (resolved.endsWith("/")) {
            resolved = resolved.substring(0, resolved.length() - 1);
        }
        return resolved;
    }

    public String amazonS3Bucket() {
        return environment.getProperty("AMAZON_S3_BUCKET");
    }

    public String dbUsername() {
        return environment.getProperty("DB_USERNAME");
    }

    public String dbPassword() {
        return environment.getProperty("DB_PASSWORD");
    }

    public String dbHost() {
        return environment.getProperty("DB_HOST");
    }

    public String dbName() {
        return environment.getProperty("DB_NAME");
    }

    public String dbPort() {
        return environment.getProperty("DB_PORT");
    }
}

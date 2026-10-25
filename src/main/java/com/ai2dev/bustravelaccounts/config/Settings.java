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
        String value = configured == null || configured.isBlank() ? "api/v1/*" : configured;
        value = value.trim();
        if (value.endsWith("/*")) {
            value = value.substring(0, value.length() - 2);
        }
        if (value.startsWith("/")) {
            value = value.substring(1);
        }
        return value;
    }

    public String dbHost() {
        return environment.getProperty("DB_HOST");
    }

    public String dbPort() {
        return environment.getProperty("DB_PORT");
    }

    public String dbName() {
        return environment.getProperty("DB_NAME");
    }

    public String dbUsername() {
        return environment.getProperty("DB_USERNAME");
    }

    public String dbPassword() {
        return environment.getProperty("DB_PASSWORD");
    }

    public String amazonS3Bucket() {
        return environment.getProperty("AMAZON_S3_BUCKET");
    }

    public String salesforceUsername() {
        return environment.getProperty("SALESFORCE_USERNAME");
    }

    public String salesforcePassword() {
        return environment.getProperty("SALESFORCE_PASSWORD");
    }

    public String salesforceToken() {
        return environment.getProperty("SALESFORCE_TOKEN");
    }

    public String salesforceUrl() {
        return environment.getProperty("SALESFORCE_URL");
    }

    public String salesforceResponseTimeout() {
        return environment.getProperty("SALESFORCE_RESPONSETIMEOUT");
    }
}

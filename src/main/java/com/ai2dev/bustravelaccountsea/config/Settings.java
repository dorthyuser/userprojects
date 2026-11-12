package com.ai2dev.bustravelaccountsea.config;

import org.springframework.core.env.Environment;
import org.springframework.stereotype.Component;

@Component
public class Settings {
    private final Environment environment;

    public Settings(Environment environment) {
        this.environment = environment;
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

    public String httpsRequesterSaAccountsHost() {
        return environment.getProperty("HTTPS_REQUESTER_SA_ACCOUNTS_HOST");
    }

    public String httpsRequesterSaAccountsPort() {
        return environment.getProperty("HTTPS_REQUESTER_SA_ACCOUNTS_PORT");
    }

    public String httpsRequesterSaAccountsBasepath() {
        return environment.getProperty("HTTPS_REQUESTER_SA_ACCOUNTS_BASEPATH");
    }

    public String httpsRequesterSaAccountsResponseTimeout() {
        return environment.getProperty("HTTPS_REQUESTER_SA_ACCOUNTS_RESPONSE_TIMEOUT");
    }

    public String httpsRequesterSaAccountsClientId() {
        return environment.getProperty("HTTPS_REQUESTER_SA_ACCOUNTS_CLIENT_ID");
    }

    public String httpsRequesterSaAccountsClientSecret() {
        return environment.getProperty("HTTPS_REQUESTER_SA_ACCOUNTS_CLIENT_SECRET");
    }

    public String amazonS3Bucket() {
        return environment.getProperty("AMAZON_S3_BUCKET");
    }
}

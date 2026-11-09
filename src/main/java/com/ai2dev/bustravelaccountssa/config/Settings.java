package com.ai2dev.bustravelaccountssa.config;

import org.jspecify.annotations.Nullable;
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
        String raw = isBlank(configured) ? "api/v1/*" : configured;
        String normalized = raw.trim();
        if (normalized.endsWith("/*")) {
            normalized = normalized.substring(0, normalized.length() - 2);
        }
        if (normalized.endsWith("/")) {
            normalized = normalized.substring(0, normalized.length() - 1);
        }
        if (!normalized.isEmpty() && !normalized.startsWith("/")) {
            normalized = "/" + normalized;
        }
        return normalized;
    }

    public @Nullable String amazonS3Region() {
        return blankToNull(environment.getProperty("AMAZON_S3_REGION"));
    }

    public @Nullable String amazonS3Bucket() {
        return blankToNull(environment.getProperty("AMAZON_S3_BUCKET"));
    }

    public @Nullable String salesforceUsername() {
        return blankToNull(environment.getProperty("SALESFORCE_USERNAME"));
    }

    public @Nullable String salesforcePassword() {
        return blankToNull(environment.getProperty("SALESFORCE_PASSWORD"));
    }

    public @Nullable String salesforceToken() {
        return blankToNull(environment.getProperty("SALESFORCE_TOKEN"));
    }

    public @Nullable String salesforceUrl() {
        return blankToNull(environment.getProperty("SALESFORCE_URL"));
    }

    public @Nullable Integer salesforceResponseTimeoutMillis() {
        String primary = blankToNull(environment.getProperty("SALESFORCE_RESPONSE_TIMEOUT"));
        String fallback = blankToNull(environment.getProperty("SALESFORCE_RESPONSETIMEOUT"));
        String value = primary != null ? primary : fallback;
        return value == null ? null : Integer.valueOf(value);
    }

    private static @Nullable String blankToNull(@Nullable String value) {
        if (value == null || value.trim().isEmpty()) {
            return null;
        }
        return value.trim();
    }

    private static boolean isBlank(@Nullable String value) {
        return value == null || value.trim().isEmpty();
    }
}

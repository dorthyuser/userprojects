package com.ai2dev.bustravelaccountssa.config;

import java.net.URI;
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
        String raw = (configured == null || configured.isBlank()) ? "api/v1/*" : configured;
        String withoutWildcard = raw.endsWith("/*") ? raw.substring(0, raw.length() - 2) : raw;
        if (withoutWildcard.startsWith("/")) {
            withoutWildcard = withoutWildcard.substring(1);
        }
        if (withoutWildcard.endsWith("/")) {
            withoutWildcard = withoutWildcard.substring(0, withoutWildcard.length() - 1);
        }
        return withoutWildcard;
    }

    public @Nullable String amazonS3Region() {
        return trimToNull(environment.getProperty("AMAZON_S3_REGION"));
    }

    public @Nullable String amazonS3Bucket() {
        return trimToNull(environment.getProperty("AMAZON_S3_BUCKET"));
    }

    public @Nullable String salesforceUsername() {
        return trimToNull(environment.getProperty("SALESFORCE_USERNAME"));
    }

    public @Nullable String salesforceUrl() {
        return trimToNull(environment.getProperty("SALESFORCE_URL"));
    }

    public @Nullable String salesforceConsumerKey() {
        return trimToNull(environment.getProperty("SALESFORCE_CONSUMERKEY"));
    }

    public @Nullable String salesforceTokenEndpoint() {
        return trimToNull(environment.getProperty("SALESFORCE_TOKENENDPOINT"));
    }

    public @Nullable String salesforceResponseTimeout() {
        return trimToNull(environment.getProperty("SALESFORCE_RESPONSETIMEOUT"));
    }

    public @Nullable String salesforcePrivateKey() {
        return trimToNull(environment.getProperty("SALESFORCE_PRIVATE_KEY"));
    }

    public URI salesforceLoginBaseUri() {
        String tokenEndpoint = salesforceTokenEndpoint();
        if (tokenEndpoint != null) {
            return URI.create(tokenEndpoint);
        }
        String url = salesforceUrl();
        if (url != null) {
            return URI.create(url);
        }
        return URI.create("https://login.salesforce.com");
    }

    public long salesforceResponseTimeoutMillis() {
        String value = salesforceResponseTimeout();
        if (value == null) {
            return 1_800_000L;
        }
        return Long.parseLong(value);
    }

    private static @Nullable String trimToNull(@Nullable String value) {
        if (value == null) {
            return null;
        }
        String trimmed = value.trim();
        return trimmed.isEmpty() ? null : trimmed;
    }
}

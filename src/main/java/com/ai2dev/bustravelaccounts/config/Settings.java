package com.ai2dev.bustravelaccounts.config;

import java.util.Map;
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

    public String basePath() { return normalizeBasePath(environment.getProperty("HTTPS_LISTENER_PATH", "api/v1/*")); }
    public String dbUsername() { return secretOrFallback("username", "DB_USERNAME"); }
    public String dbPassword() { return secretOrFallback("password", "DB_PASSWORD"); }
    public String dbHost() { return secretOrFallback("host", "DB_HOST"); }
    public String dbName() { return secretOrFallback("dbname", "DB_NAME"); }
    public String dbPort() { return secretOrFallback("port", "DB_PORT"); }
    public String apiVersion() { return environment.getProperty("API_VERSION"); }
    public String muleEnv() { return environment.getProperty("MULE_ENV"); }
    public String jsonLoggerMaskedFields() { return environment.getProperty("JSON_LOGGER_MASKED_FIELDS"); }
    public String autodiscoveryApiId() { return environment.getProperty("AUTODISCOVERY_API_ID"); }
    public String muleKey() { return environment.getProperty("MULE_KEY"); }
    public String anypointPlatformClientId() { return environment.getProperty("ANYPOINT_PLATFORM_CLIENT_ID"); }
    public String anypointPlatformClientSecret() { return environment.getProperty("ANYPOINT_PLATFORM_CLIENT_SECRET"); }
    public String anypointPlatformBaseUri() { return environment.getProperty("ANYPOINT_PLATFORM_BASE_URI"); }
    public String anypointPlatformAnalyticsBaseUri() { return environment.getProperty("ANYPOINT_PLATFORM_ANALYTICS_BASE_URI"); }
    public String jsonLoggerDisabledFields() { return environment.getProperty("JSON_LOGGER_DISABLED_FIELDS"); }
    public String jsonLoggerApplicationName() { return environment.getProperty("JSON_LOGGER_APPLICATION_NAME"); }
    public String jsonLoggerApplicationVersion() { return environment.getProperty("JSON_LOGGER_APPLICATION_VERSION"); }
    public String sysMuleHome() { return environment.getProperty("SYS_MULE_HOME"); }
    public String sysFileSeparator() { return environment.getProperty("SYS_FILE_SEPARATOR"); }
    public String salesforceUsername() { return environment.getProperty("SALESFORCE_USERNAME"); }
    public String salesforcePassword() { return environment.getProperty("SALESFORCE_PASSWORD"); }
    public String salesforceToken() { return environment.getProperty("SALESFORCE_TOKEN"); }
    public String salesforceUrl() { return environment.getProperty("SALESFORCE_URL"); }
    public String salesforcePasswordToken() { return environment.getProperty("SALESFORCE_PASSWORDTOKEN"); }
    public String salesforceConsumerKey() { return environment.getProperty("SALESFORCE_CONSUMERKEY"); }
    public String salesforceConsumerSecret() { return environment.getProperty("SALESFORCE_CONSUMERSECRET"); }
    public String salesforceTokenEndpoint() { return environment.getProperty("SALESFORCE_TOKENENDPOINT"); }
    public String salesforceReconnectFrequencyInMilliseconds() { return environment.getProperty("SALESFORCE_RECONNECT_FREQUENCYINMILLISECONDS"); }
    public String salesforceReconnectCount() { return environment.getProperty("SALESFORCE_RECONNECT_COUNT"); }
    public String salesforceResponseTimeout() { return environment.getProperty("SALESFORCE_RESPONSETIMEOUT"); }

    private String secretOrFallback(String secretKey, String envKey) {
        Map<String, String> secret = secretsLoader.values();
        String value = secret.get(secretKey);
        return value != null && !value.isBlank() ? value : environment.getProperty(envKey);
    }

    private String normalizeBasePath(String path) {
        if (path == null || path.isBlank()) return "";
        String normalized = path.trim();
        if (normalized.endsWith("/*")) normalized = normalized.substring(0, normalized.length() - 2);
        if (normalized.endsWith("/")) normalized = normalized.substring(0, normalized.length() - 1);
        return normalized;
    }
}

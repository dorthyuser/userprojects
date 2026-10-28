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

    private String get(String name) {
        return environment.getProperty(name);
    }

    public String basePath() { return defaultIfBlank(get("HTTPS_LISTENER_PATH"), "api/v1").replaceAll("/\\*$", "").replaceAll("/$", ""); }
    public String dbUsername() { return secretOrEnv("username", "DB_USERNAME"); }
    public String dbPassword() { return secretOrEnv("password", "DB_PASSWORD"); }
    public String dbHost() { return secretOrEnv("host", "DB_HOST"); }
    public String dbName() { return secretOrEnv("dbname", "DB_NAME"); }
    public String dbPort() { return secretOrEnv("port", "DB_PORT"); }
    public String apiVersion() { return get("API_VERSION"); }
    public String muleEnv() { return get("MULE_ENV"); }
    public String jsonLoggerMaskedFields() { return get("JSON_LOGGER_MASKED_FIELDS"); }
    public String autodiscoveryApiId() { return get("AUTODISCOVERY_API_ID"); }
    public String muleKey() { return get("MULE_KEY"); }
    public String anypointPlatformClientId() { return get("ANYPOINT_PLATFORM_CLIENT_ID"); }
    public String anypointPlatformClientSecret() { return get("ANYPOINT_PLATFORM_CLIENT_SECRET"); }
    public String anypointPlatformBaseUri() { return get("ANYPOINT_PLATFORM_BASE_URI"); }
    public String anypointPlatformAnalyticsBaseUri() { return get("ANYPOINT_PLATFORM_ANALYTICS_BASE_URI"); }
    public String jsonLoggerDisabledFields() { return get("JSON_LOGGER_DISABLED_FIELDS"); }
    public String jsonLoggerApplicationName() { return get("JSON_LOGGER_APPLICATION_NAME"); }
    public String jsonLoggerApplicationVersion() { return get("JSON_LOGGER_APPLICATION_VERSION"); }
    public String sysMuleHome() { return get("SYS_MULE_HOME"); }
    public String sysFileSeparator() { return get("SYS_FILE_SEPARATOR"); }

    private String secretOrEnv(String secretKey, String envKey) {
        Map<String, String> values = secretsLoader.values();
        String secret = values.get(secretKey);
        return secret == null || secret.isBlank() ? get(envKey) : secret;
    }

    private String defaultIfBlank(String value, String defaultValue) {
        return value == null || value.isBlank() ? defaultValue : value;
    }
}

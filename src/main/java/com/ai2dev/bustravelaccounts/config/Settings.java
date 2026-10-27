package com.ai2dev.bustravelaccounts.config;

import org.springframework.core.env.Environment;
import org.springframework.stereotype.Component;

@Component
public class Settings {
    private final Environment environment;

    public Settings(Environment environment) {
        this.environment = environment;
    }

    private String get(String name) {
        return environment.getProperty(name);
    }

    public String basePath() { return normalizeBasePath(get("HTTPS_LISTENER_PATH"), "/api/v1/*"); }
    public String muleEnv() { return get("MULE_ENV"); }
    public String jsonLoggerMaskedFields() { return get("JSON_LOGGER_MASKED_FIELDS"); }
    public String autodiscoveryApiId() { return get("AUTODISCOVERY_API_ID"); }
    public String muleKey() { return get("MULE_KEY"); }
    public String apiVersion() { return get("API_VERSION"); }
    public String anypointPlatformClientId() { return get("ANYPOINT_PLATFORM_CLIENT_ID"); }
    public String anypointPlatformClientSecret() { return get("ANYPOINT_PLATFORM_CLIENT_SECRET"); }
    public String anypointPlatformBaseUri() { return get("ANYPOINT_PLATFORM_BASE_URI"); }
    public String anypointPlatformAnalyticsBaseUri() { return get("ANYPOINT_PLATFORM_ANALYTICS_BASE_URI"); }
    public String jsonLoggerDisabledFields() { return get("JSON_LOGGER_DISABLED_FIELDS"); }
    public String jsonLoggerApplicationName() { return get("JSON_LOGGER_APPLICATION_NAME"); }
    public String jsonLoggerApplicationVersion() { return get("JSON_LOGGER_APPLICATION_VERSION"); }
    public String sysMuleHome() { return get("SYS_MULE_HOME"); }
    public String sysFileSeparator() { return get("SYS_FILE_SEPARATOR"); }
    public String httpsListenerHost() { return get("HTTPS_LISTENER_HOST"); }
    public String httpsListenerPort() { return get("HTTPS_LISTENER_PORT"); }
    public String keystoreFilePath() { return get("KEYSTORE_FILE_PATH"); }
    public String keystoreCertAlias() { return get("KEYSTORE_CERT_ALIAS"); }
    public String keystoreKeypassword() { return get("KEYSTORE_KEYPASSWORD"); }
    public String keystoreStorepassword() { return get("KEYSTORE_STOREPASSWORD"); }
    public String dbUsername() { return get("DB_USERNAME"); }
    public String dbPassword() { return get("DB_PASSWORD"); }
    public String dbHost() { return get("DB_HOST"); }
    public String dbName() { return get("DB_NAME"); }
    public String dbPort() { return get("DB_PORT"); }

    private String normalizeBasePath(String value, String defaultValue) {
        String path = value == null || value.isBlank() ? defaultValue : value;
        if (path.endsWith("/*")) path = path.substring(0, path.length() - 2);
        if (path.endsWith("/")) path = path.substring(0, path.length() - 1);
        return path;
    }
}

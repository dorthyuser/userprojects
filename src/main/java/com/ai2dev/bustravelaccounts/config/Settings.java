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

    public String basePath() {
        String value = environment.getProperty("HTTPS_LISTENER_PATH");
        if (value == null || value.isBlank()) {
            value = "api/v1/*";
        }
        value = value.trim();
        if (value.endsWith("/*")) {
            value = value.substring(0, value.length() - 2);
        }
        if (value.endsWith("/")) {
            value = value.substring(0, value.length() - 1);
        }
        return value;
    }

    public String dbUsername() { return dbSecretOrEnv("username", "DB_USERNAME"); }
    public String dbPassword() { return dbSecretOrEnv("password", "DB_PASSWORD"); }
    public String dbHost() { return dbSecretOrEnv("host", "DB_HOST"); }
    public String dbName() { return dbSecretOrEnv("dbname", "DB_NAME"); }
    public String dbPort() { return dbSecretOrEnv("port", "DB_PORT"); }

    private String dbSecretOrEnv(String secretKey, String envKey) {
        Map<String, String> secret = secretsLoader.values();
        String value = secret.get(secretKey);
        if (value != null && !value.isBlank()) {
            return value;
        }
        return environment.getProperty(envKey);
    }
}

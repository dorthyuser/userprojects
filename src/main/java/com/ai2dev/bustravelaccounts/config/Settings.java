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

    public String basePath() { return defaultIfBlank(environment.getProperty("HTTPS_LISTENER_PATH"), "api/v1").replaceAll("/\\*$", "").replaceAll("/$", ""); }
    public String dbUsername() { return secretOrEnv("username", "DB_USERNAME"); }
    public String dbPassword() { return secretOrEnv("password", "DB_PASSWORD"); }
    public String dbHost() { return secretOrEnv("host", "DB_HOST"); }
    public String dbName() { return secretOrEnv("dbname", "DB_NAME"); }
    public String dbPort() { return secretOrEnv("port", "DB_PORT"); }

    private String secretOrEnv(String key, String envName) {
        Map<String, String> values = secretsLoader.values();
        String value = values.get(key);
        return value != null && !value.isBlank() ? value : environment.getProperty(envName);
    }

    private String defaultIfBlank(String value, String defaultValue) {
        return value == null || value.isBlank() ? defaultValue : value;
    }
}

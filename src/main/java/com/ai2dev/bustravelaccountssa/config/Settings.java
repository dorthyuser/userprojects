package com.ai2dev.bustravelaccountssa.config;

import java.util.Objects;

import org.springframework.core.env.Environment;
import org.springframework.stereotype.Component;

import software.amazon.awssdk.services.secretsmanager.SecretsManagerClient;
import software.amazon.awssdk.services.secretsmanager.model.GetSecretValueRequest;
import software.amazon.awssdk.services.secretsmanager.model.GetSecretValueResponse;
import tools.jackson.databind.JsonNode;
import tools.jackson.databind.json.JsonMapper;

@Component
public class Settings {
    private final Environment environment;
    private final JsonMapper jsonMapper;
    private volatile DbCredentials cachedCredentials;

    public Settings(Environment environment, JsonMapper jsonMapper) {
        this.environment = environment;
        this.jsonMapper = jsonMapper;
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

    public String awsSecretName() {
        return trimToNull(environment.getProperty("AWS_SECRET_NAME"));
    }

    public String dbHost() {
        return trimToNull(environment.getProperty("DB_HOST"));
    }

    public Integer dbPort() {
        String value = trimToNull(environment.getProperty("DB_PORT"));
        return value == null ? null : Integer.valueOf(value);
    }

    public String dbName() {
        return trimToNull(environment.getProperty("DB_NAME"));
    }

    public String dbUsername() {
        return trimToNull(environment.getProperty("DB_USERNAME"));
    }

    public String dbPassword() {
        return trimToNull(environment.getProperty("DB_PASSWORD"));
    }

    public DbCredentials dbCredentials() {
        DbCredentials current = cachedCredentials;
        if (current != null) {
            return current;
        }
        synchronized (this) {
            if (cachedCredentials == null) {
                cachedCredentials = resolveDbCredentials();
            }
            return cachedCredentials;
        }
    }

    private DbCredentials resolveDbCredentials() {
        String secretName = awsSecretName();
        if (secretName != null) {
            return loadDbCredentialsFromSecret(secretName);
        }
        return new DbCredentials(dbHost(), dbPort(), dbName(), dbUsername(), dbPassword());
    }

    private DbCredentials loadDbCredentialsFromSecret(String secretName) {
        try (SecretsManagerClient client = SecretsManagerClient.create()) {
            GetSecretValueResponse response = client.getSecretValue(GetSecretValueRequest.builder().secretId(secretName).build());
            JsonNode secret = jsonMapper.readTree(response.secretString());
            String host = text(secret, "host");
            Integer port = Integer.valueOf(text(secret, "port"));
            String dbName = firstNonNull(text(secret, "dbname"), text(secret, "dbName"));
            String username = text(secret, "username");
            String password = text(secret, "password");
            return new DbCredentials(host, port, dbName, username, password);
        }
    }

    private String text(JsonNode node, String field) {
        JsonNode value = node.get(field);
        return value == null ? null : trimToNull(value.asString());
    }

    private String firstNonNull(String first, String second) {
        return first != null ? first : second;
    }

    private String trimToNull(String value) {
        if (value == null) {
            return null;
        }
        String trimmed = value.trim();
        return trimmed.isEmpty() ? null : trimmed;
    }

    public record DbCredentials(String host, Integer port, String dbName, String username, String password) {
        public String jdbcUrl() {
            Objects.requireNonNull(host, "host");
            Objects.requireNonNull(port, "port");
            Objects.requireNonNull(dbName, "dbName");
            return "jdbc:postgresql://" + host + ":" + port + "/" + dbName;
        }
    }
}

package com.ai2dev.bustravelaccountssa.config;

import java.util.Map;

import org.springframework.core.env.Environment;
import org.springframework.stereotype.Component;

import software.amazon.awssdk.auth.credentials.DefaultCredentialsProvider;
import software.amazon.awssdk.services.secretsmanager.SecretsManagerClient;
import software.amazon.awssdk.services.secretsmanager.model.GetSecretValueRequest;
import tools.jackson.databind.JsonNode;
import tools.jackson.databind.json.JsonMapper;

@Component
public class Settings {
    private final Environment environment;
    private final JsonMapper jsonMapper;
    private volatile Map<String, String> secretValues;

    public Settings(Environment environment, JsonMapper jsonMapper) {
        this.environment = environment;
        this.jsonMapper = jsonMapper;
    }

    public String basePath() {
        String value = environment.getProperty("HTTPS_LISTENER_PATH", "api/v1/*");
        value = value.replaceAll("/+$", "");
        if (value.endsWith("/*")) {
            value = value.substring(0, value.length() - 2);
        }
        if (value.startsWith("/")) {
            value = value.substring(1);
        }
        return value;
    }

    public String dbHost() {
        return secretOrFallback("host", "DB_HOST");
    }

    public Integer dbPort() {
        String value = secretOrFallback("port", "DB_PORT");
        return value == null ? null : Integer.valueOf(value);
    }

    public String dbName() {
        return secretOrFallback("dbname", "DB_NAME");
    }

    public String dbUsername() {
        return secretOrFallback("username", "DB_USERNAME");
    }

    public String dbPassword() {
        return secretOrFallback("password", "DB_PASSWORD");
    }

    public String awsSecretName() {
        return environment.getProperty("AWS_SECRET_NAME");
    }

    private String secretOrFallback(String secretKey, String envKey) {
        Map<String, String> secrets = secretValues();
        if (secrets != null && secrets.containsKey(secretKey) && secrets.get(secretKey) != null && !secrets.get(secretKey).isBlank()) {
            return secrets.get(secretKey);
        }
        return environment.getProperty(envKey);
    }

    private Map<String, String> secretValues() {
        if (secretValues != null) {
            return secretValues;
        }
        synchronized (this) {
            if (secretValues != null) {
                return secretValues;
            }
            String secretName = environment.getProperty("AWS_SECRET_NAME");
            if (secretName == null || secretName.isBlank()) {
                secretValues = null;
                return null;
            }
            SecretsManagerClient client = SecretsManagerClient.builder()
                    .credentialsProvider(DefaultCredentialsProvider.create())
                    .build();
            String secretString = client.getSecretValue(GetSecretValueRequest.builder().secretId(secretName).build()).secretString();
            try {
                JsonNode node = jsonMapper.readTree(secretString);
                secretValues = Map.of(
                        "username", node.path("username").asString(null),
                        "password", node.path("password").asString(null),
                        "host", node.path("host").asString(null),
                        "port", node.path("port").asString(null),
                        "dbname", node.path("dbname").asString(null)
                );
                return secretValues;
            } catch (Exception e) {
                throw new IllegalStateException("Unable to read AWS secret", e);
            }
        }
    }
}

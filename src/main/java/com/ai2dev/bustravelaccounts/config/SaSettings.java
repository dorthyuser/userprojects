package com.ai2dev.bustravelaccounts.config;

import org.springframework.core.env.Environment;
import org.springframework.stereotype.Component;
import software.amazon.awssdk.services.secretsmanager.SecretsManagerClient;
import software.amazon.awssdk.services.secretsmanager.model.GetSecretValueRequest;
import tools.jackson.databind.JsonNode;
import tools.jackson.databind.json.JsonMapper;

import java.util.concurrent.atomic.AtomicReference;

@Component
public class SaSettings {
    private final Environment environment;
    private final JsonMapper jsonMapper;
    private final AtomicReference<JsonNode> secretCache = new AtomicReference<>();

    public SaSettings(Environment environment, JsonMapper jsonMapper) {
        this.environment = environment;
        this.jsonMapper = jsonMapper;
    }

    public String jdbcUrl() {
        return "jdbc:postgresql://" + dbHost() + ":" + dbPort() + "/" + dbName();
    }

    public String dbUser() {
        return secretOrFallback("username", "DB_USERNAME");
    }

    public String dbPassword() {
        return secretOrFallback("password", "DB_PASSWORD");
    }

    public String dbHost() {
        return secretOrFallback("host", "DB_HOST");
    }

    public String dbPort() {
        return secretOrFallback("port", "DB_PORT");
    }

    public String dbName() {
        return secretOrFallback("dbname", "DB_NAME");
    }

    private String secretOrFallback(String secretKey, String envVar) {
        JsonNode secret = secret();
        if (secret != null) {
            JsonNode node = secret.get(secretKey);
            if (node != null && !node.isNull()) {
                String value = node.asString();
                if (value != null && !value.isBlank()) {
                    return value.trim();
                }
            }
        }
        String fallback = environment.getProperty(envVar);
        return fallback == null ? null : fallback.trim();
    }

    private JsonNode secret() {
        JsonNode cached = secretCache.get();
        if (cached != null) {
            return cached;
        }
        String secretName = environment.getProperty("AWS_SECRET_NAME");
        if (secretName == null || secretName.isBlank()) {
            return null;
        }
        try (SecretsManagerClient client = SecretsManagerClient.create()) {
            String secretString = client.getSecretValue(GetSecretValueRequest.builder().secretId(secretName.trim()).build()).secretString();
            JsonNode parsed = jsonMapper.readTree(secretString);
            secretCache.compareAndSet(null, parsed);
            return secretCache.get();
        } catch (Exception e) {
            return null;
        }
    }
}

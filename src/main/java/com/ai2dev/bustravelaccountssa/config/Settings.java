package com.ai2dev.bustravelaccountssa.config;

import org.springframework.core.env.Environment;
import org.springframework.stereotype.Component;

import software.amazon.awssdk.services.secretsmanager.SecretsManagerClient;
import software.amazon.awssdk.services.secretsmanager.model.GetSecretValueRequest;

import tools.jackson.databind.JsonNode;
import tools.jackson.databind.json.JsonMapper;

@Component
public class Settings {
    private final Environment environment;
    private final JsonMapper jsonMapper;
    private volatile SecretSettings cachedSecretSettings;

    public Settings(Environment environment, JsonMapper jsonMapper) {
        this.environment = environment;
        this.jsonMapper = jsonMapper;
    }

    public String basePath() {
        String value = environment.getProperty("HTTPS_LISTENER_PATH", "api/v1/*");
        value = value.replaceAll("/\\*$", "");
        if (value.endsWith("/")) {
            value = value.substring(0, value.length() - 1);
        }
        return value;
    }

    public DbSettings dbSettings() {
        String secretName = environment.getProperty("AWS_SECRET_NAME");
        if (secretName != null && !secretName.isBlank()) {
            SecretSettings secret = cachedSecretSettings;
            if (secret == null) {
                synchronized (this) {
                    secret = cachedSecretSettings;
                    if (secret == null) {
                        secret = loadSecret(secretName);
                        cachedSecretSettings = secret;
                    }
                }
            }
            return new DbSettings(secret.host(), secret.port(), secret.dbName(), secret.username(), secret.password());
        }
        return new DbSettings(
                environment.getProperty("DB_HOST"),
                environment.getProperty("DB_PORT"),
                environment.getProperty("DB_NAME"),
                environment.getProperty("DB_USERNAME"),
                environment.getProperty("DB_PASSWORD"));
    }

    public String dbUrl() {
        DbSettings db = dbSettings();
        return "jdbc:postgresql://" + db.host() + ":" + db.port() + "/" + db.dbName();
    }

    public String dbUser() {
        return dbSettings().username();
    }

    public String dbPassword() {
        return dbSettings().password();
    }

    private SecretSettings loadSecret(String secretName) {
        try (SecretsManagerClient client = SecretsManagerClient.builder().build()) {
            String secretString = client.getSecretValue(GetSecretValueRequest.builder().secretId(secretName).build()).secretString();
            JsonNode node = jsonMapper.readTree(secretString);
            return new SecretSettings(
                    node.get("host").asString(),
                    node.get("port").asString(),
                    node.get("dbname").asString(),
                    node.get("username").asString(),
                    node.get("password").asString());
        }
    }

    public record DbSettings(String host, String port, String dbName, String username, String password) {}

    private record SecretSettings(String host, String port, String dbName, String username, String password) {}
}

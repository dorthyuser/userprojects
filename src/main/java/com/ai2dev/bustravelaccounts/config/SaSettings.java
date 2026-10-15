package com.ai2dev.bustravelaccounts.config;

import org.springframework.core.env.Environment;
import org.springframework.stereotype.Component;
import software.amazon.awssdk.services.secretsmanager.SecretsManagerClient;
import software.amazon.awssdk.services.secretsmanager.model.GetSecretValueRequest;
import tools.jackson.databind.JsonNode;
import tools.jackson.databind.json.JsonMapper;

@Component
public class SaSettings {
    private final String jdbcUrl;
    private final String dbUser;
    private final String dbPassword;

    public SaSettings(Environment environment) {
        String host = environment.getProperty("DB_HOST");
        String port = environment.getProperty("DB_PORT");
        String name = environment.getProperty("DB_NAME");
        String user = environment.getProperty("DB_USERNAME");
        String password = environment.getProperty("DB_PASSWORD");
        String secretName = environment.getProperty("AWS_SECRET_NAME");
        if (host != null && port != null && name != null && user != null && password != null) {
            this.jdbcUrl = "jdbc:postgresql://" + host + ":" + port + "/" + name;
            this.dbUser = user;
            this.dbPassword = password;
            return;
        }
        if (secretName == null || secretName.isBlank()) {
            this.jdbcUrl = null;
            this.dbUser = null;
            this.dbPassword = null;
            return;
        }
        SecretsManagerClient client = SecretsManagerClient.builder().build();
        String secret = client.getSecretValue(GetSecretValueRequest.builder().secretId(secretName).build()).secretString();
        JsonNode node = JsonMapper.builder().build().readTree(secret);
        String secretHost = node.get("host").asString();
        String secretPort = node.get("port").asString();
        String secretDb = node.get("dbname").asString();
        this.dbUser = node.get("username").asString();
        this.dbPassword = node.get("password").asString();
        this.jdbcUrl = "jdbc:postgresql://" + secretHost + ":" + secretPort + "/" + secretDb;
    }

    public String jdbcUrl() {
        return jdbcUrl;
    }

    public String dbUser() {
        return dbUser;
    }

    public String dbPassword() {
        return dbPassword;
    }
}

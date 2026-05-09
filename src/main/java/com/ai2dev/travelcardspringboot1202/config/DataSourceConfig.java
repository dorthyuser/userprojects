package com.ai2dev.travelcardspringboot1202.config;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.zaxxer.hikari.HikariConfig;
import com.zaxxer.hikari.HikariDataSource;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.context.annotation.Primary;
import software.amazon.awssdk.regions.Region;
import software.amazon.awssdk.services.secretsmanager.SecretsManagerClient;
import software.amazon.awssdk.services.secretsmanager.model.GetSecretValueRequest;
import javax.sql.DataSource;

@Configuration
public class DataSourceConfig {

    private static final Logger log = LoggerFactory.getLogger(DataSourceConfig.class);

    @Bean
    @Primary
    public DataSource dataSource() {
        String secretName = System.getenv("DB_SECRET_NAME");
        if (secretName == null || secretName.isBlank()) {
            throw new RuntimeException("DB_SECRET_NAME environment variable is required but not set");
        }
        String region = System.getenv().getOrDefault("AWS_REGION", "eu-west-2");
        log.info("Fetching DB credentials from Secrets Manager: secret={} region={}", secretName, region);

        try (SecretsManagerClient client = SecretsManagerClient.builder()
                .region(Region.of(region))
                .build()) {

            String secretJson = client.getSecretValue(
                    GetSecretValueRequest.builder().secretId(secretName).build()
            ).secretString();

            JsonNode node     = new ObjectMapper().readTree(secretJson);
            String host       = node.get("host").asText();
            int    port       = node.get("port").asInt(5432);
            String dbname     = node.get("dbname").asText();
            String username   = node.get("username").asText();
            String password   = node.get("password").asText();

            HikariConfig cfg = new HikariConfig();
            cfg.setJdbcUrl(String.format("jdbc:postgresql://%s:%d/%s", host, port, dbname));
            cfg.setUsername(username);
            cfg.setPassword(password);
            cfg.setDriverClassName("org.postgresql.Driver");
            cfg.setMaximumPoolSize(5);
            cfg.setMinimumIdle(1);
            cfg.setIdleTimeout(600000);
            cfg.setPoolName("travelcardspringboot1202HikariPool");
            cfg.addDataSourceProperty("stringtype", "unspecified");
            log.info("HikariCP pool created: url=jdbc:postgresql://{}:{}/{}", host, port, dbname);
            return new HikariDataSource(cfg);

        } catch (Exception e) {
            log.error("Failed to initialise DataSource from Secrets Manager", e);
            throw new RuntimeException("DataSource init failed", e);
        }
    }
}

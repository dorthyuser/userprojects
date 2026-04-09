package com.ai2dev.sb_lambda_testng_2.config;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.zaxxer.hikari.HikariDataSource;
import java.io.IOException;
import java.time.Duration;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.context.annotation.Primary;
import software.amazon.awssdk.awscore.exception.AwsServiceException;
import software.amazon.awssdk.regions.Region;
import software.amazon.awssdk.services.secretsmanager.SecretsManagerClient;
import software.amazon.awssdk.services.secretsmanager.model.GetSecretValueRequest;
import software.amazon.awssdk.services.secretsmanager.model.GetSecretValueResponse;

@Configuration
public class SecretsConfig {
    private static final Logger log = LoggerFactory.getLogger(SecretsConfig.class);

    @Bean
    @Primary
    public HikariDataSource dataSource() {
        String regionEnv = System.getenv().getOrDefault("AWS_REGION", "us-east-1");
        Region region = Region.of(regionEnv);
        String secretId = System.getenv().getOrDefault("DB_SECRET_NAME", "sb-lambda-testng-2/db");

        try (SecretsManagerClient sm = SecretsManagerClient.builder().region(region).build()) {
            log.info("Fetching DB secret from Secrets Manager secretId={}", secretId);
            GetSecretValueRequest req = GetSecretValueRequest.builder().secretId(secretId).build();
            GetSecretValueResponse resp = sm.getSecretValue(req);
            String secret = resp.secretString();
            ObjectMapper om = new ObjectMapper();
            JsonNode node = om.readTree(secret);
            String jdbcUrl = node.get("jdbcUrl").asText();
            String username = node.get("username").asText();
            String password = node.get("password").asText();

            HikariDataSource ds = new HikariDataSource();
            ds.setJdbcUrl(jdbcUrl);
            ds.setUsername(username);
            ds.setPassword(password);
            ds.setMaximumPoolSize(10);
            ds.setMinimumIdle(2);
            ds.setPoolName("HikariPool-sb-lambda-testng-2");
            ds.setConnectionTimeout(Duration.ofSeconds(30).toMillis());
            log.info("HikariDataSource initialized");
            return ds;
        } catch (AwsServiceException e) {
            throw new RuntimeException("Failed to fetch DB secret from Secrets Manager: " + e.getMessage(), e);
        } catch (IOException e) {
            throw new RuntimeException("Failed to parse DB secret JSON: " + e.getMessage(), e);
        }
    }
}

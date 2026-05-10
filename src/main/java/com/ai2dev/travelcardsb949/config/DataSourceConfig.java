package com.ai2dev.travelcardsb949.config;

import com.fasterxml.jackson.databind.JsonNode;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.zaxxer.hikari.HikariConfig;
import com.zaxxer.hikari.HikariDataSource;
import java.util.Optional;
import javax.sql.DataSource;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import software.amazon.awssdk.regions.Region;
import software.amazon.awssdk.services.secretsmanager.SecretsManagerClient;
import software.amazon.awssdk.services.secretsmanager.model.GetSecretValueRequest;
import software.amazon.awssdk.services.secretsmanager.model.GetSecretValueResponse;

@Configuration
public class DataSourceConfig
{
    private static final Logger logger = LoggerFactory.getLogger(DataSourceConfig.class);

    private final ObjectMapper objectMapper = new ObjectMapper();

    @Bean
    public DataSource dataSource()
    {
        String secretName = Optional.ofNullable(System.getenv("DB_SECRET_NAME")).orElseThrow(() -> new IllegalStateException("DB_SECRET_NAME is required"));
        String regionName = Optional.ofNullable(System.getenv("AWS_REGION")).orElse("eu-west-2");
        logger.info("Loading database secret from Secrets Manager secretName={}", secretName);
        try (SecretsManagerClient client = SecretsManagerClient.builder().region(Region.of(regionName)).build())
        {
            GetSecretValueResponse response = client.getSecretValue(GetSecretValueRequest.builder().secretId(secretName).build());
            JsonNode secret = objectMapper.readTree(response.secretString());
            String host = secret.path("host").asText();
            String port = secret.path("port").asText("5432");
            String dbname = secret.path("dbname").asText();
            String username = secret.path("username").asText();
            String password = secret.path("password").asText();
            String jdbcUrl = "jdbc:postgresql://" + host + ":" + port + "/" + dbname;
            HikariConfig config = new HikariConfig();
            config.setJdbcUrl(jdbcUrl);
            config.setUsername(username);
            config.setPassword(password);
            config.addDataSourceProperty("stringtype", "unspecified");
            return new HikariDataSource(config);
        }
        catch (Exception ex)
        {
            logger.error("Failed to create DataSource message={}", ex.getMessage());
            throw new IllegalStateException("Unable to initialize DataSource", ex);
        }
    }
}
package com.ai2dev.bustravelaccounts.config;

import com.zaxxer.hikari.HikariDataSource;
import java.util.Map;
import javax.sql.DataSource;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

@Configuration
public class DataSourceConfig {
    private static final Logger log = LoggerFactory.getLogger(DataSourceConfig.class);

    @Bean
    public DataSource dataSource(Settings settings, SecretsLoader secretsLoader) {
        Map<String, String> secret = secretsLoader.values();
        boolean secretConfigured = !secret.isEmpty();
        log.info("Database credentials source: {}", secretConfigured ? "AWS Secrets Manager with DB_* fallback" : "DB_* environment variables");
        String host = value(secret.get("host"), settings.dbHost());
        String port = value(secret.get("port"), settings.dbPort());
        String dbName = value(secret.get("dbname"), settings.dbName());
        String username = value(secret.get("username"), settings.dbUsername());
        String password = value(secret.get("password"), settings.dbPassword());

        HikariDataSource pool = new HikariDataSource();
        pool.setJdbcUrl("jdbc:postgresql://" + host + ":" + port + "/" + dbName);
        pool.setUsername(username);
        pool.setPassword(password);
        pool.setMaximumPoolSize(2);
        pool.setMinimumIdle(0);
        return pool;
    }

    private String value(String preferred, String fallback) {
        return preferred == null || preferred.isBlank() ? fallback : preferred;
    }
}

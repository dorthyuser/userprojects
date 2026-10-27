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
        boolean usingSecret = !secret.isEmpty();
        log.info("Database configuration source: {}", usingSecret ? "aws-secrets-manager" : "environment");

        String host = usingSecret ? secret.getOrDefault("host", settings.dbHost()) : settings.dbHost();
        String port = usingSecret ? secret.getOrDefault("port", settings.dbPort()) : settings.dbPort();
        String dbName = usingSecret ? secret.getOrDefault("dbname", settings.dbName()) : settings.dbName();
        String username = usingSecret ? secret.getOrDefault("username", settings.dbUsername()) : settings.dbUsername();
        String password = usingSecret ? secret.getOrDefault("password", settings.dbPassword()) : settings.dbPassword();

        HikariDataSource pool = new HikariDataSource();
        pool.setJdbcUrl(jdbcUrl(host, port, dbName));
        pool.setUsername(username);
        pool.setPassword(password);
        pool.setMaximumPoolSize(2);
        pool.setMinimumIdle(0);
        pool.setPoolName("bus-travel-accounts-pool");
        return pool;
    }

    private String jdbcUrl(String host, String port, String dbName) {
        return "jdbc:postgresql://" + host + ":" + port + "/" + dbName;
    }
}

package com.ai2dev.bustravelaccounts.config;

import com.zaxxer.hikari.HikariDataSource;
import java.util.Map;
import javax.sql.DataSource;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.context.annotation.Lazy;

@Configuration
public class DataSourceConfig {
    private static final Logger log = LoggerFactory.getLogger(DataSourceConfig.class);

    @Bean
    public DataSource dataSource(Settings settings, @Lazy SecretsLoader secretsLoader) {
        Map<String, String> secret = secretsLoader.values();
        String source = secret.isEmpty() ? "environment" : "SecretsManager";
        log.info("credentials source: {}", source);
        String host = secret.getOrDefault("host", settings.dbHost());
        String port = secret.getOrDefault("port", settings.dbPort());
        String dbName = secret.getOrDefault("dbname", settings.dbName());
        String username = secret.getOrDefault("username", settings.dbUsername());
        String password = secret.getOrDefault("password", settings.dbPassword());
        HikariDataSource pool = new HikariDataSource();
        pool.setMaximumPoolSize(2);
        pool.setMinimumIdle(0);
        pool.setJdbcUrl("jdbc:postgresql://" + host + ":" + port + "/" + dbName);
        pool.setUsername(username);
        pool.setPassword(password);
        return pool;
    }
}

package com.ai2dev.odata4accounts.config;

import com.zaxxer.hikari.HikariDataSource;
import java.util.Map;
import javax.sql.DataSource;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.context.annotation.Lazy;
import org.springframework.jdbc.core.simple.JdbcClient;

@Configuration
public class DataSourceConfig {
    private static final Logger log = LoggerFactory.getLogger(DataSourceConfig.class);

    @Bean
    public DataSource dataSource(Settings settings, SecretsLoader secretsLoader) {
        Map<String, String> secretValues = secretsLoader.values();
        boolean secretConfigured = !secretValues.isEmpty();
        if (secretConfigured) {
            log.info("Using database credentials from Secrets Manager");
        } else {
            log.info("Using database credentials from environment variables");
        }
        HikariDataSource pool = new HikariDataSource();
        pool.setJdbcUrl(jdbcUrl(
                secretValues.getOrDefault("host", settings.dbHost()),
                secretValues.getOrDefault("port", settings.dbPort()),
                secretValues.getOrDefault("dbname", settings.dbName())
        ));
        pool.setUsername(secretValues.getOrDefault("username", settings.dbUsername()));
        pool.setPassword(secretValues.getOrDefault("password", settings.dbPassword()));
        pool.setMaximumPoolSize(2);
        pool.setMinimumIdle(0);
        pool.setAllowPoolSuspension(true);
        return pool;
    }

    @Bean
    public JdbcClient jdbcClient(@Lazy DataSource dataSource) {
        return JdbcClient.create(dataSource);
    }

    private String jdbcUrl(String host, String port, String dbName) {
        return "jdbc:postgresql://" + host + ":" + port + "/" + dbName;
    }
}

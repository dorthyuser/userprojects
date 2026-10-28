package com.ai2dev.bustravelaccounts.config;

import java.util.Map;
import javax.sql.DataSource;
import com.zaxxer.hikari.HikariDataSource;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

@Configuration
public class DataSourceConfig {
    @Bean
    public DataSource dataSource(Settings settings, SecretsLoader secrets) {
        Map<String, String> secret = secrets.values();
        String host = secret.getOrDefault("host", settings.dbHost());
        String port = secret.getOrDefault("port", settings.dbPort());
        String dbname = secret.getOrDefault("dbname", settings.dbName());
        HikariDataSource pool = new HikariDataSource();
        pool.setMaximumPoolSize(2);
        pool.setMinimumIdle(0);
        pool.setAllowPoolSuspension(true);
        pool.setJdbcUrl("jdbc:postgresql://" + host + ":" + port + "/" + dbname);
        pool.setUsername(secret.getOrDefault("username", settings.dbUsername()));
        pool.setPassword(secret.getOrDefault("password", settings.dbPassword()));
        return pool;
    }
}

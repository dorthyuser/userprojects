package com.ai2dev.test_tc_sb_api.config;

import com.zaxxer.hikari.HikariConfig;
import com.zaxxer.hikari.HikariDataSource;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.context.annotation.Primary;

import javax.sql.DataSource;

@Configuration
public class DataSourceConfig {

    @Bean
    @Primary
    public DataSource dataSource() {
        HikariConfig config = new HikariConfig();
        config.setJdbcUrl(System.getenv().getOrDefault("JDBC_DATABASE_URL", "jdbc:postgresql://localhost:5432/db_name"));
        config.setUsername(System.getenv().getOrDefault("JDBC_DATABASE_USERNAME", "user"));
        config.setPassword(System.getenv().getOrDefault("JDBC_DATABASE_PASSWORD", "password"));
        config.setDriverClassName("org.postgresql.Driver");
        config.setMaximumPoolSize(10);
        config.setMinimumIdle(1);
        config.setIdleTimeout(600000);
        config.setPoolName("test-tc-sb-apiHikariPool");
        config.addDataSourceProperty("stringtype", "unspecified");
        return new HikariDataSource(config);
    }
}

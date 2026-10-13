package com.ai2dev.bustravelaccountssa.config;

import javax.sql.DataSource;

import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

import com.zaxxer.hikari.HikariDataSource;

@Configuration
public class DatabaseConfig {
    @Bean
    public DataSource dataSource(Settings settings) {
        HikariDataSource pool = new HikariDataSource();
        pool.setJdbcUrl(settings.dbUrl());
        pool.setUsername(settings.dbUser());
        pool.setPassword(settings.dbPassword());
        pool.setMaximumPoolSize(2);
        pool.setMinimumIdle(0);
        return pool;
    }
}

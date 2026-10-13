package com.ai2dev.bustravelaccountssa.config;

import javax.sql.DataSource;

import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

import com.zaxxer.hikari.HikariDataSource;

@Configuration
public class DataSourceConfig {
    @Bean
    public DataSource dataSource(Settings settings) {
        Settings.DbCredentials credentials = settings.dbCredentials();
        HikariDataSource pool = new HikariDataSource();
        pool.setMaximumPoolSize(2);
        pool.setMinimumIdle(0);
        pool.setJdbcUrl(credentials.jdbcUrl());
        pool.setUsername(credentials.username());
        pool.setPassword(credentials.password());
        return pool;
    }
}

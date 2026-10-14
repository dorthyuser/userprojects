package com.ai2dev.bustravelaccounts.config;

import com.zaxxer.hikari.HikariDataSource;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.context.annotation.Lazy;
import org.springframework.jdbc.core.namedparam.NamedParameterJdbcTemplate;
import javax.sql.DataSource;

@Configuration
public class SaConfig {
    @Bean
    public DataSource dataSource(SaSettings settings) {
        HikariDataSource pool = new HikariDataSource();
        pool.setJdbcUrl(settings.jdbcUrl());
        pool.setUsername(settings.dbUser());
        pool.setPassword(settings.dbPassword());
        pool.setMaximumPoolSize(2);
        pool.setMinimumIdle(0);
        return pool;
    }

    @Bean
    public NamedParameterJdbcTemplate namedParameterJdbcTemplate(@Lazy DataSource dataSource) {
        return new NamedParameterJdbcTemplate(dataSource);
    }
}

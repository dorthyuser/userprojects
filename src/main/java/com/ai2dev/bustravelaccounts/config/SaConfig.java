package com.ai2dev.bustravelaccounts.config;

import javax.sql.DataSource;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.context.annotation.Lazy;
import org.springframework.jdbc.core.namedparam.NamedParameterJdbcTemplate;
import org.springframework.jdbc.core.simple.JdbcClient;
import com.zaxxer.hikari.HikariDataSource;

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
    public JdbcClient jdbcClient(@Lazy DataSource dataSource) {
        return JdbcClient.create(new NamedParameterJdbcTemplate(dataSource));
    }
}

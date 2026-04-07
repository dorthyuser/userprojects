package com.ai2dev.demo_travelcard_sb.config;

import com.zaxxer.hikari.HikariConfig;
import com.zaxxer.hikari.HikariDataSource;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.context.annotation.Primary;
import org.springframework.jdbc.core.JdbcTemplate;
import javax.sql.DataSource;

@Configuration
public class DataSourceConfig {
    private static final Logger log = LoggerFactory.getLogger(DataSourceConfig.class);

    @Bean
    @Primary
    public DataSource dataSource() {
        HikariConfig config = new HikariConfig();
        config.setJdbcUrl(System.getenv().getOrDefault("JDBC_DATABASE_URL", "jdbc:postgresql://161.97.137.17:5432/admin_travelcards"));
        config.setUsername(System.getenv().getOrDefault("JDBC_DATABASE_USERNAME", "test_user_tc"));
        config.setPassword(System.getenv().getOrDefault("JDBC_DATABASE_PASSWORD", "test_user455_ps"));
        config.setDriverClassName("org.postgresql.Driver");
        config.setMaximumPoolSize(10);
        config.setMinimumIdle(1);
        config.setIdleTimeout(600000);
        config.setPoolName("demo-travelcard-sbHikariPool");
        config.addDataSourceProperty("stringtype", "unspecified");
        log.info("Configured HikariPool with URL: {}", config.getJdbcUrl());
        return new HikariDataSource(config);
    }

    @Bean
    public JdbcTemplate jdbcTemplate(DataSource dataSource) {
        return new JdbcTemplate(dataSource);
    }
}

package com.ai2dev.demo_travelcards_spring.config;

import com.zaxxer.hikari.HikariConfig;
import com.zaxxer.hikari.HikariDataSource;
import javax.sql.DataSource;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.context.annotation.Primary;
import org.springframework.jdbc.core.JdbcTemplate;

@Configuration
public class DataSourceConfig {
    private static final Logger log = LoggerFactory.getLogger(DataSourceConfig.class);

    @Bean
    @Primary
    public DataSource dataSource() {
        log.info("Configuring Hikari DataSource");
        HikariConfig config = new HikariConfig();
        config.setJdbcUrl(System.getenv().getOrDefault("JDBC_DATABASE_URL", "jdbc:postgresql://localhost:5432/db_name"));
        config.setUsername(System.getenv().getOrDefault("JDBC_DATABASE_USERNAME", "user"));
        config.setPassword(System.getenv().getOrDefault("JDBC_DATABASE_PASSWORD", "password"));
        config.setDriverClassName("org.postgresql.Driver");
        config.setMaximumPoolSize(10);
        config.setMinimumIdle(1);
        config.setIdleTimeout(600000);
        config.setPoolName("demo-travelcards-springHikariPool");
        config.addDataSourceProperty("stringtype", "unspecified");
        return new HikariDataSource(config);
    }

    @Bean
    public JdbcTemplate jdbcTemplate(DataSource dataSource) {
        return new JdbcTemplate(dataSource);
    }
}

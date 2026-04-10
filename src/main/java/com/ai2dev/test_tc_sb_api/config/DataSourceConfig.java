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
        config.setJdbcUrl(System.getenv().getOrDefault("JDBC_DATABASE_URL", "jdbc:postgresql://161.97.137.17:5432/admin_travelcards"));
        config.setUsername(System.getenv().getOrDefault("JDBC_DATABASE_USERNAME", "test_user_tc"));
        config.setPassword(System.getenv().getOrDefault("JDBC_DATABASE_PASSWORD", "test_user455_ps"));
        config.setDriverClassName("org.postgresql.Driver");
        config.setMaximumPoolSize(10);
        config.setMinimumIdle(1);
        config.setIdleTimeout(600000);
        config.setPoolName("test-tc-sb-apiHikariPool");
        config.addDataSourceProperty("stringtype", "unspecified");
        return new HikariDataSource(config);
    }
}

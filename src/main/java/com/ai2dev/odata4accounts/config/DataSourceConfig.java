package com.ai2dev.odata4accounts.config;

import com.zaxxer.hikari.HikariDataSource;
import javax.sql.DataSource;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.context.annotation.Lazy;
import org.springframework.jdbc.core.simple.JdbcClient;

@Configuration
public class DataSourceConfig {
    @Bean
    public DataSource dataSource(Settings settings) {
        HikariDataSource pool = new HikariDataSource();
        pool.setJdbcUrl(jdbcUrl(settings.dbHost(), settings.dbPort(), settings.dbName()));
        pool.setUsername(settings.dbUsername());
        pool.setPassword(settings.dbPassword());
        pool.setMaximumPoolSize(2);
        pool.setMinimumIdle(0);
        pool.setAllowPoolSuspension(true);
        return pool;
    }

    @Bean
    public JdbcClient jdbcClient(@Lazy DataSource dataSource) {
        return JdbcClient.create(dataSource);
    }

    private String jdbcUrl(String host, String port, String database) {
        return "jdbc:postgresql://" + host + ":" + port + "/" + database;
    }
}

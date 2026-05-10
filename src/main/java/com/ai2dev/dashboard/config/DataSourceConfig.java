package com.ai2dev.dashboard.config;

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
public class DataSourceConfig
{
    private static final Logger log = LoggerFactory.getLogger(DataSourceConfig.class);

    @Bean
    @Primary
    public DataSource dataSource()
    {
        log.info("Creating primary DataSource");
        HikariConfig config = new HikariConfig();
        String host = System.getenv().getOrDefault("MYSQL_HOST", "localhost");
        String port = System.getenv().getOrDefault("MYSQL_PORT", "3306");
        String database = System.getenv().getOrDefault("MYSQL_DATABASE", "db_name");
        String username = System.getenv().getOrDefault("MYSQL_USERNAME", "user");
        String password = System.getenv().getOrDefault("MYSQL_PASSWORD", "password");
        config.setJdbcUrl("jdbc:mysql://" + host + ":" + port + "/" + database);
        config.setUsername(username);
        config.setPassword(password);
        config.setDriverClassName("com.mysql.cj.jdbc.Driver");
        config.setMaximumPoolSize(10);
        config.setMinimumIdle(1);
        config.setIdleTimeout(600000);
        config.setPoolName("dashboardHikariPool");
        HikariDataSource dataSource = new HikariDataSource(config);
        log.info("Primary DataSource created");
        return dataSource;
    }

    @Bean
    public JdbcTemplate jdbcTemplate(DataSource dataSource)
    {
        log.info("Creating JdbcTemplate");
        return new JdbcTemplate(dataSource);
    }
}

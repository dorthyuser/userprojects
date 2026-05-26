package com.ai2dev.javasbtesting237.config;

import com.zaxxer.hikari.HikariDataSource;
import javax.sql.DataSource;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.context.annotation.Primary;

@Configuration
public class DataSourceConfig {

    @Value("${POSTGRESQLHOST:localhost}")
    private String host;

    @Value("${POSTGRESQLPORT:5432}")
    private String port;

    @Value("${POSTGRESQLDATABASE:travel}")
    private String database;

    @Value("${POSTGRESQLUSERNAME:postgres}")
    private String username;

    @Value("${POSTGRESQLPASSWORD:postgres}")
    private String password;

    @Bean
    @Primary
    public DataSource dataSource() {
        HikariDataSource dataSource = new HikariDataSource();
        dataSource.setDriverClassName("org.postgresql.Driver");
        dataSource.setJdbcUrl("jdbc:postgresql://" + host + ":" + port + "/" + database);
        dataSource.setUsername(username);
        dataSource.setPassword(password);
        dataSource.setMaximumPoolSize(10);
        dataSource.setMinimumIdle(1);
        return dataSource;
    }
}

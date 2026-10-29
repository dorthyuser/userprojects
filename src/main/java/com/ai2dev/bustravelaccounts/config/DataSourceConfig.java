package com.ai2dev.bustravelaccounts.config;

import java.util.Map;
import javax.sql.DataSource;
import com.ai2dev.bustravelaccounts.util.DbUtil;
import com.zaxxer.hikari.HikariDataSource;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

@Configuration
public class DataSourceConfig {
    @Bean
    public DataSource dataSource(Settings settings) {
        Map<String, String> values = DbUtil.emptyValues(settings);
        String host = values.getOrDefault("host", settings.dbHost());
        String port = values.getOrDefault("port", settings.dbPort());
        String dbname = values.getOrDefault("dbname", settings.dbName());
        String username = values.getOrDefault("username", settings.dbUsername());
        String password = values.getOrDefault("password", settings.dbPassword());
        HikariDataSource pool = new HikariDataSource();
        pool.setMaximumPoolSize(2);
        pool.setMinimumIdle(0);
        pool.setAllowPoolSuspension(true);
        pool.setJdbcUrl("jdbc:postgresql://" + host + ":" + port + "/" + dbname);
        pool.setUsername(username);
        pool.setPassword(password);
        return pool;
    }
}

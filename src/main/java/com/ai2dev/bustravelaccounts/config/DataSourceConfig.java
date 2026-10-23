package com.ai2dev.bustravelaccounts.config;

import java.util.Map;
import javax.sql.DataSource;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.context.annotation.Lazy;
import com.zaxxer.hikari.HikariDataSource;
import com.ai2dev.bustravelaccounts.util.DatabaseSettingsUtil;

@Configuration
public class DataSourceConfig {
    @Bean
    public DataSource dataSource(Settings settings, @Lazy SecretsLoader secrets) {
        Map<String, String> values = secrets.values();
        String host = DatabaseSettingsUtil.value(values, "host", settings.dbHost());
        String port = DatabaseSettingsUtil.value(values, "port", settings.dbPort());
        String dbName = DatabaseSettingsUtil.value(values, "dbname", settings.dbName());
        String username = DatabaseSettingsUtil.value(values, "username", settings.dbUsername());
        String password = DatabaseSettingsUtil.value(values, "password", settings.dbPassword());
        HikariDataSource pool = new HikariDataSource();
        pool.setMaximumPoolSize(2);
        pool.setMinimumIdle(0);
        pool.setJdbcUrl("jdbc:postgresql://" + host + ":" + port + "/" + dbName);
        pool.setUsername(username);
        pool.setPassword(password);
        return pool;
    }
}

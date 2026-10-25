package com.ai2dev.bustravelaccounts.config;

import java.util.Map;
import javax.sql.DataSource;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import com.zaxxer.hikari.HikariDataSource;
import com.ai2dev.bustravelaccounts.util.DbSettingsUtil;

@Configuration
public class DataSourceConfig {
    @Bean
    public DataSource dataSource(Settings settings, SecretsLoader secrets) {
        Map<String, String> secret = secrets.values();
        String host = DbSettingsUtil.value(secret, "host", settings.dbHost());
        String port = DbSettingsUtil.value(secret, "port", settings.dbPort());
        String dbname = DbSettingsUtil.value(secret, "dbname", settings.dbName());
        String username = DbSettingsUtil.value(secret, "username", settings.dbUsername());
        String password = DbSettingsUtil.value(secret, "password", settings.dbPassword());
        HikariDataSource pool = new HikariDataSource();
        pool.setMaximumPoolSize(2);
        pool.setMinimumIdle(0);
        pool.setJdbcUrl("jdbc:postgresql://" + host + ":" + port + "/" + dbname);
        pool.setUsername(username);
        pool.setPassword(password);
        return pool;
    }
}

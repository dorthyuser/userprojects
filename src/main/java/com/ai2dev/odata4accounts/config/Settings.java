package com.ai2dev.odata4accounts.config;

import org.springframework.core.env.Environment;
import org.springframework.stereotype.Component;

@Component
public class Settings {
    private final Environment environment;

    public Settings(Environment environment) {
        this.environment = environment;
    }

    public String basePath() {
        return "/api";
    }

    public String dbHost() {
        return environment.getProperty("DS_DB_HOST");
    }

    public String dbPort() {
        return environment.getProperty("DS_DB_PORT");
    }

    public String dbName() {
        return environment.getProperty("DS_DB_DATABASE");
    }

    public String dbUsername() {
        return environment.getProperty("DS_DB_USER");
    }

    public String dbPassword() {
        return environment.getProperty("DS_DB_PASSWORD");
    }

    public Integer accountsPageSize() {
        String value = environment.getProperty("SERVICE_ACCOUNTS_PAGE_SIZE");
        return value == null ? null : Integer.valueOf(value);
    }
}

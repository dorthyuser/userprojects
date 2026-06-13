package com.ai2dev.syncspringboot109.config;

import com.azure.identity.ManagedIdentityCredentialBuilder;
import com.azure.security.keyvault.secrets.SecretClient;
import com.azure.security.keyvault.secrets.SecretClientBuilder;
import com.zaxxer.hikari.HikariConfig;
import com.zaxxer.hikari.HikariDataSource;
import javax.sql.DataSource;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.context.annotation.Primary;
import org.springframework.data.jdbc.core.dialect.JdbcDialect;
import org.springframework.data.jdbc.core.dialect.JdbcPostgresDialect;
import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.jdbc.core.namedparam.NamedParameterJdbcTemplate;

@Configuration
public class DataSourceConfig
{
    private static final Logger logger = LoggerFactory.getLogger(DataSourceConfig.class);

    @Bean
    public SecretClient secretClient(@Value("${AZURE_KEY_VAULT_URI:}") String keyVaultUri)
    {
        if (keyVaultUri == null || keyVaultUri.isBlank())
        {
            return null;
        }
        return new SecretClientBuilder().vaultUrl(keyVaultUri).credential(new ManagedIdentityCredentialBuilder().build()).buildClient();
    }

    @Bean
    @Primary
    public DataSource dataSource(SecretClient secretClient)
    {
        try
        {
            var host = resolve(secretClient, "POSTGRESQLHOST");
            var port = resolve(secretClient, "POSTGRESQLPORT");
            var database = resolve(secretClient, "POSTGRESQLDATABASE");
            var username = resolve(secretClient, "POSTGRESQLUSERNAME");
            var password = resolve(secretClient, "POSTGRESQLPASSWORD");
            var config = new HikariConfig();
            config.setJdbcUrl("jdbc:postgresql://" + host + ":" + port + "/" + database);
            config.setUsername(username);
            config.setPassword(password);
            config.setMaximumPoolSize(10);
            config.setMinimumIdle(1);
            config.setIdleTimeout(600000L);
            config.setPoolName("syncspringboot109HikariPool");
            return new HikariDataSource(config);
        }
        catch (Exception ex)
        {
            logger.error("DB connection failed: {}", ex.getMessage());
            throw ex;
        }
    }

    @Bean
    public JdbcTemplate jdbcTemplate(DataSource dataSource)
    {
        return new JdbcTemplate(dataSource);
    }

    @Bean
    public NamedParameterJdbcTemplate namedParameterJdbcTemplate(DataSource dataSource)
    {
        return new NamedParameterJdbcTemplate(dataSource);
    }

    @Bean
    public JdbcDialect jdbcDialect()
    {
        return JdbcPostgresDialect.INSTANCE;
    }

    @Bean
    public org.springframework.web.client.RestTemplate apiRestTemplate()
    {
        var factory = new org.springframework.http.client.SimpleClientHttpRequestFactory();
        factory.setConnectTimeout(10000);
        factory.setReadTimeout(30000);
        return new org.springframework.web.client.RestTemplate(factory);
    }

    @Bean
    public org.springframework.web.client.RestTemplate tokenRestTemplate()
    {
        var factory = new org.springframework.http.client.SimpleClientHttpRequestFactory();
        factory.setConnectTimeout(10000);
        factory.setReadTimeout(30000);
        return new org.springframework.web.client.RestTemplate(factory);
    }

    private String resolve(SecretClient secretClient, String name)
    {
        try
        {
            if (secretClient != null)
            {
                var secret = secretClient.getSecret(name);
                if (secret != null && secret.getValue() != null && !secret.getValue().isBlank())
                {
                    return secret.getValue();
                }
            }
        }
        catch (Exception ex)
        {
            logger.warn("Key Vault fetch failed for {}: {}", name, ex.getMessage());
        }
        var value = System.getenv(name);
        if (value == null || value.isBlank())
        {
            throw new IllegalStateException("Missing required environment variable: " + name);
        }
        return value;
    }
}

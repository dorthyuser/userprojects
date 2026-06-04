package com.ai2dev.springboottravelcardapi.config;

import com.azure.identity.DefaultAzureCredentialBuilder;
import com.azure.security.keyvault.secrets.SecretClient;
import com.azure.security.keyvault.secrets.SecretClientBuilder;
import com.zaxxer.hikari.HikariDataSource;
import java.util.Optional;
import javax.sql.DataSource;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.context.annotation.Primary;
import org.springframework.jdbc.core.JdbcTemplate;

@Configuration
public class DataSourceConfig
{
    private static final Logger logger = LoggerFactory.getLogger(DataSourceConfig.class);

    @Value("${AZURE_KEY_VAULT_URI:}")
    private String keyVaultUri;

    @Bean
    @Primary
    public DataSource dataSource()
    {
        logger.info("Creating datasource");
        String host = secret("POSTGRESQLHOST");
        String port = secret("POSTGRESQLPORT");
        String database = secret("POSTGRESQLDATABASE");
        String username = secret("POSTGRESQLUSERNAME");
        String password = secret("POSTGRESQLPASSWORD");

        HikariDataSource dataSource = new HikariDataSource();
        dataSource.setPoolName("springboottravelcardapiHikariPool");
        dataSource.setMaximumPoolSize(10);
        dataSource.setMinimumIdle(1);
        dataSource.setIdleTimeout(600000L);
        dataSource.setJdbcUrl("jdbc:postgresql://" + host + ":" + port + "/" + database);
        dataSource.setUsername(username);
        dataSource.setPassword(password);
        return dataSource;
    }

    @Bean
    public JdbcTemplate jdbcTemplate(DataSource dataSource)
    {
        return new JdbcTemplate(dataSource);
    }

    private String secret(String name)
    {
        try
        {
            if (keyVaultUri != null && !keyVaultUri.isBlank())
            {
                SecretClient client = new SecretClientBuilder()
                        .vaultUrl(keyVaultUri)
                        .credential(new DefaultAzureCredentialBuilder().build())
                        .buildClient();
                return Optional.ofNullable(client.getSecret(name)).map(secret -> secret.getValue()).orElseThrow();
            }
        }
        catch (Exception ex)
        {
            logger.warn("Key Vault lookup failed for secret {}", name);
        }
        String value = System.getenv(name);
        if (value == null || value.isBlank())
        {
            throw new IllegalArgumentException("Missing required database configuration");
        }
        return value;
    }
}

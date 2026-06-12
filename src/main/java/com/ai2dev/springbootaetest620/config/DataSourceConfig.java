package com.ai2dev.springbootaetest620.config;

import com.azure.identity.ManagedIdentityCredentialBuilder;
import com.azure.security.keyvault.secrets.SecretClient;
import com.azure.security.keyvault.secrets.SecretClientBuilder;
import com.zaxxer.hikari.HikariConfig;
import com.zaxxer.hikari.HikariDataSource;
import java.util.Objects;
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

    @Value("${AZURE_KEY_VAULT_URI:}")
    private String keyVaultUri;

    @Bean
    @Primary
    public DataSource dataSource()
    {
        String host = null;
        String port = null;
        String database = null;
        String username = null;
        String password = null;
        try
        {
            if (keyVaultUri != null && !keyVaultUri.isBlank())
            {
                SecretClient client = new SecretClientBuilder().vaultUrl(keyVaultUri).credential(new ManagedIdentityCredentialBuilder().build()).buildClient();
                host = client.getSecret("POSTGRESQLHOST").getValue();
                port = client.getSecret("POSTGRESQLPORT").getValue();
                database = client.getSecret("POSTGRESQLDATABASE").getValue();
                username = client.getSecret("POSTGRESQLUSERNAME").getValue();
                password = client.getSecret("POSTGRESQLPASSWORD").getValue();
            }
        }
        catch (Exception ex)
        {
            logger.error("Key Vault fetch failed: {}", ex.getMessage());
        }
        host = firstNonBlank(host, System.getenv("POSTGRESQLHOST"));
        port = firstNonBlank(port, System.getenv("POSTGRESQLPORT"));
        database = firstNonBlank(database, System.getenv("POSTGRESQLDATABASE"));
        username = firstNonBlank(username, System.getenv("POSTGRESQLUSERNAME"));
        password = firstNonBlank(password, System.getenv("POSTGRESQLPASSWORD"));
        require(host, "POSTGRESQLHOST");
        require(port, "POSTGRESQLPORT");
        require(database, "POSTGRESQLDATABASE");
        require(username, "POSTGRESQLUSERNAME");
        require(password, "POSTGRESQLPASSWORD");
        try
        {
            HikariConfig config = new HikariConfig();
            config.setJdbcUrl("jdbc:postgresql://" + host + ":" + port + "/" + database);
            config.setUsername(username);
            config.setPassword(password);
            config.setMaximumPoolSize(10);
            config.setMinimumIdle(1);
            config.setIdleTimeout(600000L);
            config.setPoolName("springbootaetest620HikariPool");
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

    private String firstNonBlank(String first, String second)
    {
        return first != null && !first.isBlank() ? first : second;
    }

    private void require(String value, String name)
    {
        if (value == null || value.isBlank())
        {
            logger.error("Missing required environment variable: {}", name);
            throw new IllegalStateException("Missing required environment variable: " + name);
        }
    }
}

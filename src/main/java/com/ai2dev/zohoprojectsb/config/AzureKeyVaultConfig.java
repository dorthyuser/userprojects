package com.ai2dev.zohoprojectsb.config;

import com.azure.identity.DefaultAzureCredentialBuilder;
import com.azure.security.keyvault.secrets.SecretClient;
import com.azure.security.keyvault.secrets.SecretClientBuilder;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.http.client.SimpleClientHttpRequestFactory;
import org.springframework.web.client.RestTemplate;

import java.time.Instant;

@Configuration
public class AzureKeyVaultConfig {
    private static final Logger logger = LoggerFactory.getLogger(AzureKeyVaultConfig.class);

    @Bean
    public SecretClient secretClient() {
        logger.info("Initializing Azure SecretClient");
        String kvUrl = System.getenv("AZURE_KEY_VAULT");
        if (kvUrl == null || kvUrl.isBlank()) {
            throw new IllegalStateException("Environment variable AZURE_KEY_VAULT must be set to the Key Vault URL");
        }

        SecretClient client = new SecretClientBuilder()
            .vaultUrl(kvUrl)
            .credential(new DefaultAzureCredentialBuilder().build())
            .buildClient();

        logger.info("Azure SecretClient initialized");
        return client;
    }

    @Bean(name = "tokenRestTemplate")
    public RestTemplate tokenRestTemplate() {
        logger.info("Creating token RestTemplate");
        SimpleClientHttpRequestFactory rf = new SimpleClientHttpRequestFactory();
        rf.setConnectTimeout(10_000);
        rf.setReadTimeout(30_000);
        RestTemplate rt = new RestTemplate(rf);
        logger.info("Token RestTemplate created");
        return rt;
    }

    @Bean(name = "apiRestTemplate")
    public RestTemplate apiRestTemplate() {
        logger.info("Creating API RestTemplate");
        SimpleClientHttpRequestFactory rf = new SimpleClientHttpRequestFactory();
        rf.setConnectTimeout(10_000);
        rf.setReadTimeout(30_000);
        RestTemplate rt = new RestTemplate(rf);
        logger.info("API RestTemplate created");
        return rt;
    }

    @Bean
    public com.ai2dev.zohoprojectsb.config.ZohoConfig zohoConfig(SecretClient secretClient) {
        logger.info("Resolving Zoho configuration secrets from Key Vault at startup");

        String baseUrlKey = System.getenv("ZOHO_BASE_URL");
        String clientIdKey = System.getenv("ZOHO_CLIENT_ID");
        String clientSecretKey = System.getenv("ZOHO_CLIENT_SECRET");
        String tokenUrlKey = System.getenv("ZOHO_TOKEN_URL");
        String refreshTokenKey = System.getenv("ZOHO_REFRESH_TOKEN");

        if (baseUrlKey == null || clientIdKey == null || clientSecretKey == null || tokenUrlKey == null || refreshTokenKey == null) {
            throw new IllegalStateException("One or more required environment variables are not set: ZOHO_BASE_URL, ZOHO_CLIENT_ID, ZOHO_CLIENT_SECRET, ZOHO_TOKEN_URL, ZOHO_REFRESH_TOKEN");
        }

        String baseUrl = secretClient.getSecret(baseUrlKey).getValue();
        String clientId = secretClient.getSecret(clientIdKey).getValue();
        String clientSecret = secretClient.getSecret(clientSecretKey).getValue();
        String tokenUrl = secretClient.getSecret(tokenUrlKey).getValue();
        String refreshToken = secretClient.getSecret(refreshTokenKey).getValue();

        if (baseUrl == null || baseUrl.isBlank()) {
            throw new IllegalStateException("Resolved ZOHO_BASE_URL is empty");
        }

        String normalizedBase = baseUrl.endsWith("/") ? baseUrl.substring(0, baseUrl.length() - 1) : baseUrl;

        ZohoConfig cfg = new ZohoConfig();
        cfg.setBaseUrl(normalizedBase);
        cfg.setClientId(clientId);
        cfg.setClientSecret(clientSecret);
        cfg.setTokenUrl(tokenUrl);
        cfg.setRefreshToken(refreshToken);
        cfg.setScopes("ZohoCRM.users.ALL");
        cfg.setResolvedAt(Instant.now());

        logger.info("Zoho configuration resolved and stored");
        return cfg;
    }
}

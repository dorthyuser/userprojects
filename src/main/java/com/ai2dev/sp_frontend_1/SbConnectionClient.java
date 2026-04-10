package com.ai2dev.sp_frontend_1;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.context.annotation.Bean;
import org.springframework.core.env.Environment;
import org.springframework.http.HttpEntity;
import org.springframework.http.HttpHeaders;
import org.springframework.http.MediaType;
import org.springframework.http.client.ClientHttpRequestExecution;
import org.springframework.http.client.ClientHttpRequestInterceptor;
import org.springframework.http.client.ClientHttpResponse;
import org.springframework.stereotype.Component;
import org.springframework.web.client.HttpClientErrorException;
import org.springframework.web.client.RestClientException;
import org.springframework.web.client.RestTemplate;

import software.amazon.awssdk.regions.Region;
import software.amazon.awssdk.services.secretsmanager.SecretsManagerClient;
import software.amazon.awssdk.services.secretsmanager.model.GetSecretValueRequest;
import software.amazon.awssdk.services.secretsmanager.model.GetSecretValueResponse;

import java.io.BufferedReader;
import java.io.IOException;
import java.io.InputStreamReader;
import java.net.URI;
import java.net.URLEncoder;
import java.nio.charset.StandardCharsets;
import java.time.Instant;
import java.util.ArrayList;
import java.util.Base64;
import java.util.List;
import java.util.Map;
import java.util.concurrent.atomic.AtomicReference;

import com.fasterxml.jackson.core.type.TypeReference;
import com.fasterxml.jackson.databind.ObjectMapper;

/**
 * sb-connection: Configures an HTTPS RestTemplate with OAuth2 client credentials support.
 *
 * Authentication values are resolved using @Value or from AWS Secrets Manager when missing.
 * This class fetches a token using the client credentials flow and attaches it as
 * Authorization: Bearer &lt;access_token&gt; on every outgoing request.
 */
@Component
public class SbConnectionClient {
    private static final Logger logger = LoggerFactory.getLogger(SbConnectionClient.class);

    private final Environment env;

    @Value("${OAUTH_SECRET_NAME:}")
    private String oauthSecretName;

    @Value("${AZURE-CLIENT-ID:}")
    private String azureClientIdProp;

    @Value("${AZURE-CLIENT-SECRET:}")
    private String azureClientSecretProp;

    @Value("${AZURE-TOKEN-URL:}")
    private String azureTokenUrlProp;

    @Value("${AZURE-SCOPES:}")
    private String azureScopesProp;

    private final AtomicReference<String> accessToken = new AtomicReference<>();
    private volatile Instant tokenExpiry = Instant.EPOCH;

    private final ObjectMapper mapper = new ObjectMapper();

    public SbConnectionClient(Environment env) {
        this.env = env;
        logger.info("Initializing SbConnectionClient");
    }

    /**
     * Exposes a RestTemplate that attaches an Authorization header with a Bearer token.
     * The token is fetched using client credentials grant before the first request and
     * refreshed on expiry or on receiving HTTP 401 (handled by retry logic).
     */
    @Bean
    public RestTemplate restTemplate() {
        RestTemplate rt = new RestTemplate();

        List<ClientHttpRequestInterceptor> interceptors = new ArrayList<>();
        interceptors.add(new ClientHttpRequestInterceptor() {
            @Override
            public ClientHttpResponse intercept(org.springframework.http.HttpRequest request, byte[] body, ClientHttpRequestExecution execution) throws IOException {
                try {
                    ensureCredentialsPresent();

                    String token = getValidToken();

                    request.getHeaders().set(HttpHeaders.AUTHORIZATION, "Bearer " + token);

                    String clientId = resolveValue("AZURE-CLIENT-ID", azureClientIdProp);

                    if (clientId != null && !clientId.isEmpty()) {
                        request.getHeaders().set("client_id", clientId);
                    }

                    if (!request.getHeaders().containsKey(HttpHeaders.CONTENT_TYPE)) {
                        request.getHeaders().setContentType(MediaType.APPLICATION_JSON);
                    }

                    ClientHttpResponse response = execution.execute(request, body);

                    if (response.getRawStatusCode() == 401) {
                        logger.warn("Received 401 from endpoint, attempting token refresh and single retry");
                        synchronized (SbConnectionClient.this) {
                            // Force refresh
                            accessToken.set(null);
                            tokenExpiry = Instant.EPOCH;
                        }

                        String refreshed = getValidToken();
                        request.getHeaders().set(HttpHeaders.AUTHORIZATION, "Bearer " + refreshed);

                        return execution.execute(request, body);
                    }

                    return response;
                } catch (IOException e) {
                    logger.error("IO error in HTTP interceptor", e);
                    throw e;
                } catch (Exception e) {
                    logger.error("Error in HTTP interceptor", e);
                    throw new RestClientException("Interceptor failure", e);
                }
            }
        });

        rt.setInterceptors(interceptors);

        logger.info("RestTemplate for sb-connection initialized");

        return rt;
    }

    private void ensureCredentialsPresent() {
        // If properties are blank try to fetch from Secrets Manager using OAUTH_SECRET_NAME
        if ((azureClientIdProp == null || azureClientIdProp.isEmpty()) || (azureClientSecretProp == null || azureClientSecretProp.isEmpty()) || (azureTokenUrlProp == null || azureTokenUrlProp.isEmpty())) {
            if (oauthSecretName == null || oauthSecretName.isEmpty()) {
                logger.error("OAuth credentials and OAUTH_SECRET_NAME are not configured");
                throw new IllegalStateException("OAuth configuration missing: set AZURE-CLIENT-ID, AZURE-CLIENT-SECRET, AZURE-TOKEN-URL or provide OAUTH_SECRET_NAME");
            }

            try {
                String secret = fetchSecret(oauthSecretName);

                Map<String, String> map = mapper.readValue(secret, new TypeReference<Map<String, String>>() {});

                if ((azureClientIdProp == null || azureClientIdProp.isEmpty()) && map.containsKey("AZURE-CLIENT-ID")) {
                    azureClientIdProp = map.get("AZURE-CLIENT-ID");
                }
                if ((azureClientSecretProp == null || azureClientSecretProp.isEmpty()) && map.containsKey("AZURE-CLIENT-SECRET")) {
                    azureClientSecretProp = map.get("AZURE-CLIENT-SECRET");
                }
                if ((azureTokenUrlProp == null || azureTokenUrlProp.isEmpty()) && map.containsKey("AZURE-TOKEN-URL")) {
                    azureTokenUrlProp = map.get("AZURE-TOKEN-URL");
                }
                if ((azureScopesProp == null || azureScopesProp.isEmpty()) && map.containsKey("AZURE-SCOPES")) {
                    azureScopesProp = map.get("AZURE-SCOPES");
                }

                logger.info("Loaded OAuth values from Secrets Manager");
            } catch (Exception e) {
                logger.error("Failed to load secret {}", oauthSecretName, e);
                throw new IllegalStateException("Unable to load OAuth secrets", e);
            }
        }
    }

    private String fetchSecret(String secretName) {
        logger.info("Fetching secret from AWS Secrets Manager: {}", secretName);
        try (SecretsManagerClient client = SecretsManagerClient.builder().region(Region.of(System.getenv().getOrDefault("AWS_REGION", "us-east-1"))).build()) {
            GetSecretValueRequest req = GetSecretValueRequest.builder().secretId(secretName).build();
            GetSecretValueResponse resp = client.getSecretValue(req);
            if (resp.secretString() != null) {
                return resp.secretString();
            }

            // If secret is binary
            return new String(resp.secretBinary().asByteArray(), StandardCharsets.UTF_8);
        }
    }

    private synchronized String getValidToken() {
        String current = accessToken.get();
        if (current != null && Instant.now().isBefore(tokenExpiry.minusSeconds(30))) {
            return current;
        }

        logger.info("Fetching new OAuth2 access token");
        try {
            String clientId = resolveValue("AZURE-CLIENT-ID", azureClientIdProp);
            String clientSecret = resolveValue("AZURE-CLIENT-SECRET", azureClientSecretProp);
            String tokenUrl = resolveValue("AZURE-TOKEN-URL", azureTokenUrlProp);
            String scopes = resolveValue("AZURE-SCOPES", azureScopesProp);

            if (clientId == null || clientSecret == null || tokenUrl == null) {
                logger.error("OAuth client configuration is incomplete");
                throw new IllegalStateException("OAuth client configuration incomplete");
            }

            RestTemplate rt = new RestTemplate();

            HttpHeaders headers = new HttpHeaders();
            headers.setContentType(MediaType.APPLICATION_FORM_URLENCODED);
            String basic = clientId + ":" + clientSecret;
            headers.set(HttpHeaders.AUTHORIZATION, "Basic " + Base64.getEncoder().encodeToString(basic.getBytes(StandardCharsets.UTF_8)));

            String body = "grant_type=client_credentials" + (scopes != null && !scopes.isEmpty() ? "&scope=" + URLEncoder.encode(scopes, StandardCharsets.UTF_8) : "");

            HttpEntity<String> entity = new HttpEntity<>(body, headers);

            String resp = rt.postForObject(URI.create(tokenUrl), entity, String.class);

            Map<String, Object> map = mapper.readValue(resp, new TypeReference<Map<String, Object>>() {});

            if (!map.containsKey("access_token")) {
                logger.error("Token response missing access_token");
                throw new TokenException("Token response did not contain access_token");
            }

            String token = String.valueOf(map.get("access_token"));

            long expiresIn = 300; // default
            if (map.containsKey("expires_in")) {
                try {
                    expiresIn = Long.parseLong(String.valueOf(map.get("expires_in")));
                } catch (NumberFormatException ignored) {
                }
            }

            accessToken.set(token);
            tokenExpiry = Instant.now().plusSeconds(expiresIn);

            logger.info("Fetched OAuth2 token, expires in {} seconds", expiresIn);

            return token;
        } catch (HttpClientErrorException e) {
            logger.error("Token endpoint returned error", e);
            throw new TokenException("Token endpoint error: " + e.getStatusCode());
        } catch (Exception e) {
            logger.error("Failed to obtain access token", e);
            throw new TokenException("Failed to obtain access token", e);
        }
    }

    private String resolveValue(String key, String propValue) {
        if (propValue != null && !propValue.isEmpty()) {
            return propValue;
        }
        String fromEnv = env.getProperty(key);
        if (fromEnv != null && !fromEnv.isEmpty()) {
            return fromEnv;
        }
        return null;
    }
}

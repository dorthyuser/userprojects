package com.ai2dev.syncspringboot109.service;

import com.azure.security.keyvault.secrets.SecretClient;
import com.ai2dev.syncspringboot109.model.dto.ZohoTokenResponseDto;
import java.net.URI;
import java.time.Instant;
import java.util.Map;
import java.util.Objects;
import java.util.concurrent.locks.ReentrantLock;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.http.HttpEntity;
import org.springframework.http.HttpHeaders;
import org.springframework.http.HttpMethod;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.stereotype.Service;
import org.springframework.util.LinkedMultiValueMap;
import org.springframework.util.MultiValueMap;
import org.springframework.web.client.HttpStatusCodeException;
import org.springframework.web.client.RestTemplate;
import org.springframework.web.util.UriComponentsBuilder;

@Service
public class ZohoHttpConnectionConnection implements IZohoHttpConnectionConnection
{
    private static final Logger logger = LoggerFactory.getLogger(ZohoHttpConnectionConnection.class);

    private final RestTemplate apiRestTemplate;
    private final RestTemplate tokenRestTemplate;
    private final SecretClient secretClient;
    private final String baseUrl;
    private final String tokenUrl;
    private final String clientId;
    private final String clientSecret;
    private final String refreshToken;
    private final ReentrantLock tokenLock = new ReentrantLock();

    private volatile String accessToken;
    private volatile Instant tokenExpiry = Instant.MIN;
    private volatile String apiDomain;

    public ZohoHttpConnectionConnection(RestTemplate apiRestTemplate, RestTemplate tokenRestTemplate, SecretClient secretClient, @Value("${zoho.base-url:}") String baseUrl)
    {
        this.apiRestTemplate = apiRestTemplate;
        this.tokenRestTemplate = tokenRestTemplate;
        this.secretClient = secretClient;
        this.baseUrl = normalizeBaseUrl(resolveBaseUrl(baseUrl));
        this.tokenUrl = requireEnv("ZOHOTOKENURL");
        this.clientId = resolveSecret("ZOHOCLIENTID");
        this.clientSecret = resolveSecret("ZOHOCLIENTSECRET");
        this.refreshToken = resolveSecret("ZOHOREFRESHTOKEN");
    }

    @Override
    public ResponseEntity<String> execute(HttpMethod method, URI uri, HttpHeaders headers, Object body)
    {
        ensureToken();
        return doExecute(method, uri, headers, body, false);
    }

    @Override
    public ResponseEntity<String> executeTokenRequest()
    {
        var form = new LinkedMultiValueMap<String, String>();
        form.add("grant_type", "refresh_token");
        form.add("client_id", clientId);
        form.add("client_secret", clientSecret);
        form.add("refresh_token", refreshToken);
        var headers = new HttpHeaders();
        headers.setContentType(MediaType.APPLICATION_FORM_URLENCODED);
        var entity = new HttpEntity<MultiValueMap<String, String>>(form, headers);
        return tokenRestTemplate.exchange(URI.create(tokenUrl), HttpMethod.POST, entity, String.class);
    }

    private ResponseEntity<String> doExecute(HttpMethod method, URI uri, HttpHeaders headers, Object body, boolean retried)
    {
        var requestHeaders = new HttpHeaders();
        requestHeaders.putAll(headers);
        requestHeaders.set("Authorization", "Zoho-oauthtoken " + accessToken);
        var entity = new HttpEntity<>(body, requestHeaders);
        try
        {
            return apiRestTemplate.exchange(resolveUri(uri), method, entity, String.class);
        }
        catch (HttpStatusCodeException ex)
        {
            if (ex.getStatusCode().value() == 401 && !retried)
            {
                forceRefresh();
                return doExecute(method, uri, headers, body, true);
            }
            throw new RuntimeException("Connection error: " + ex.getStatusCode() + " - " + ex.getResponseBodyAsString(), ex);
        }
        catch (Exception ex)
        {
            throw new RuntimeException("Connection error: " + ex.getMessage(), ex);
        }
    }

    private URI resolveUri(URI uri)
    {
        var effectiveBase = apiDomain != null && !apiDomain.isBlank() ? apiDomain : baseUrl;
        return UriComponentsBuilder.newInstance().scheme(URI.create(effectiveBase).getScheme()).host(URI.create(effectiveBase).getHost()).port(URI.create(effectiveBase).getPort()).path(uri.getPath()).query(uri.getQuery()).build(true).toUri();
    }

    private void ensureToken()
    {
        if (!isTokenExpired())
        {
            return;
        }
        tokenLock.lock();
        try
        {
            if (isTokenExpired())
            {
                refreshToken();
            }
        }
        finally
        {
            tokenLock.unlock();
        }
    }

    private void forceRefresh()
    {
        tokenLock.lock();
        try
        {
            tokenExpiry = Instant.MIN;
            refreshToken();
        }
        finally
        {
            tokenLock.unlock();
        }
    }

    private void refreshToken()
    {
        try
        {
            var response = executeTokenRequest();
            if (!response.getStatusCode().is2xxSuccessful())
            {
                throw new RuntimeException("Token refresh failed: " + response.getStatusCode() + " - " + response.getBody());
            }
            var body = Objects.requireNonNull(response.getBody(), "Token refresh failed: empty body");
            var token = new com.fasterxml.jackson.databind.ObjectMapper().readValue(body, ZohoTokenResponseDto.class);
            if (token.access_token() == null || token.access_token().isBlank())
            {
                throw new RuntimeException("Token refresh failed: " + response.getStatusCode() + " - " + body);
            }
            accessToken = token.access_token();
            apiDomain = token.api_domain() != null && !token.api_domain().isBlank() ? token.api_domain() : baseUrl;
            var expiresIn = token.expires_in() == null || token.expires_in() <= 0 ? 3600 : token.expires_in();
            if (expiresIn < 60)
            {
                tokenExpiry = Instant.MIN;
            }
            else
            {
                tokenExpiry = Instant.now().plusSeconds(expiresIn - 30L);
            }
        }
        catch (Exception ex)
        {
            if (ex instanceof RuntimeException runtimeException && runtimeException.getMessage() != null && runtimeException.getMessage().startsWith("Token refresh failed:"))
            {
                throw runtimeException;
            }
            throw new RuntimeException("Token refresh failed: 500 - " + ex.getMessage(), ex);
        }
    }

    private boolean isTokenExpired()
    {
        return accessToken == null || accessToken.isBlank() || Instant.now().isAfter(tokenExpiry) || Instant.now().equals(tokenExpiry);
    }

    private String resolveSecret(String name)
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
        return requireEnv(name);
    }

    private String resolveBaseUrl(String configured)
    {
        var env = System.getenv("ZOHO_KEY_URL");
        if (env != null && !env.isBlank())
        {
            return env;
        }
        return configured;
    }

    private String normalizeBaseUrl(String value)
    {
        if (value == null || value.isBlank())
        {
            throw new IllegalStateException("Missing required environment variable: ZOHO_KEY_URL");
        }
        return value.endsWith("/") ? value.substring(0, value.length() - 1) : value;
    }

    private String requireEnv(String name)
    {
        var value = System.getenv(name);
        if (value == null || value.isBlank())
        {
            throw new IllegalStateException("Missing required environment variable: " + name);
        }
        return value;
    }
}

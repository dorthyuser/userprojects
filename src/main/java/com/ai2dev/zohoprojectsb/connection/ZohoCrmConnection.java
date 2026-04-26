package com.ai2dev.zohoprojectsb.connection;

import com.ai2dev.zohoprojectsb.config.ZohoConfig;
import com.fasterxml.jackson.core.type.TypeReference;
import com.fasterxml.jackson.databind.ObjectMapper;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.http.HttpEntity;
import org.springframework.http.HttpHeaders;
import org.springframework.http.HttpMethod;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.stereotype.Service;
import org.springframework.web.client.HttpStatusCodeException;
import org.springframework.web.client.RestTemplate;

import java.net.URI;
import java.time.Instant;
import java.util.HashMap;
import java.util.Map;

@Service
public class ZohoCrmConnection implements IZohoCrmConnection {
    private static final Logger logger = LoggerFactory.getLogger(ZohoCrmConnection.class);

    private final RestTemplate apiRestTemplate;

    private final RestTemplate tokenRestTemplate;

    private final ZohoConfig cfg;

    private final ObjectMapper mapper = new ObjectMapper();

    private volatile String accessToken;

    private volatile Instant tokenExpiry;

    private volatile String apiDomain;

    public ZohoCrmConnection(@org.springframework.beans.factory.annotation.Qualifier("apiRestTemplate") RestTemplate apiRestTemplate,
                              @org.springframework.beans.factory.annotation.Qualifier("tokenRestTemplate") RestTemplate tokenRestTemplate,
                              ZohoConfig cfg) {
        this.apiRestTemplate = apiRestTemplate;
        this.tokenRestTemplate = tokenRestTemplate;
        this.cfg = cfg;
        logger.info("ZohoCrmConnection initialized with base URL: {}", cfg.getBaseUrl());
    }

    @Override
    public ResponseEntity<String> getUsers() {
        logger.info("Entering getUsers");
        try {
            ensureValidToken();
            String relative = "/crm/v2/users";
            URI uri = buildUri(relative);

            HttpHeaders headers = new HttpHeaders();
            headers.set("Authorization", "Zoho-oauthtoken " + accessToken);
            headers.setAccept(java.util.List.of(MediaType.APPLICATION_JSON));

            HttpEntity<Void> request = new HttpEntity<>(headers);

            try {
                ResponseEntity<String> resp = apiRestTemplate.exchange(uri, HttpMethod.GET, request, String.class);
                logger.info("Exiting getUsers with status {}", resp.getStatusCodeValue());
                return resp;
            } catch (HttpStatusCodeException ex) {
                if (ex.getStatusCode().value() == 401) {
                    logger.warn("Received 401, forcing token refresh and retrying once");
                    forceRefreshTokenAndRetry(() -> apiRestTemplate.exchange(buildUri(relative), HttpMethod.GET, new HttpEntity<>(new HttpHeaders() {{ put("Authorization", java.util.List.of("Zoho-oauthtoken " + accessToken)); setAccept(java.util.List.of(MediaType.APPLICATION_JSON)); }}), String.class));
                }
                throw ex;
            }
        } catch (Exception ex) {
            logger.error("Error in getUsers", ex);
            throw ex;
        }
    }

    @Override
    public ResponseEntity<String> createUser(String payload) {
        logger.info("Entering createUser");
        try {
            ensureValidToken();
            String relative = "/crm/v2/users";
            URI uri = buildUri(relative);

            HttpHeaders headers = new HttpHeaders();
            headers.setContentType(MediaType.APPLICATION_JSON);
            headers.set("Authorization", "Zoho-oauthtoken " + accessToken);

            HttpEntity<String> request = new HttpEntity<>(payload, headers);

            try {
                ResponseEntity<String> resp = apiRestTemplate.exchange(uri, HttpMethod.POST, request, String.class);
                logger.info("Exiting createUser with status {}", resp.getStatusCodeValue());
                return resp;
            } catch (HttpStatusCodeException ex) {
                if (ex.getStatusCode().value() == 401) {
                    logger.warn("Received 401 on createUser, forcing token refresh and retrying once");
                    forceRefreshTokenAndRetry(() -> apiRestTemplate.exchange(buildUri(relative), HttpMethod.POST, new HttpEntity<>(payload, new HttpHeaders() {{ setContentType(MediaType.APPLICATION_JSON); put("Authorization", java.util.List.of("Zoho-oauthtoken " + accessToken)); }}), String.class));
                }
                throw ex;
            }
        } catch (Exception ex) {
            logger.error("Error in createUser", ex);
            throw ex;
        }
    }

    @Override
    public ResponseEntity<String> updateUser(String id, String payload) {
        logger.info("Entering updateUser for id {}", id);
        try {
            ensureValidToken();
            String relative = "/crm/v2/users/" + id;
            URI uri = buildUri(relative);

            HttpHeaders headers = new HttpHeaders();
            headers.setContentType(MediaType.APPLICATION_JSON);
            headers.set("Authorization", "Zoho-oauthtoken " + accessToken);

            HttpEntity<String> request = new HttpEntity<>(payload, headers);

            try {
                ResponseEntity<String> resp = apiRestTemplate.exchange(uri, HttpMethod.PUT, request, String.class);
                logger.info("Exiting updateUser with status {}", resp.getStatusCodeValue());
                return resp;
            } catch (HttpStatusCodeException ex) {
                if (ex.getStatusCode().value() == 401) {
                    logger.warn("Received 401 on updateUser, forcing token refresh and retrying once");
                    forceRefreshTokenAndRetry(() -> apiRestTemplate.exchange(buildUri(relative), HttpMethod.PUT, new HttpEntity<>(payload, new HttpHeaders() {{ setContentType(MediaType.APPLICATION_JSON); put("Authorization", java.util.List.of("Zoho-oauthtoken " + accessToken)); }}), String.class));
                }
                throw ex;
            }
        } catch (Exception ex) {
            logger.error("Error in updateUser", ex);
            throw ex;
        }
    }

    private URI buildUri(String relativePath) {
        String effectiveBase = (apiDomain != null && !apiDomain.isBlank()) ? apiDomain : cfg.getBaseUrl();
        String corrected = relativePath.startsWith("/") ? relativePath : "/" + relativePath;
        return URI.create(effectiveBase + corrected);
    }

    private boolean isTokenExpired() {
        if (accessToken == null || accessToken.isBlank()) {
            return true;
        }
        if (tokenExpiry == null) {
            return true;
        }
        return Instant.now().isAfter(tokenExpiry) || Instant.now().equals(tokenExpiry);
    }

    private void ensureValidToken() {
        if (isTokenExpired()) {
            synchronized (this) {
                if (isTokenExpired()) {
                    refreshTokenInternal();
                }
            }
        }
    }

    private void refreshTokenInternal() {
        logger.info("Refreshing Zoho access token");
        try {
            HttpHeaders headers = new HttpHeaders();
            headers.setContentType(MediaType.APPLICATION_FORM_URLENCODED);

            Map<String, String> form = new HashMap<>();
            form.put("grant_type", "refresh_token");
            form.put("client_id", cfg.getClientId());
            form.put("client_secret", cfg.getClientSecret());
            form.put("refresh_token", cfg.getRefreshToken());

            StringBuilder bodyBuilder = new StringBuilder();
            form.forEach((k, v) -> {
                if (bodyBuilder.length() > 0) {
                    bodyBuilder.append('&');
                }
                bodyBuilder.append(k).append('=').append(java.net.URLEncoder.encode(v, java.nio.charset.StandardCharsets.UTF_8));
            });

            HttpEntity<String> entity = new HttpEntity<>(bodyBuilder.toString(), headers);

            String tokenUrl = cfg.getTokenUrl();

            ResponseEntity<String> resp = tokenRestTemplate.postForEntity(tokenUrl, entity, String.class);

            int status = resp.getStatusCodeValue();
            String body = resp.getBody();

            if (status < 200 || status >= 300) {
                throw new RuntimeException("Token refresh failed: " + status + " - " + body);
            }

            Map<String, Object> parsed = mapper.readValue(body, new TypeReference<Map<String, Object>>() {});

            Object atObj = parsed.get("access_token");
            if (!(atObj instanceof String) || ((String) atObj).isBlank()) {
                throw new RuntimeException("Token refresh failed: access_token missing or empty in response");
            }

            String newAccessToken = (String) atObj;
            Number expiresInNum = null;
            if (parsed.get("expires_in") instanceof Number) {
                expiresInNum = (Number) parsed.get("expires_in");
            } else if (parsed.get("expires_in") instanceof String) {
                try {
                    expiresInNum = Integer.parseInt((String) parsed.get("expires_in"));
                } catch (NumberFormatException ignored) {
                }
            }

            long expiresIn = 3600L;
            if (expiresInNum != null) {
                expiresIn = expiresInNum.longValue();
                if (expiresIn == 0L) {
                    expiresIn = 3600L;
                }
            }

            String apiDomainFromResp = null;
            if (parsed.get("api_domain") instanceof String) {
                apiDomainFromResp = (String) parsed.get("api_domain");
            }

            this.accessToken = newAccessToken;

            if (expiresIn < 60L) {
                this.tokenExpiry = Instant.EPOCH;
            } else {
                this.tokenExpiry = Instant.now().plusSeconds(expiresIn - 30L);
            }

            if (apiDomainFromResp != null && !apiDomainFromResp.isBlank()) {
                this.apiDomain = apiDomainFromResp.endsWith("/") ? apiDomainFromResp.substring(0, apiDomainFromResp.length() - 1) : apiDomainFromResp;
            } else {
                this.apiDomain = cfg.getBaseUrl();
            }

            logger.info("Token refreshed successfully, apiDomain set to {}", this.apiDomain);
        } catch (RuntimeException re) {
            logger.error("Token refresh runtime failure", re);
            throw re;
        } catch (Exception ex) {
            logger.error("Token refresh failed", ex);
            throw new RuntimeException("Token refresh failed: " + ex.getMessage(), ex);
        }
    }

    private ResponseEntity<String> forceRefreshTokenAndRetry(java.util.concurrent.Callable<ResponseEntity<String>> action) {
        synchronized (this) {
            this.tokenExpiry = Instant.EPOCH;
            this.accessToken = null;
            refreshTokenInternal();
        }

        try {
            return action.call();
        } catch (Exception ex) {
            logger.error("Retry after token refresh failed", ex);
            if (ex instanceof RuntimeException) {
                throw (RuntimeException) ex;
            }
            throw new RuntimeException(ex);
        }
    }
}

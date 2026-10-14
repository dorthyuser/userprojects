package com.ai2dev.bustravelaccounts.service;

import com.ai2dev.bustravelaccounts.client.SaPostgresClient;
import com.ai2dev.bustravelaccounts.model.request.SaAccountCreateRequest;
import com.ai2dev.bustravelaccounts.model.request.SaAccountUpdateRequest;
import com.ai2dev.bustravelaccounts.model.response.SaAccountResponse;
import com.ai2dev.bustravelaccounts.model.response.SaApiResult;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.dao.DataAccessException;
import org.springframework.stereotype.Service;
import tools.jackson.databind.ObjectMapper;

import java.sql.SQLException;
import java.time.OffsetDateTime;
import java.util.List;
import java.util.Map;
import java.util.UUID;

@Service
public class SaAccountService {
    private static final Logger log = LoggerFactory.getLogger(SaAccountService.class);
    private final SaPostgresClient client;
    private final ObjectMapper mapper;

    public SaAccountService(SaPostgresClient client, ObjectMapper mapper) {
        this.client = client;
        this.mapper = mapper;
    }

    public SaApiResult getAccounts(Map<String, String> query, Map<String, String> headers) {
        try {
            String email = query == null ? null : query.get("email");
            List<SaAccountResponse> accounts = client.findByEmail(email);
            if (accounts.isEmpty()) {
                return new SaApiResult(204, "[]", Map.of("Content-Type", "application/json"));
            }
            return new SaApiResult(200, mapper.writeValueAsString(accounts), Map.of("Content-Type", "application/json"));
        } catch (Exception e) {
            log.error("GET /accounts failed", e);
            return errorFromException(e, 400, "BAD REQUEST");
        }
    }

    public SaApiResult getAccountById(String id, Map<String, String> headers) {
        try {
            UUID uuid;
            try {
                uuid = UUID.fromString(id == null ? "" : id.trim());
            } catch (Exception ex) {
                return new SaApiResult(204, "[]", Map.of("Content-Type", "application/json"));
            }
            return client.findById(uuid)
                    .map(account -> {
                        try {
                            return new SaApiResult(200, mapper.writeValueAsString(account), Map.of("Content-Type", "application/json"));
                        } catch (Exception e) {
                            throw new RuntimeException(e);
                        }
                    })
                    .orElseGet(() -> new SaApiResult(204, "[]", Map.of("Content-Type", "application/json")));
        } catch (RuntimeException e) {
            if (e.getCause() instanceof Exception cause) {
                log.error("GET /accounts/{id} failed", cause);
                return errorFromException(cause, 400, "BAD REQUEST");
            }
            throw e;
        } catch (Exception e) {
            log.error("GET /accounts/{id} failed", e);
            return errorFromException(e, 400, "BAD REQUEST");
        }
    }

    public SaApiResult postAccounts(String body, Map<String, String> headers) {
        try {
            SaAccountCreateRequest request = mapper.readValue(body, SaAccountCreateRequest.class);
            UUID id = UUID.randomUUID();
            client.insert(id, request);
            return new SaApiResult(200, mapper.writeValueAsString(Map.of("id", id.toString())), Map.of("Content-Type", "application/json"));
        } catch (DataAccessException e) {
            log.error("POST /accounts failed", e);
            String sqlState = e.getMostSpecificCause() instanceof SQLException sql ? sql.getSQLState() : null;
            String message = (sqlState == null ? "UNKNOWN" : sqlState) + " ERROR";
            return new SaApiResult(400, mapperValue(Map.of("error", Map.of("errorCode", 400, "errorDateTime", OffsetDateTime.now().toString(), "errorMessage", message, "errorDescription", mostSpecificMessage(e)))), Map.of("Content-Type", "application/json"));
        } catch (Exception e) {
            log.error("POST /accounts failed", e);
            return errorFromException(e, 500, "MULE UNKNOWN ERROR");
        }
    }

    public SaApiResult putAccounts(String id, String body, Map<String, String> headers) {
        try {
            UUID uuid = UUID.fromString(id == null ? "" : id.trim());
            SaAccountUpdateRequest request = mapper.readValue(body, SaAccountUpdateRequest.class);
            int updated = client.update(uuid, request);
            if (updated == 0) {
                return new SaApiResult(400, mapperValue(Map.of("error", Map.of("errorCode", 400, "errorDateTime", OffsetDateTime.now().toString(), "errorMessage", "RESOURCE NOT FOUND", "errorDescription", "Account not found"))), Map.of("Content-Type", "application/json"));
            }
            return new SaApiResult(200, mapper.writeValueAsString(Map.of("id", uuid.toString())), Map.of("Content-Type", "application/json"));
        } catch (DataAccessException e) {
            log.error("PUT /accounts/{id} failed", e);
            String sqlState = e.getMostSpecificCause() instanceof SQLException sql ? sql.getSQLState() : null;
            String message = (sqlState == null ? "UNKNOWN" : sqlState) + " ERROR";
            return new SaApiResult(400, mapperValue(Map.of("error", Map.of("errorCode", 400, "errorDateTime", OffsetDateTime.now().toString(), "errorMessage", message, "errorDescription", mostSpecificMessage(e)))), Map.of("Content-Type", "application/json"));
        } catch (Exception e) {
            log.error("PUT /accounts/{id} failed", e);
            return errorFromException(e, 500, "MULE UNKNOWN ERROR");
        }
    }

    public SaApiResult alive() {
        return new SaApiResult(200, "UP", Map.of("Content-Type", "text/plain"));
    }

    public SaApiResult ready() {
        return new SaApiResult(200, "UP", Map.of("Content-Type", "text/plain"));
    }

    private String mapperValue(Object value) {
        try {
            return mapper.writeValueAsString(value);
        } catch (Exception e) {
            throw new RuntimeException(e);
        }
    }

    private String mostSpecificMessage(DataAccessException e) {
        Throwable cause = e.getMostSpecificCause();
        return cause == null ? "" : cause.getMessage();
    }

    private SaApiResult errorFromException(Exception e, int status, String message) {
        try {
            return new SaApiResult(status, mapper.writeValueAsString(Map.of("error", Map.of("errorCode", status, "errorDateTime", OffsetDateTime.now().toString(), "errorMessage", message, "errorDescription", "An unexpected error occurred"))), Map.of("Content-Type", "application/json"));
        } catch (Exception ex) {
            throw new RuntimeException(ex);
        }
    }
}

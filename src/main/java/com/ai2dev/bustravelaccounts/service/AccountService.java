package com.ai2dev.bustravelaccounts.service;

import com.ai2dev.bustravelaccounts.client.PostgreSqlAccountRepository;
import com.ai2dev.bustravelaccounts.exception.AccountHotlistedException;
import com.ai2dev.bustravelaccounts.exception.AccountNotFoundException;
import com.ai2dev.bustravelaccounts.exception.BadRequestException;
import com.ai2dev.bustravelaccounts.exception.DatabaseOperationException;
import com.ai2dev.bustravelaccounts.exception.DuplicateEmailException;
import com.ai2dev.bustravelaccounts.exception.MuleUnknownException;
import com.ai2dev.bustravelaccounts.model.request.AccountUpsertRequest;
import com.ai2dev.bustravelaccounts.model.response.AccountResponse;
import com.ai2dev.bustravelaccounts.model.response.IdResponse;
import com.ai2dev.bustravelaccounts.util.HeaderUtils;
import java.util.List;
import java.util.UUID;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.http.HttpHeaders;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.stereotype.Service;
import tools.jackson.core.JacksonException;
import tools.jackson.databind.json.JsonMapper;

@Service
public class AccountService {
    private static final Logger log = LoggerFactory.getLogger(AccountService.class);

    private final PostgreSqlAccountRepository repository;
    private final JsonMapper jsonMapper;

    public AccountService(PostgreSqlAccountRepository repository, JsonMapper jsonMapper) {
        this.repository = repository;
        this.jsonMapper = jsonMapper;
    }

    public ResponseEntity<?> getAccounts(String email, HttpHeaders headers, String relativePath) {
        String customerCorrelationId = HeaderUtils.customerCorrelationId(headers);
        String correlationId = HeaderUtils.correlationId(headers);
        log.info("START - Request received");
        List<AccountResponse> accounts = repository.findByEmail(email, customerCorrelationId, correlationId);
        log.info("END - Request processing completed");
        if (accounts.isEmpty()) {
            return ResponseEntity.status(204).contentType(MediaType.APPLICATION_JSON).build();
        }
        return ResponseEntity.ok().contentType(MediaType.APPLICATION_JSON).body(accounts);
    }

    public ResponseEntity<?> getAccount(String id, HttpHeaders headers, String relativePath) {
        String customerCorrelationId = HeaderUtils.customerCorrelationId(headers);
        String correlationId = HeaderUtils.correlationId(headers);
        log.info("START - Request received");
        AccountResponse account = repository.findById(id, customerCorrelationId, correlationId);
        log.info("END - Request processing completed");
        if (account == null) {
            return ResponseEntity.status(204).contentType(MediaType.APPLICATION_JSON).build();
        }
        return ResponseEntity.ok().contentType(MediaType.APPLICATION_JSON).body(account);
    }

    public ResponseEntity<?> createAccount(String body, HttpHeaders headers, String relativePath) {
        String customerCorrelationId = HeaderUtils.customerCorrelationId(headers);
        String correlationId = HeaderUtils.correlationId(headers);
        log.info("START - Request received");
        AccountUpsertRequest request = parseBody(body);

        List<AccountResponse> existingAccounts = repository.findByEmail(lowerTrim(request.personEmail()), customerCorrelationId, correlationId);
        if (!existingAccounts.isEmpty()) {
            log.warn("POST /accounts rejected: duplicate email");
            throw new DuplicateEmailException("An account with this email already exists");
        }

        try {
            UUID id = repository.create(request, customerCorrelationId, correlationId);
            log.info("END - Request processing completed");
            return ResponseEntity.status(201).contentType(MediaType.APPLICATION_JSON).body(new IdResponse(id.toString()));
        } catch (DatabaseOperationException ex) {
            log.error("Error payload archive replaced with log entry for POST /accounts");
            throw ex;
        }
    }

    public ResponseEntity<?> updateAccount(String id, String body, HttpHeaders headers, String relativePath) {
        String customerCorrelationId = HeaderUtils.customerCorrelationId(headers);
        String correlationId = HeaderUtils.correlationId(headers);
        log.info("START - Request received");
        AccountUpsertRequest request = parseBody(body);

        AccountResponse existingAccount = repository.findById(id, customerCorrelationId, correlationId);
        if (existingAccount == null) {
            log.warn("PUT /accounts/{id} rejected: Account not found");
            throw new AccountNotFoundException("Account not found");
        }
        if (Boolean.TRUE.equals(existingAccount.hotlisted())) {
            log.warn("PUT /accounts/{id} rejected: Account is hotlisted and cannot be changed");
            throw new AccountHotlistedException("Account is hotlisted and cannot be changed");
        }

        try {
            UUID updatedId = repository.update(id, request, customerCorrelationId, correlationId);
            log.info("END - Request processing completed");
            return ResponseEntity.ok().contentType(MediaType.APPLICATION_JSON).body(new IdResponse(updatedId.toString()));
        } catch (DatabaseOperationException ex) {
            log.error("Error payload archive replaced with log entry for PUT /accounts/{id}");
            throw ex;
        }
    }

    private AccountUpsertRequest parseBody(String body) {
        if (body == null || body.isBlank()) {
            throw new MuleUnknownException("check payload existence or method/payload combination requirement");
        }
        try {
            return jsonMapper.readValue(body, AccountUpsertRequest.class);
        } catch (JacksonException ex) {
            throw new BadRequestException("BAD REQUEST", ex);
        }
    }

    private String lowerTrim(String value) {
        return value == null ? "" : value.trim().toLowerCase();
    }
}

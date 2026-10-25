package com.ai2dev.bustravelaccounts.service;

import com.ai2dev.bustravelaccounts.client.PostgresAccountsRepository;
import com.ai2dev.bustravelaccounts.exception.BadRequestException;
import com.ai2dev.bustravelaccounts.exception.ConflictException;
import com.ai2dev.bustravelaccounts.exception.HotlistedAccountException;
import com.ai2dev.bustravelaccounts.exception.MuleUnknownErrorException;
import com.ai2dev.bustravelaccounts.exception.NotFoundRuleException;
import com.ai2dev.bustravelaccounts.model.request.AccountUpsertRequest;
import com.ai2dev.bustravelaccounts.model.response.AccountResponse;
import com.ai2dev.bustravelaccounts.model.response.IdResponse;
import com.ai2dev.bustravelaccounts.util.HeaderUtils;
import java.util.List;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.http.HttpHeaders;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.stereotype.Service;
import tools.jackson.databind.JsonNode;
import tools.jackson.databind.ObjectMapper;

@Service
public class AccountsService {
    private static final Logger log = LoggerFactory.getLogger(AccountsService.class);

    private final PostgresAccountsRepository postgresAccountsRepository;
    private final ObjectMapper objectMapper;

    public AccountsService(PostgresAccountsRepository postgresAccountsRepository, ObjectMapper objectMapper) {
        this.postgresAccountsRepository = postgresAccountsRepository;
        this.objectMapper = objectMapper;
    }

    public ResponseEntity<?> getAccounts(String email, HttpHeaders headers) {
        String customerCorrelationId = HeaderUtils.customerCorrelationId(headers);
        String correlationId = HeaderUtils.correlationId(headers);
        log.debug("Before Request - ListAccountsByEmail correlationId={} customerCorrelationId={}", correlationId, customerCorrelationId);
        List<AccountResponse> accounts = postgresAccountsRepository.findByEmail(email);
        if (accounts.isEmpty()) {
            return ResponseEntity.status(204).contentType(MediaType.APPLICATION_JSON).body("[]");
        }
        return ResponseEntity.ok().contentType(MediaType.APPLICATION_JSON).body(accounts);
    }

    public ResponseEntity<?> getAccount(String id, HttpHeaders headers) {
        String customerCorrelationId = HeaderUtils.customerCorrelationId(headers);
        String correlationId = HeaderUtils.correlationId(headers);
        log.debug("Before Request - GetAccountDetails correlationId={} customerCorrelationId={}", correlationId, customerCorrelationId);
        AccountResponse account = postgresAccountsRepository.findById(id);
        if (account == null) {
            return ResponseEntity.status(204).contentType(MediaType.APPLICATION_JSON).body("[]");
        }
        return ResponseEntity.ok().contentType(MediaType.APPLICATION_JSON).body(account);
    }

    public ResponseEntity<?> createAccount(String body, HttpHeaders headers) {
        AccountUpsertRequest request = parseBody(body);
        String personEmail = lowerTrim(request.personEmail());
        log.debug("Before Request - duplicate check");
        List<AccountResponse> existingAccounts = postgresAccountsRepository.findByEmail(personEmail);
        if (!existingAccounts.isEmpty()) {
            throw new ConflictException("An account with this email already exists");
        }
        String id = postgresAccountsRepository.insert(request);
        return ResponseEntity.status(201).contentType(MediaType.APPLICATION_JSON).body(new IdResponse(id));
    }

    public ResponseEntity<?> updateAccount(String id, String body, HttpHeaders headers) {
        AccountUpsertRequest request = parseBody(body);
        AccountResponse existingAccount = postgresAccountsRepository.findById(id == null ? null : id.trim());
        if (existingAccount == null) {
            throw new NotFoundRuleException("Account not found");
        }
        if (Boolean.TRUE.equals(existingAccount.hotlisted())) {
            throw new HotlistedAccountException("Account is hotlisted and cannot be changed");
        }
        String updatedId = postgresAccountsRepository.update(id == null ? null : id.trim(), request);
        return ResponseEntity.ok().contentType(MediaType.APPLICATION_JSON).body(new IdResponse(updatedId));
    }

    private AccountUpsertRequest parseBody(String body) {
        if (body == null || body.isBlank()) {
            throw new MuleUnknownErrorException("check payload existence or method/payload combination requirement");
        }
        try {
            JsonNode ignored = objectMapper.readTree(body);
            return objectMapper.readValue(body, AccountUpsertRequest.class);
        } catch (RuntimeException exception) {
            throw new BadRequestException("BAD REQUEST", exception);
        }
    }

    private String lowerTrim(String value) {
        return value == null ? "" : value.trim().toLowerCase();
    }
}

package com.ai2dev.bustravelaccounts.service;

import com.ai2dev.bustravelaccounts.dto.request.AccountRequest;
import com.ai2dev.bustravelaccounts.dto.response.AccountResponse;
import com.ai2dev.bustravelaccounts.exception.AccountHotlistedException;
import com.ai2dev.bustravelaccounts.exception.AccountNotFoundException;
import com.ai2dev.bustravelaccounts.exception.DuplicateAccountException;
import com.ai2dev.bustravelaccounts.exception.EmptyBodyException;
import com.ai2dev.bustravelaccounts.repository.AccountRepository;
import java.util.List;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.http.HttpHeaders;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.stereotype.Service;
import tools.jackson.databind.ObjectMapper;

@Service
public class AccountService {
    private static final Logger log = LoggerFactory.getLogger(AccountService.class);
    private final AccountRepository repository;
    private final ObjectMapper objectMapper;

    public AccountService(AccountRepository repository, ObjectMapper objectMapper) {
        this.repository = repository;
        this.objectMapper = objectMapper;
    }

    public void ready() {
        repository.findByEmail("");
    }

    public ResponseEntity<List<AccountResponse>> getAccounts(HttpHeaders headers, String email) {
        List<AccountResponse> accounts = repository.findByEmail(email == null ? "" : email);
        if (accounts.isEmpty()) {
            return ResponseEntity.status(HttpStatus.NO_CONTENT).contentType(org.springframework.http.MediaType.APPLICATION_JSON).build();
        }
        return ResponseEntity.ok().contentType(org.springframework.http.MediaType.APPLICATION_JSON).body(accounts);
    }

    public ResponseEntity<AccountResponse> getAccount(HttpHeaders headers, String id) {
        return repository.findById(id)
                .map(account -> ResponseEntity.ok().contentType(org.springframework.http.MediaType.APPLICATION_JSON).body(account))
                .orElseThrow(() -> new AccountNotFoundException("Account not found"));
    }

    public ResponseEntity<?> createAccount(HttpHeaders headers, String body) {
        if (body == null || body.isBlank()) throw new EmptyBodyException("check payload existence or method/payload combination requirement");
        AccountRequest request = parse(body);
        if (!repository.findByEmail(request.personEmail() == null ? "" : request.personEmail()).isEmpty()) {
            throw new DuplicateAccountException("An account with this email already exists");
        }
        return ResponseEntity.status(HttpStatus.CREATED).contentType(org.springframework.http.MediaType.APPLICATION_JSON).body(java.util.Map.of("id", repository.insert(request).toString()));
    }

    public ResponseEntity<?> updateAccount(HttpHeaders headers, String id, String body) {
        if (body == null || body.isBlank()) throw new EmptyBodyException("check payload existence or method/payload combination requirement");
        AccountRequest request = parse(body);
        AccountResponse existing = repository.findById(id).orElseThrow(() -> new AccountNotFoundException("Account not found"));
        if (Boolean.TRUE.equals(existing.hotlisted())) throw new AccountHotlistedException("Account is hotlisted and cannot be changed");
        return ResponseEntity.ok().contentType(org.springframework.http.MediaType.APPLICATION_JSON).body(java.util.Map.of("id", repository.update(id, request).toString()));
    }

    private AccountRequest parse(String body) {
        try {
            return objectMapper.readValue(body, AccountRequest.class);
        } catch (Exception ex) {
            throw new RuntimeException(ex);
        }
    }
}

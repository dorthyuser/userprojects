package com.ai2dev.bustravelaccountssa.controller;

import java.util.List;

import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.PutMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.RestController;

import com.ai2dev.bustravelaccountssa.model.request.AccountCreateRequest;
import com.ai2dev.bustravelaccountssa.model.request.AccountUpdateRequest;
import com.ai2dev.bustravelaccountssa.model.response.AccountResponse;
import com.ai2dev.bustravelaccountssa.model.response.IdResponse;
import com.ai2dev.bustravelaccountssa.service.AccountService;

@RestController
@RequestMapping
public class AccountController {
    private final AccountService service;

    public AccountController(AccountService service) {
        this.service = service;
    }

    @PutMapping(value = "/accounts/{id}", consumes = MediaType.APPLICATION_JSON_VALUE, produces = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<?> update(@PathVariable String id, @RequestBody(required = false) AccountUpdateRequest body) {
        IdResponse response = service.updateAccount(id, body);
        return ResponseEntity.ok(response);
    }

    @GetMapping(value = "/accounts", produces = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<?> getAccounts(@RequestParam(required = false) String email) {
        List<AccountResponse> response = service.getAccounts(email);
        if (response.isEmpty()) {
            return ResponseEntity.status(204).body("[]");
        }
        return ResponseEntity.ok(response);
    }

    @GetMapping(value = "/accounts/{id}", produces = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<?> getAccount(@PathVariable String id) {
        AccountResponse response = service.getAccountById(id);
        if (response == null) {
            return ResponseEntity.status(204).body("[]");
        }
        return ResponseEntity.ok(response);
    }

    @PostMapping(value = "/accounts", consumes = MediaType.APPLICATION_JSON_VALUE, produces = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<?> create(@RequestBody(required = false) AccountCreateRequest body) {
        IdResponse response = service.createAccount(body);
        return ResponseEntity.ok(response);
    }
}

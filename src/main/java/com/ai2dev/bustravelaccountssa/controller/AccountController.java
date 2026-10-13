package com.ai2dev.bustravelaccountssa.controller;

import java.util.List;

import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.PutMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.RestController;

import com.ai2dev.bustravelaccountssa.model.request.AccountCreateRequest;
import com.ai2dev.bustravelaccountssa.model.request.AccountUpdateRequest;
import com.ai2dev.bustravelaccountssa.model.response.AccountResponse;
import com.ai2dev.bustravelaccountssa.model.response.IdResponse;
import com.ai2dev.bustravelaccountssa.service.AccountService;

@RestController
public class AccountController {
    private final AccountService service;

    public AccountController(AccountService service) {
        this.service = service;
    }

    @GetMapping("/accounts")
    public ResponseEntity<List<AccountResponse>> getAccounts(@RequestParam String email) {
        List<AccountResponse> body = service.getAccounts(email);
        return body.isEmpty() ? ResponseEntity.status(204).body(List.of()) : ResponseEntity.ok(body);
    }

    @GetMapping("/accounts/{id}")
    public ResponseEntity<AccountResponse> getAccount(@PathVariable String id) {
        return ResponseEntity.ok(service.getAccount(id));
    }

    @PostMapping(value = "/accounts", consumes = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<IdResponse> create(@RequestBody AccountCreateRequest request) {
        return ResponseEntity.ok(service.create(request));
    }

    @PutMapping(value = "/accounts/{id}", consumes = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<IdResponse> update(@PathVariable String id, @RequestBody AccountUpdateRequest request) {
        return ResponseEntity.ok(service.update(id, request));
    }
}

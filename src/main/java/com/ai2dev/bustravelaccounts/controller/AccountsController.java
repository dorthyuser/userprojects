package com.ai2dev.bustravelaccounts.controller;

import com.ai2dev.bustravelaccounts.model.request.AccountRequest;
import com.ai2dev.bustravelaccounts.model.response.AccountResponse;
import com.ai2dev.bustravelaccounts.model.response.IdResponse;
import com.ai2dev.bustravelaccounts.service.AccountsService;
import java.util.List;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.PutMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestHeader;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.RestController;

@RestController
@RequestMapping
public class AccountsController {
    private final AccountsService service;

    public AccountsController(AccountsService service) {
        this.service = service;
    }

    @GetMapping("/accounts")
    public ResponseEntity<List<AccountResponse>> getAccounts(@RequestParam(required = false) String email, @RequestHeader org.springframework.http.HttpHeaders headers) {
        return ResponseEntity.ok().contentType(MediaType.APPLICATION_JSON).body(service.getAccounts(email));
    }

    @GetMapping("/accounts/{id}")
    public ResponseEntity<AccountResponse> getAccount(@PathVariable String id) {
        return ResponseEntity.ok().contentType(MediaType.APPLICATION_JSON).body(service.getAccountById(id));
    }

    @PostMapping(value = "/accounts", consumes = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<IdResponse> createAccount(@RequestBody(required = false) AccountRequest request) {
        return ResponseEntity.status(201).contentType(MediaType.APPLICATION_JSON).body(service.createAccount(request));
    }

    @PutMapping(value = "/accounts/{id}", consumes = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<IdResponse> updateAccount(@PathVariable String id, @RequestBody(required = false) AccountRequest request) {
        return ResponseEntity.ok().contentType(MediaType.APPLICATION_JSON).body(service.updateAccount(id, request));
    }
}

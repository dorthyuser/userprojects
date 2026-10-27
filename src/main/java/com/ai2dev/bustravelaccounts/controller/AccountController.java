package com.ai2dev.bustravelaccounts.controller;

import com.ai2dev.bustravelaccounts.service.AccountService;
import org.springframework.http.HttpHeaders;
import org.springframework.http.MediaType;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.PutMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestHeader;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.RestController;

@RestController
public class AccountController {
    private final AccountService accountService;

    public AccountController(AccountService accountService) {
        this.accountService = accountService;
    }

    @GetMapping("/accounts")
    public ResponseEntity<?> getAccounts(@RequestParam(required = false) String email, @RequestHeader HttpHeaders headers) {
        return accountService.getAccounts(email, headers, "/accounts");
    }

    @GetMapping("/accounts/{id}")
    public ResponseEntity<?> getAccount(@PathVariable String id, @RequestHeader HttpHeaders headers) {
        return accountService.getAccount(id, headers, "/accounts/" + id);
    }

    @PostMapping(value = "/accounts", consumes = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<?> createAccount(@RequestBody(required = false) String body, @RequestHeader HttpHeaders headers) {
        return accountService.createAccount(body, headers, "/accounts");
    }

    @PutMapping(value = "/accounts/{id}", consumes = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<?> updateAccount(@PathVariable String id, @RequestBody(required = false) String body, @RequestHeader HttpHeaders headers) {
        return accountService.updateAccount(id, body, headers, "/accounts/" + id);
    }
}

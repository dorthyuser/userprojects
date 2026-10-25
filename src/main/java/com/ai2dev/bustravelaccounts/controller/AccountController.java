package com.ai2dev.bustravelaccounts.controller;

import com.ai2dev.bustravelaccounts.util.JsonBodyParser;

import java.util.List;
import org.springframework.http.HttpHeaders;
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
import com.ai2dev.bustravelaccounts.model.response.AccountResponse;
import com.ai2dev.bustravelaccounts.model.response.IdResponse;
import com.ai2dev.bustravelaccounts.service.AccountService;

@RestController
@RequestMapping
public class AccountController {
    private final AccountService service;

    public AccountController(AccountService service) {
        this.service = service;
    }

    @GetMapping("/accounts")
    public ResponseEntity<List<AccountResponse>> getAccounts(@RequestHeader HttpHeaders headers, @RequestParam(required = false) String email) {
        return ResponseEntity.ok().contentType(MediaType.APPLICATION_JSON).body(service.getAccounts(headers, email));
    }

    @GetMapping("/accounts/{id}")
    public ResponseEntity<AccountResponse> getAccount(@RequestHeader HttpHeaders headers, @PathVariable String id) {
        AccountResponse response = service.getAccountById(headers, id);
        return ResponseEntity.ok().contentType(MediaType.APPLICATION_JSON).body(response);
    }

    @PostMapping(value = "/accounts", consumes = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<IdResponse> createAccount(@RequestHeader HttpHeaders headers, @RequestBody(required = false) String body) {
        return ResponseEntity.status(201).contentType(MediaType.APPLICATION_JSON).body(service.createAccount(headers, JsonBodyParser.parse(body)));
    }

    @PutMapping(value = "/accounts/{id}", consumes = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<IdResponse> updateAccount(@RequestHeader HttpHeaders headers, @PathVariable String id, @RequestBody(required = false) String body) {
        return ResponseEntity.ok().contentType(MediaType.APPLICATION_JSON).body(service.updateAccount(headers, id, JsonBodyParser.parse(body)));
    }
}

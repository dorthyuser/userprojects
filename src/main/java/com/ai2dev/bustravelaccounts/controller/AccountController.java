package com.ai2dev.bustravelaccounts.controller;

import com.ai2dev.bustravelaccounts.dto.request.AccountRequest;
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
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.RestController;

@RestController
@RequestMapping
public class AccountController {
    private final AccountService service;

    public AccountController(AccountService service) {
        this.service = service;
    }

    @GetMapping(value = "/accounts", produces = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<?> getAccounts(@RequestHeader HttpHeaders headers, @RequestParam(required = false) String email) {
        return ResponseEntity.ok().contentType(MediaType.APPLICATION_JSON).body(service.getAccounts(email));
    }

    @GetMapping(value = "/accounts/{id}", produces = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<?> getAccount(@PathVariable String id) {
        return ResponseEntity.ok().contentType(MediaType.APPLICATION_JSON).body(service.getAccount(id));
    }

    @PostMapping(value = "/accounts", consumes = MediaType.APPLICATION_JSON_VALUE, produces = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<?> create(@RequestBody(required = false) AccountRequest request) {
        return ResponseEntity.status(201).contentType(MediaType.APPLICATION_JSON).body(service.create(request));
    }

    @PutMapping(value = "/accounts/{id}", consumes = MediaType.APPLICATION_JSON_VALUE, produces = MediaType.APPLICATION_JSON_VALUE)
    public ResponseEntity<?> update(@PathVariable String id, @RequestBody(required = false) AccountRequest request) {
        return ResponseEntity.ok().contentType(MediaType.APPLICATION_JSON).body(service.update(id, request));
    }
}
